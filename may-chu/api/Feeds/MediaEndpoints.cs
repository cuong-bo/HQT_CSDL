using System.Security.Claims;
using Hue_travel.Api.Audit;
using Hue_travel.Api.Data;
using Hue_travel.Api.Photos;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Hue_travel.Api.Feeds;

public sealed record MediaUpload(string? Data);

/// <summary>The only place that knows where the pictures of posts are kept (now: in PostgreSQL). Moving them to disk later changes only this class.</summary>
public sealed class MediaStore(NpgsqlDataSource db)
{
    public async Task<long> SaveAsync(long userId, byte[] full, byte[] thumb, int width, int height, CancellationToken ct) =>
        (long)(await db.ScalarAsync(
            "insert into post_media (uploaded_by, data, thumb, width, height) values (@u, @d, @t, @w, @h) returning id", ct,
            ("u", userId), ("d", full), ("t", thumb), ("w", width), ("h", height)))!;

    /// <summary>The picture and who may see it: its uploader, and anyone once its post is visible.</summary>
    public async Task<(byte[] Data, long UploadedBy, string? PostStatus)?> ReadAsync(long id, bool thumb, CancellationToken ct)
    {
        await using NpgsqlCommand cmd = db.Command(
            $"select m.{(thumb ? "thumb" : "data")}, m.uploaded_by, p.status from post_media m left join post p on p.id = m.post_id where m.id = @i", [("i", id)]);
        await using NpgsqlDataReader r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;
        return ((byte[])r[0], r.GetInt64(1), r.Text(2));
    }

    /// <summary>Pictures uploaded but never put in a post are removed after a day.</summary>
    public Task<int> DeleteOrphansAsync(CancellationToken ct) =>
        db.ExecAsync("delete from post_media where post_id is null and created_at < now() - interval '1 day'", ct);
}

public static class MediaEndpoints
{
    private const int FullSide = 1600, ThumbSide = 640;

    /// <summary>True for the request that carries a picture of a post: only this path may have a body bigger than a few kilobytes.</summary>
    public static bool IsUploadRequest(HttpRequest request) => HttpMethods.IsPost(request.Method) && request.Path.Value == "/v1/media";

    public static void MapMediaEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/v1/media").RequireFeed();
        group.MapPost("", UploadAsync).RequireAuthorization().RequireRateLimiting("heavy");
        group.MapGet("/{id:long}", GetAsync);
    }

    private static async Task<IResult> UploadAsync(
        MediaUpload upload, ClaimsPrincipal user, NpgsqlDataSource db, MediaStore store, IOptions<FeedOptions> options, CancellationToken ct)
    {
        FeedOptions feed = options.Value;
        long userId = user.UserId();
        if (!await FeedRules.HasAcceptedTermsAsync(db, userId, ct)) return FeedRules.Forbidden(FeedRules.TermsRequired);
        if (string.IsNullOrEmpty(upload.Data)) return FeedRules.Bad("Thiếu dữ liệu ảnh (data, base64).");
        if (upload.Data.Length > PhotoEndpoints.MaxBodyBytes) return FeedRules.Bad("Ảnh quá lớn (tối đa khoảng 6 MB).");

        long today = (long)(await db.ScalarAsync("select count(*) from post_media where uploaded_by = @u and created_at > now() - interval '1 day'", ct, ("u", userId)))!;
        if (today >= feed.MediaPerDay) return FeedRules.TooMany("Hôm nay bạn đã tải lên quá nhiều ảnh. Hãy thử lại vào ngày mai.");
        long orphans = (long)(await db.ScalarAsync("select count(*) from post_media where uploaded_by = @u and post_id is null", ct, ("u", userId)))!;
        if (orphans >= feed.MaxOrphanMedia) return FeedRules.Bad("Bạn có quá nhiều ảnh chưa đăng. Hãy đăng bài hoặc đợi ảnh cũ tự xóa.");

        byte[] source;
        try
        {
            source = Convert.FromBase64String(upload.Data);
        }
        catch (FormatException)
        {
            return FeedRules.Bad("Dữ liệu ảnh không phải base64 hợp lệ.");
        }

        (byte[]? full, int width, int height, string? problem) = PhotoEndpoints.Reencode(source, FullSide, 82);
        if (full == null) return FeedRules.Bad(problem!);
        (byte[]? thumb, _, _, string? thumbProblem) = PhotoEndpoints.Reencode(source, ThumbSide, 72);
        if (thumb == null) return FeedRules.Bad(thumbProblem!);

        long id = await store.SaveAsync(userId, full, thumb, width, height, ct);
        if (Random.Shared.Next(50) == 0) await store.DeleteOrphansAsync(ct);   // cheap housekeeping instead of a background service
        return Results.Json(new { id, width, height }, statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> GetAsync(long id, string? size, ClaimsPrincipal user, MediaStore store, CancellationToken ct)
    {
        var found = await store.ReadAsync(id, thumb: size != "full", ct);
        if (found == null) return FeedRules.NotFound("Không tìm thấy ảnh.");
        bool allowed = found.Value.PostStatus == "visible"
                       || (user.Identity?.IsAuthenticated == true && (user.IsInRole("admin") || user.UserId() == found.Value.UploadedBy));
        if (!allowed) return FeedRules.NotFound("Không tìm thấy ảnh.");
        return Results.Ok(new { id, contentType = "image/jpeg", data = Convert.ToBase64String(found.Value.Data) });
    }
}
