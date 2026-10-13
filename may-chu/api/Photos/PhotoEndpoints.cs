using System.Security.Claims;
using Hue_travel.Api.Audit;
using Hue_travel.Api.Data;
using Npgsql;
using SkiaSharp;

namespace Hue_travel.Api.Photos;

public sealed record PhotoUpload(string? ContentType, string? Data);
public sealed record PhotoDecision(bool Approve, string? Note);

/// <summary>
/// Photos of places. An owner (or an admin) uploads; every upload is decoded, turned upright, shrunk to at most 1600 px and
/// saved again as a JPEG without any metadata, so what is stored is only pixels. It is public after an admin approves it.
/// The bytes travel as base64 inside the signed JSON replies, like everything else the desktop app reads.
/// </summary>
public static class PhotoEndpoints
{
    public const int MaxBodyBytes = 8 * 1024 * 1024;
    public const int MaxPhotosPerPlace = 8;
    public const int MaxPendingPerPlace = 4;
    private const int MaxSide = 1600;
    private const long MaxPixels = 40_000_000;
    private const int MaxEncodedBytes = 3_000_000;

    private static IResult Bad(string detail) => Results.Problem(detail: detail, statusCode: StatusCodes.Status400BadRequest);

    private static IResult NotFound(string detail) => Results.Problem(detail: detail, statusCode: StatusCodes.Status404NotFound);

    /// <summary>True for the requests that carry a photo, which are allowed a bigger body than the 64 KB of everything else.</summary>
    public static bool IsUploadRequest(HttpRequest request) =>
        HttpMethods.IsPost(request.Method) && request.Path.Value != null && request.Path.Value.EndsWith("/photos", StringComparison.Ordinal)
        && (request.Path.StartsWithSegments("/v1/owner/places") || request.Path.StartsWithSegments("/v1/admin/pois"));

    public static void MapPhotoEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/pois/{poiId}/photos", ListPublicAsync);
        app.MapGet("/v1/photos/{id:long}", GetAsync);

        RouteGroupBuilder owner = app.MapGroup("/v1/owner/places/{poiId}/photos").RequireAuthorization();
        owner.MapGet("", ListOwnAsync);
        owner.MapPost("", UploadAsOwnerAsync).RequireRateLimiting("heavy");
        owner.MapDelete("/{id:long}", DeleteAsOwnerAsync);

        RouteGroupBuilder admin = app.MapGroup("/v1/admin").RequireAuthorization("admin");
        admin.MapGet("/photos", ListPendingAsync);
        admin.MapPost("/photos/{id:long}/decision", DecideAsync);
        admin.MapDelete("/photos/{id:long}", DeleteAsAdminAsync);
        admin.MapPost("/pois/{poiId}/photos", UploadAsAdminAsync).RequireRateLimiting("heavy");
    }

    // ------------------------------------------------------------ everyone

    private static async Task<IResult> ListPublicAsync(string poiId, NpgsqlDataSource db, CancellationToken ct) =>
        Results.Ok(await db.ListAsync(
            "select id, width, height from poi_photo where poi_id = @p and status = 'approved' order by position, id",
            r => new { id = r.GetInt64(0), width = r.GetInt32(1), height = r.GetInt32(2) }, ct, ("p", poiId)));

    /// <summary>The photo itself. An approved photo is public; a waiting or refused one is only seen by its place's owners and by admins.</summary>
    private static async Task<IResult> GetAsync(long id, ClaimsPrincipal user, NpgsqlDataSource db, CancellationToken ct)
    {
        string poiId, status;
        byte[] data;
        await using (NpgsqlCommand cmd = db.Command("select poi_id, status, data from poi_photo where id = @i", [("i", id)]))
        await using (NpgsqlDataReader r = await cmd.ExecuteReaderAsync(ct))
        {
            if (!await r.ReadAsync(ct)) return NotFound("Không tìm thấy ảnh.");
            poiId = r.GetString(0);
            status = r.GetString(1);
            data = (byte[])r[2];
        }

        if (status != "approved" && !await CanSeeUnapprovedAsync(user, poiId, db, ct)) return NotFound("Không tìm thấy ảnh.");
        return Results.Ok(new { id, poiId, status, contentType = "image/jpeg", data = Convert.ToBase64String(data) });
    }

    private static async Task<bool> CanSeeUnapprovedAsync(ClaimsPrincipal user, string poiId, NpgsqlDataSource db, CancellationToken ct)
    {
        if (user.Identity?.IsAuthenticated != true) return false;
        if (user.IsInRole("admin")) return true;
        return await IsOwnerAsync(db, user.UserId(), poiId, ct);
    }

    private static Task<bool> IsOwnerAsync(NpgsqlDataSource db, long userId, string poiId, CancellationToken ct) =>
        db.ExistsAsync("select exists (select 1 from place_owner where user_id = @u and poi_id = @p and status = 'approved')", ct, ("u", userId), ("p", poiId));

    // ------------------------------------------------------------ owners

    private static async Task<IResult> ListOwnAsync(string poiId, ClaimsPrincipal user, NpgsqlDataSource db, CancellationToken ct)
    {
        if (!await IsOwnerAsync(db, user.UserId(), poiId, ct)) return NotOwner();
        return Results.Ok(await db.ListAsync(
            "select id, status, note, created_at from poi_photo where poi_id = @p order by position, id",
            r => new { id = r.GetInt64(0), status = r.GetString(1), note = r.Text(2), createdAt = r.GetFieldValue<DateTimeOffset>(3) }, ct, ("p", poiId)));
    }

    private static IResult NotOwner() => Results.Problem(detail: "Bạn không phải chủ quản địa điểm này.", statusCode: StatusCodes.Status403Forbidden);

    private static async Task<IResult> UploadAsOwnerAsync(string poiId, PhotoUpload upload, ClaimsPrincipal user, NpgsqlDataSource db, CancellationToken ct)
    {
        long userId = user.UserId();
        if (!await IsOwnerAsync(db, userId, poiId, ct)) return NotOwner();
        return await StoreAsync(poiId, upload, userId, approved: false, db, ct);
    }

    private static async Task<IResult> UploadAsAdminAsync(string poiId, PhotoUpload upload, ClaimsPrincipal admin, NpgsqlDataSource db, CancellationToken ct) =>
        await StoreAsync(poiId, upload, admin.UserId(), approved: true, db, ct);

    private static async Task<IResult> StoreAsync(string poiId, PhotoUpload upload, long userId, bool approved, NpgsqlDataSource db, CancellationToken ct)
    {
        if (!await db.ExistsAsync("select exists (select 1 from poi where id = @p and deleted_at is null)", ct, ("p", poiId))) return NotFound("Không tìm thấy địa điểm.");
        if (string.IsNullOrEmpty(upload.Data)) return Bad("Thiếu dữ liệu ảnh (data, base64).");
        if (upload.Data.Length > MaxBodyBytes) return Bad("Ảnh quá lớn (tối đa khoảng 6 MB).");

        long total = (long)(await db.ScalarAsync("select count(*) from poi_photo where poi_id = @p and status <> 'rejected'", ct, ("p", poiId)))!;
        if (total >= MaxPhotosPerPlace) return Bad($"Mỗi địa điểm có tối đa {MaxPhotosPerPlace} ảnh. Hãy xóa bớt ảnh cũ.");
        if (!approved)
        {
            long waiting = (long)(await db.ScalarAsync("select count(*) from poi_photo where poi_id = @p and status = 'pending'", ct, ("p", poiId)))!;
            if (waiting >= MaxPendingPerPlace) return Bad($"Đang có {MaxPendingPerPlace} ảnh chờ duyệt. Hãy đợi quản trị viên xử lý.");
        }

        byte[] source;
        try
        {
            source = Convert.FromBase64String(upload.Data);
        }
        catch (FormatException)
        {
            return Bad("Dữ liệu ảnh không phải base64 hợp lệ.");
        }

        (byte[]? jpeg, int width, int height, string? problem) = Reencode(source);
        if (jpeg == null) return Bad(problem!);

        object? id = await db.ScalarAsync(
            "insert into poi_photo (poi_id, data, width, height, status, position, uploaded_by, reviewed_by, reviewed_at) " +
            "values (@p, @d, @w, @h, @s, (select coalesce(max(position), 0) + 1 from poi_photo where poi_id = @p), @u, " +
            "case when @a then @u end, case when @a then now() end) returning id", ct,
            ("p", poiId), ("d", jpeg), ("w", width), ("h", height), ("s", approved ? "approved" : "pending"), ("u", userId), ("a", approved));
        await db.RecordAsync(userId, "photo.upload", "poi_photo", id?.ToString(), null, new { poiId, approved, bytes = jpeg.Length }, ct);
        return Results.Json(new { id, status = approved ? "approved" : "pending" }, statusCode: StatusCodes.Status201Created);
    }

    /// <summary>Decodes an upload and saves it again as an upright JPEG without metadata; returns a message for the person when it is not usable.</summary>
    public static (byte[]? Jpeg, int Width, int Height, string? Problem) Reencode(byte[] source)
    {
        try
        {
            using SKData data = SKData.CreateCopy(source);
            using SKCodec? codec = SKCodec.Create(data);
            if (codec == null || codec.EncodedFormat is not (SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Png))
                return (null, 0, 0, "Chỉ nhận ảnh JPEG hoặc PNG.");
            SKImageInfo info = codec.Info;
            if ((long)info.Width * info.Height > MaxPixels) return (null, 0, 0, "Ảnh có quá nhiều điểm ảnh (tối đa khoảng 40 triệu).");
            if (info.Width < 200 || info.Height < 200) return (null, 0, 0, "Ảnh quá nhỏ (cần ít nhất 200 x 200 điểm ảnh).");
            SKEncodedOrigin origin = codec.EncodedOrigin;

            using SKBitmap? decoded = SKBitmap.Decode(codec);
            if (decoded == null) return (null, 0, 0, "Không đọc được ảnh. Hãy chọn tệp JPEG hoặc PNG hợp lệ.");

            // Turn it upright (the camera's rotation lives in metadata that is not kept), shrink it, and put it on white (a PNG may be see-through).
            bool swap = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
            int w = swap ? decoded.Height : decoded.Width, h = swap ? decoded.Width : decoded.Height;
            double scale = Math.Min(1.0, (double)MaxSide / Math.Max(w, h));
            int outW = Math.Max(1, (int)Math.Round(w * scale)), outH = Math.Max(1, (int)Math.Round(h * scale));

            using var surface = SKSurface.Create(new SKImageInfo(outW, outH, SKColorType.Rgba8888, SKAlphaType.Opaque));
            SKCanvas canvas = surface.Canvas;
            canvas.Clear(SKColors.White);
            canvas.Scale((float)scale);
            SKMatrix upright = UprightMatrix(origin, decoded.Width, decoded.Height);
            canvas.Concat(ref upright);
            using var paint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High };
            canvas.DrawBitmap(decoded, 0, 0, paint);

            using SKImage image = surface.Snapshot();
            using SKData jpeg = image.Encode(SKEncodedImageFormat.Jpeg, 82);
            if (jpeg == null) return (null, 0, 0, "Không nén được ảnh. Hãy chọn ảnh khác.");
            if (jpeg.Size > MaxEncodedBytes) return (null, 0, 0, "Ảnh vẫn quá nặng sau khi nén. Hãy chọn ảnh khác.");
            return (jpeg.ToArray(), outW, outH, null);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or NullReferenceException)
        {
            return (null, 0, 0, "Không đọc được ảnh. Hãy chọn tệp JPEG hoặc PNG hợp lệ.");
        }
    }

    // The matrix that draws an image of the given size upright for an EXIF orientation.
    private static SKMatrix UprightMatrix(SKEncodedOrigin origin, int w, int h) => origin switch
    {
        SKEncodedOrigin.TopRight => new SKMatrix { ScaleX = -1, ScaleY = 1, TransX = w, Persp2 = 1 },
        SKEncodedOrigin.BottomRight => new SKMatrix { ScaleX = -1, ScaleY = -1, TransX = w, TransY = h, Persp2 = 1 },
        SKEncodedOrigin.BottomLeft => new SKMatrix { ScaleX = 1, ScaleY = -1, TransY = h, Persp2 = 1 },
        SKEncodedOrigin.LeftTop => new SKMatrix { SkewX = 1, SkewY = 1, Persp2 = 1 },
        SKEncodedOrigin.RightTop => new SKMatrix { SkewX = -1, SkewY = 1, TransX = h, Persp2 = 1 },
        SKEncodedOrigin.RightBottom => new SKMatrix { SkewX = -1, SkewY = -1, TransX = h, TransY = w, Persp2 = 1 },
        SKEncodedOrigin.LeftBottom => new SKMatrix { SkewX = 1, SkewY = -1, TransY = w, Persp2 = 1 },
        _ => SKMatrix.Identity,
    };

    private static async Task<IResult> DeleteAsOwnerAsync(string poiId, long id, ClaimsPrincipal user, NpgsqlDataSource db, CancellationToken ct)
    {
        long userId = user.UserId();
        if (!await IsOwnerAsync(db, userId, poiId, ct)) return NotOwner();
        int rows = await db.ExecAsync("delete from poi_photo where id = @i and poi_id = @p", ct, ("i", id), ("p", poiId));
        if (rows == 0) return NotFound("Không tìm thấy ảnh.");
        await db.RecordAsync(userId, "photo.delete", "poi_photo", id.ToString(), new { poiId }, null, ct);
        return Results.NoContent();
    }

    // ------------------------------------------------------------ admins

    private static async Task<IResult> ListPendingAsync(NpgsqlDataSource db, CancellationToken ct, string status = "pending") =>
        Results.Ok(await db.ListAsync(
            "select f.id, f.poi_id, p.ten, f.status, u.username::text, f.created_at, f.width, f.height from poi_photo f " +
            "join poi p on p.id = f.poi_id left join app_user u on u.id = f.uploaded_by where f.status = @s order by f.created_at limit 50",
            r => new
            {
                id = r.GetInt64(0), poiId = r.GetString(1), poiName = r.GetString(2), status = r.GetString(3), uploadedBy = r.Text(4),
                createdAt = r.GetFieldValue<DateTimeOffset>(5), width = r.GetInt32(6), height = r.GetInt32(7),
            }, ct, ("s", status)));

    private static async Task<IResult> DecideAsync(long id, PhotoDecision decision, ClaimsPrincipal admin, NpgsqlDataSource db, CancellationToken ct)
    {
        int rows = await db.ExecAsync(
            "update poi_photo set status = @s, note = @n, reviewed_by = @a, reviewed_at = now() where id = @i and status = 'pending'", ct,
            ("s", decision.Approve ? "approved" : "rejected"), ("n", decision.Note?.Trim()), ("a", admin.UserId()), ("i", id));
        if (rows == 0) return NotFound("Không có ảnh đang chờ.");
        await db.RecordAsync(admin.UserId(), decision.Approve ? "photo.approve" : "photo.reject", "poi_photo", id.ToString(), null, new { decision.Note }, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteAsAdminAsync(long id, ClaimsPrincipal admin, NpgsqlDataSource db, CancellationToken ct)
    {
        int rows = await db.ExecAsync("delete from poi_photo where id = @i", ct, ("i", id));
        if (rows == 0) return NotFound("Không tìm thấy ảnh.");
        await db.RecordAsync(admin.UserId(), "photo.delete", "poi_photo", id.ToString(), null, null, ct);
        return Results.NoContent();
    }
}
