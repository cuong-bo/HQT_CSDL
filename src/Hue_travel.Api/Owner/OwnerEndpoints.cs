using System.Security.Claims;
using System.Text.Json;
using Hue_travel.Api.Audit;
using Hue_travel.Api.Data;
using Hue_travel.Api.Pois;
using Npgsql;

namespace Hue_travel.Api.Owner;

public sealed record ClaimRequest(string? PoiId);
public sealed record ClaimDto(string PoiId, string PoiName, string Status, DateTimeOffset CreatedAt);
public sealed record EditRequestBody(Dictionary<string, string?>? Changes);
public sealed record AnnouncementRequest(string? Title, string? Body, DateTimeOffset? EndsAt);
public sealed record ReplyRequest(string? Reply);
public sealed record StatsDay(DateOnly Day, int Views, int RouteAdds);
public sealed record StatsDto(IReadOnlyList<StatsDay> Days, double AverageRating, long ReviewCount);

/// <summary>
/// What a place owner can do. An owner never edits the data directly: changes go through an edit request that an admin
/// approves. Ownership is checked in the database on every call (the role in the token can be up to 15 minutes old).
/// </summary>
public static class OwnerEndpoints
{
    // JSON name -> (database column, longest allowed value)
    public static readonly IReadOnlyDictionary<string, (string Column, int Max)> Editable = new Dictionary<string, (string, int)>
    {
        ["address"] = ("dia_chi", 300),
        ["openingHours"] = ("gio_mo_cua", 300),
        ["priceText"] = ("gia_text", 300),
        ["phone"] = ("sdt", 60),
        ["website"] = ("website", 300),
        ["summary"] = ("mo_ta", 1000),
    };

    private const int MaxPendingPerPlace = 5;
    private const int MaxReplyLength = 1000;
    private const int MaxActiveAnnouncements = 10;

    private static IResult Bad(string detail) => Results.Problem(detail: detail, statusCode: StatusCodes.Status400BadRequest);

    private static IResult NotOwner() => Results.Problem(detail: "Bạn không phải chủ quản địa điểm này.", statusCode: StatusCodes.Status403Forbidden);

    public static void MapOwnerEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder owner = app.MapGroup("/v1/owner").RequireAuthorization();
        owner.MapPost("/claims", ClaimAsync);
        owner.MapGet("/claims", ListClaimsAsync);
        owner.MapGet("/places", ListPlacesAsync);
        owner.MapPost("/places/{poiId}/edit-requests", RequestEditAsync);
        owner.MapPost("/places/{poiId}/announcements", AnnounceAsync);
        owner.MapPost("/places/{poiId}/reviews/{reviewId:long}/reply", ReplyAsync);
        owner.MapGet("/places/{poiId}/stats", StatsAsync);
    }

    private static Task<bool> IsOwnerAsync(NpgsqlDataSource db, long userId, string poiId, CancellationToken ct) =>
        db.ExistsAsync("select exists (select 1 from place_owner where user_id = @u and poi_id = @p and status = 'approved')", ct, ("u", userId), ("p", poiId));

    /// <summary>Asks to become an owner of a place; an admin checks the claim outside the system (a phone call, papers) and decides.</summary>
    private static async Task<IResult> ClaimAsync(ClaimRequest request, ClaimsPrincipal user, NpgsqlDataSource db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.PoiId)) return Bad("Thiếu poiId.");
        if (!await db.ExistsAsync("select exists (select 1 from poi where id = @p and deleted_at is null)", ct, ("p", request.PoiId)))
            return Results.Problem(detail: "Không tìm thấy địa điểm.", statusCode: StatusCodes.Status404NotFound);
        int rows = await db.ExecAsync("insert into place_owner (user_id, poi_id) values (@u, @p) on conflict do nothing", ct, ("u", user.UserId()), ("p", request.PoiId));
        if (rows == 0) return Results.Problem(detail: "Bạn đã gửi yêu cầu cho địa điểm này.", statusCode: StatusCodes.Status409Conflict);
        await db.RecordAsync(user.UserId(), "claim.create", "place_owner", request.PoiId, null, new { poiId = request.PoiId }, ct);
        return Results.Json(new { status = "pending" }, statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> ListClaimsAsync(ClaimsPrincipal user, NpgsqlDataSource db, CancellationToken ct) =>
        Results.Ok(await db.ListAsync(
            "select o.poi_id, p.ten, o.status, o.created_at from place_owner o join poi p on p.id = o.poi_id where o.user_id = @u order by o.created_at desc",
            r => new ClaimDto(r.GetString(0), r.GetString(1), r.GetString(2), r.GetFieldValue<DateTimeOffset>(3)), ct, ("u", user.UserId())));

    private static async Task<IResult> ListPlacesAsync(ClaimsPrincipal user, PoiRepository pois, CancellationToken ct) =>
        Results.Ok(await pois.OwnedAsync(user.UserId(), ct));

    private static async Task<IResult> RequestEditAsync(string poiId, EditRequestBody body, ClaimsPrincipal user, NpgsqlDataSource db, CancellationToken ct)
    {
        long userId = user.UserId();
        if (!await IsOwnerAsync(db, userId, poiId, ct)) return NotOwner();
        if (body.Changes is not { Count: > 0 }) return Bad("Chưa có thay đổi nào.");

        var columns = new Dictionary<string, string>();
        foreach ((string key, string? value) in body.Changes)
        {
            if (!Editable.TryGetValue(key, out (string Column, int Max) rule))
                return Bad($"Không được sửa trường '{key}'. Được sửa: {string.Join(", ", Editable.Keys)}.");
            if (value == null || value.Length > rule.Max) return Bad($"Giá trị của '{key}' phải có và tối đa {rule.Max} ký tự.");
            if (key == "website" && !InputRules.IsHttpUrlOrEmpty(value)) return Bad("Website phải là địa chỉ http:// hoặc https://.");
            columns[rule.Column] = value.Trim();
        }
        if ((long)(await db.ScalarAsync("select count(*) from poi_edit_request where poi_id = @p and user_id = @u and status = 'pending'", ct, ("p", poiId), ("u", userId)))! >= MaxPendingPerPlace)
            return Bad($"Đang có {MaxPendingPerPlace} yêu cầu chờ duyệt cho địa điểm này.");

        object? id = await db.ScalarAsync("insert into poi_edit_request (poi_id, user_id, changes) values (@p, @u, @c::jsonb) returning id", ct,
            ("p", poiId), ("u", userId), ("c", JsonSerializer.Serialize(columns)));
        await db.RecordAsync(userId, "edit_request.create", "poi_edit_request", id?.ToString(), null, columns, ct);
        return Results.Json(new { id, status = "pending" }, statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> AnnounceAsync(string poiId, AnnouncementRequest request, ClaimsPrincipal user, NpgsqlDataSource db, CancellationToken ct)
    {
        long userId = user.UserId();
        if (!await IsOwnerAsync(db, userId, poiId, ct)) return NotOwner();
        string title = request.Title?.Trim() ?? "", text = request.Body?.Trim() ?? "";
        if (title.Length is < 1 or > 120) return Bad("Tiêu đề từ 1 đến 120 ký tự.");
        if (text.Length is < 1 or > 2000) return Bad("Nội dung từ 1 đến 2000 ký tự.");
        if (request.EndsAt is { } end && end <= DateTimeOffset.UtcNow) return Bad("Thời điểm kết thúc phải ở tương lai.");
        if ((long)(await db.ScalarAsync("select count(*) from announcement where poi_id = @p and (ends_at is null or ends_at > now())", ct, ("p", poiId)))! >= MaxActiveAnnouncements)
            return Bad($"Mỗi địa điểm có tối đa {MaxActiveAnnouncements} thông báo đang hiệu lực.");

        object? id = await db.ScalarAsync("insert into announcement (poi_id, title, body, ends_at, created_by) values (@p, @t, @b, @e, @u) returning id", ct,
            ("p", poiId), ("t", title), ("b", text), ("e", request.EndsAt), ("u", userId));
        await db.RecordAsync(userId, "announcement.create", "announcement", id?.ToString(), null, new { poiId, title }, ct);
        return Results.Json(new { id }, statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> ReplyAsync(string poiId, long reviewId, ReplyRequest request, ClaimsPrincipal user, NpgsqlDataSource db, CancellationToken ct)
    {
        long userId = user.UserId();
        if (!await IsOwnerAsync(db, userId, poiId, ct)) return NotOwner();
        string reply = request.Reply?.Trim() ?? "";
        if (reply.Length is < 1 or > MaxReplyLength) return Bad($"Phản hồi từ 1 đến {MaxReplyLength} ký tự.");
        int rows = await db.ExecAsync("update review set owner_reply = @r where id = @i and poi_id = @p", ct, ("r", reply), ("i", reviewId), ("p", poiId));
        if (rows == 0) return Results.Problem(detail: "Không tìm thấy đánh giá.", statusCode: StatusCodes.Status404NotFound);
        await db.RecordAsync(userId, "review.reply", "review", reviewId.ToString(), null, new { reply }, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> StatsAsync(string poiId, ClaimsPrincipal user, NpgsqlDataSource db, CancellationToken ct, int days = 30)
    {
        if (!await IsOwnerAsync(db, user.UserId(), poiId, ct)) return NotOwner();
        days = Math.Clamp(days, 1, 90);
        List<StatsDay> rows = await db.ListAsync(
            "select day, views, route_adds from poi_stats_daily where poi_id = @p and day > current_date - @d order by day",
            r => new StatsDay(DateOnly.FromDateTime(r.GetDateTime(0)), r.GetInt32(1), r.GetInt32(2)), ct, ("p", poiId), ("d", days));
        double average = 0;
        long count = 0;
        await using (NpgsqlCommand cmd = db.Command("select coalesce(avg(rating), 0), count(*) from review where poi_id = @p and status = 'visible'", [("p", poiId)]))
        await using (NpgsqlDataReader r = await cmd.ExecuteReaderAsync(ct))
        {
            if (await r.ReadAsync(ct))
            {
                average = Math.Round(r.GetDouble(0), 2);
                count = r.GetInt64(1);
            }
        }
        return Results.Ok(new StatsDto(rows, average, count));
    }
}
