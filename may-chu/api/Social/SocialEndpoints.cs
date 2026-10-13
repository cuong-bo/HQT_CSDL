using System.Security.Claims;
using Hue_travel.Api.Audit;
using Hue_travel.Api.Auth;
using Hue_travel.Api.Data;
using Hue_travel.Api.Pois;
using Npgsql;

namespace Hue_travel.Api.Social;

public sealed record ProfileRequest(string? DisplayName);
public sealed record PasswordRequest(string? CurrentPassword, string? NewPassword);
public sealed record ItineraryRequest(string? Name, string[]? PoiIds);
public sealed record ItineraryDto(long Id, string Name, string[] PoiIds);
public sealed record ReviewRequest(int Rating, string? Comment);
public sealed record ReviewDto(long Id, int Rating, string? Comment, string Author, DateTimeOffset CreatedAt, string? OwnerReply, bool Mine = false);
public sealed record MyReviewDto(string PoiId, string PoiName, int Rating, string? Comment, DateTimeOffset CreatedAt, string? OwnerReply, string Status);
public sealed record ReviewList(double Average, long Count, IReadOnlyList<ReviewDto> Items);
public sealed record ReportRequest(string? Kind, string? Content);
public sealed record AnnouncementDto(long Id, string Title, string Body, DateTimeOffset StartsAt, DateTimeOffset? EndsAt);

/// <summary>What a signed-in user can do: profile, password, favourites, itineraries, reviews and reports.</summary>
public static class SocialEndpoints
{
    private const int MaxItineraries = 20;
    private const int MaxItineraryItems = 30;
    private const int MaxComment = 1000;
    private const int MaxOpenReports = 20;
    private static readonly HashSet<string> ReportKinds = ["wrong_info", "closed", "wrong_location", "other"];

    private static IResult Bad(string detail) => Results.Problem(detail: detail, statusCode: StatusCodes.Status400BadRequest);

    private static IResult NotFound(string detail = "Không tìm thấy.") => Results.Problem(detail: detail, statusCode: StatusCodes.Status404NotFound);

    public static void MapSocialEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder me = app.MapGroup("/v1").RequireAuthorization();
        me.MapPut("/me", UpdateProfileAsync);
        me.MapPost("/me/password", ChangePasswordAsync).RequireRateLimiting("auth");
        me.MapGet("/favorites", ListFavoritesAsync);
        me.MapPut("/favorites/{poiId}", AddFavoriteAsync);
        me.MapDelete("/favorites/{poiId}", RemoveFavoriteAsync);
        me.MapGet("/itineraries", ListItinerariesAsync);
        me.MapPost("/itineraries", CreateItineraryAsync);
        me.MapPut("/itineraries/{id:long}", UpdateItineraryAsync);
        me.MapDelete("/itineraries/{id:long}", DeleteItineraryAsync);
        me.MapPost("/pois/{poiId}/reviews", SaveReviewAsync);
        me.MapDelete("/pois/{poiId}/reviews", DeleteReviewAsync);
        me.MapGet("/me/reviews", ListMyReviewsAsync);
        me.MapPost("/pois/{poiId}/reports", ReportAsync);

        app.MapGet("/v1/pois/{poiId}/reviews", ListReviewsAsync);
        app.MapGet("/v1/pois/{poiId}/announcements", ListAnnouncementsAsync);
    }

    private static async Task<IResult> UpdateProfileAsync(ProfileRequest request, ClaimsPrincipal user, NpgsqlDataSource db, CancellationToken ct)
    {
        string name = request.DisplayName?.Trim() ?? "";
        if (name.Length is < 1 or > AuthValidation.MaxDisplayNameLength) return Bad("Tên hiển thị từ 1 đến 60 ký tự.");
        await db.ExecAsync("update app_user set display_name = @n where id = @id", ct, ("n", name), ("id", user.UserId()));
        return Results.NoContent();
    }

    /// <summary>Changes the password (this is also how a temporary password is replaced). Every refresh token is revoked.</summary>
    private static async Task<IResult> ChangePasswordAsync(PasswordRequest request, ClaimsPrincipal principal, UserRepository users, CancellationToken ct)
    {
        AppUser? user = await users.FindByIdAsync(principal.UserId(), ct);
        if (user == null) return NotFound("Tài khoản không còn tồn tại.");
        if (string.IsNullOrEmpty(request.CurrentPassword) || !PasswordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            await users.RecordFailedLoginAsync(user.Id, 5, 15, ct);   // guessing the current password counts like a failed login
            return Results.Problem(detail: "Mật khẩu hiện tại không đúng.", statusCode: StatusCodes.Status403Forbidden);
        }
        string? problem = AuthValidation.ValidateRegistration(user.Username, request.NewPassword, null);
        if (problem != null) return Bad(problem);

        await users.SetPasswordAsync(user.Id, PasswordHasher.Hash(request.NewPassword!), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ListFavoritesAsync(ClaimsPrincipal user, PoiRepository pois, CancellationToken ct) =>
        Results.Ok(await pois.FavoritesAsync(user.UserId(), ct));

    private static async Task<IResult> AddFavoriteAsync(string poiId, ClaimsPrincipal user, NpgsqlDataSource db, CancellationToken ct)
    {
        if (!await PoiExistsAsync(db, poiId, ct)) return NotFound("Không tìm thấy địa điểm.");
        await db.ExecAsync("insert into favorite (user_id, poi_id) values (@u, @p) on conflict do nothing", ct, ("u", user.UserId()), ("p", poiId));
        return Results.NoContent();
    }

    private static async Task<IResult> RemoveFavoriteAsync(string poiId, ClaimsPrincipal user, NpgsqlDataSource db, CancellationToken ct)
    {
        await db.ExecAsync("delete from favorite where user_id = @u and poi_id = @p", ct, ("u", user.UserId()), ("p", poiId));
        return Results.NoContent();
    }

    private static async Task<IResult> ListItinerariesAsync(ClaimsPrincipal user, NpgsqlDataSource db, CancellationToken ct) =>
        Results.Ok(await db.ListAsync(
            "select i.id, i.name, coalesce(array_agg(t.poi_id order by t.position) filter (where t.poi_id is not null), '{}') " +
            "from itinerary i left join itinerary_item t on t.itinerary_id = i.id where i.user_id = @u group by i.id order by i.updated_at desc",
            r => new ItineraryDto(r.GetInt64(0), r.GetString(1), r.GetFieldValue<string[]>(2)), ct, ("u", user.UserId())));

    private static async Task<IResult> CreateItineraryAsync(ItineraryRequest request, ClaimsPrincipal user, NpgsqlDataSource db, CancellationToken ct)
    {
        (string? problem, string name, string[] ids) = ValidateItinerary(request);
        if (problem != null) return Bad(problem);
        long userId = user.UserId();
        if ((long)(await db.ScalarAsync("select count(*) from itinerary where user_id = @u", ct, ("u", userId)))! >= MaxItineraries)
            return Bad($"Mỗi người lưu tối đa {MaxItineraries} lộ trình.");
        if (!await AllExistAsync(db, ids, ct)) return NotFound("Có địa điểm không tồn tại.");

        await using NpgsqlConnection conn = await db.OpenConnectionAsync(ct);
        await using NpgsqlTransaction tx = await conn.BeginTransactionAsync(ct);
        await using var insert = new NpgsqlCommand("insert into itinerary (user_id, name) values (@u, @n) returning id", conn, tx);
        insert.Parameters.AddWithValue("u", userId);
        insert.Parameters.AddWithValue("n", name);
        long id = (long)(await insert.ExecuteScalarAsync(ct))!;
        await ReplaceItemsAsync(conn, tx, id, ids, ct);
        await tx.CommitAsync(ct);
        return Results.Json(new ItineraryDto(id, name, ids), statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> UpdateItineraryAsync(long id, ItineraryRequest request, ClaimsPrincipal user, NpgsqlDataSource db, CancellationToken ct)
    {
        (string? problem, string name, string[] ids) = ValidateItinerary(request);
        if (problem != null) return Bad(problem);
        if (!await AllExistAsync(db, ids, ct)) return NotFound("Có địa điểm không tồn tại.");

        await using NpgsqlConnection conn = await db.OpenConnectionAsync(ct);
        await using NpgsqlTransaction tx = await conn.BeginTransactionAsync(ct);
        await using var update = new NpgsqlCommand("update itinerary set name = @n, updated_at = now() where id = @i and user_id = @u", conn, tx);
        update.Parameters.AddWithValue("n", name);
        update.Parameters.AddWithValue("i", id);
        update.Parameters.AddWithValue("u", user.UserId());
        if (await update.ExecuteNonQueryAsync(ct) == 0) return NotFound("Không tìm thấy lộ trình.");
        await ReplaceItemsAsync(conn, tx, id, ids, ct);
        await tx.CommitAsync(ct);
        return Results.Ok(new ItineraryDto(id, name, ids));
    }

    private static async Task<IResult> DeleteItineraryAsync(long id, ClaimsPrincipal user, NpgsqlDataSource db, CancellationToken ct)
    {
        int rows = await db.ExecAsync("delete from itinerary where id = @i and user_id = @u", ct, ("i", id), ("u", user.UserId()));
        return rows == 0 ? NotFound("Không tìm thấy lộ trình.") : Results.NoContent();
    }

    private static async Task<IResult> SaveReviewAsync(string poiId, ReviewRequest request, ClaimsPrincipal user, NpgsqlDataSource db, CancellationToken ct)
    {
        if (request.Rating is < 1 or > 5) return Bad("Điểm đánh giá từ 1 đến 5.");
        if (request.Comment is { Length: > MaxComment }) return Bad($"Nhận xét tối đa {MaxComment} ký tự.");
        if (!await PoiExistsAsync(db, poiId, ct)) return NotFound("Không tìm thấy địa điểm.");
        // An owner rating their own place would skew its average; they answer reviews instead.
        if (await db.ExistsAsync("select exists (select 1 from place_owner where user_id = @u and poi_id = @p and status = 'approved')", ct, ("u", user.UserId()), ("p", poiId)))
            return Results.Problem(detail: "Chủ quản không đánh giá địa điểm của chính mình.", statusCode: StatusCodes.Status403Forbidden);
        await db.ExecAsync(
            "insert into review (user_id, poi_id, rating, comment) values (@u, @p, @r, @c) " +
            "on conflict (user_id, poi_id) do update set rating = excluded.rating, comment = excluded.comment, created_at = now()", ct,
            ("u", user.UserId()), ("p", poiId), ("r", (short)request.Rating), ("c", request.Comment?.Trim()));
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteReviewAsync(string poiId, ClaimsPrincipal user, NpgsqlDataSource db, CancellationToken ct)
    {
        int rows = await db.ExecAsync("delete from review where user_id = @u and poi_id = @p", ct, ("u", user.UserId()), ("p", poiId));
        return rows == 0 ? NotFound("Bạn chưa đánh giá địa điểm này.") : Results.NoContent();
    }

    private static async Task<IResult> ListMyReviewsAsync(ClaimsPrincipal user, NpgsqlDataSource db, CancellationToken ct) =>
        Results.Ok(await db.ListAsync(
            "select r.poi_id, p.ten, r.rating, r.comment, r.created_at, r.owner_reply, r.status from review r join poi p on p.id = r.poi_id " +
            "where r.user_id = @u and p.deleted_at is null order by r.created_at desc limit 100",
            r => new MyReviewDto(r.GetString(0), r.GetString(1), r.GetInt16(2), r.Text(3), r.GetFieldValue<DateTimeOffset>(4), r.Text(5), r.GetString(6)), ct, ("u", user.UserId())));

    private static async Task<IResult> ReportAsync(string poiId, ReportRequest request, ClaimsPrincipal user, NpgsqlDataSource db, CancellationToken ct)
    {
        if (request.Kind == null || !ReportKinds.Contains(request.Kind)) return Bad("kind phải là: " + string.Join(", ", ReportKinds));
        if (request.Content is { Length: > MaxComment }) return Bad($"Nội dung tối đa {MaxComment} ký tự.");
        if (!await PoiExistsAsync(db, poiId, ct)) return NotFound("Không tìm thấy địa điểm.");
        if ((long)(await db.ScalarAsync("select count(*) from report where user_id = @u and status = 'open'", ct, ("u", user.UserId())))! >= MaxOpenReports)
            return Bad($"Bạn đang có {MaxOpenReports} báo cáo chưa được xử lý.");
        await db.ExecAsync("insert into report (user_id, poi_id, kind, content) values (@u, @p, @k, @c)", ct,
            ("u", user.UserId()), ("p", poiId), ("k", request.Kind), ("c", request.Content?.Trim()));
        return Results.Json(new { status = "received" }, statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> ListReviewsAsync(string poiId, ClaimsPrincipal user, NpgsqlDataSource db, CancellationToken ct)
    {
        double average = 0;
        long count = 0;
        // Anyone may read; a signed-in reader also learns which review is theirs (their own comes first).
        long? me = user.Identity?.IsAuthenticated == true ? user.UserId() : null;
        List<ReviewDto> items = await db.ListAsync(
            "select r.id, r.rating, r.comment, u.display_name, r.created_at, r.owner_reply, avg(r.rating) over (), count(*) over (), r.user_id = @me " +
            "from review r join app_user u on u.id = r.user_id where r.poi_id = @p and r.status = 'visible' " +
            "order by (r.user_id = @me) desc nulls last, r.created_at desc limit 50",
            r =>
            {
                average = r.GetDouble(6);
                count = r.GetInt64(7);
                return new ReviewDto(r.GetInt64(0), r.GetInt16(1), r.Text(2), r.GetString(3), r.GetFieldValue<DateTimeOffset>(4), r.Text(5), !r.IsDBNull(8) && r.GetBoolean(8));
            }, ct, ("p", poiId), ("me", me));
        return Results.Ok(new ReviewList(Math.Round(average, 2), count, items));
    }

    private static async Task<IResult> ListAnnouncementsAsync(string poiId, NpgsqlDataSource db, CancellationToken ct) =>
        Results.Ok(await db.ListAsync(
            "select id, title, body, starts_at, ends_at from announcement where poi_id = @p and starts_at <= now() " +
            "and (ends_at is null or ends_at > now()) order by starts_at desc limit 20",
            r => new AnnouncementDto(r.GetInt64(0), r.GetString(1), r.GetString(2), r.GetFieldValue<DateTimeOffset>(3),
                r.IsDBNull(4) ? null : r.GetFieldValue<DateTimeOffset>(4)), ct, ("p", poiId)));

    private static Task<bool> PoiExistsAsync(NpgsqlDataSource db, string poiId, CancellationToken ct) =>
        db.ExistsAsync("select exists (select 1 from poi where id = @p and deleted_at is null and status = 'visible')", ct, ("p", poiId));

    private static async Task<bool> AllExistAsync(NpgsqlDataSource db, string[] ids, CancellationToken ct) =>
        ids.Length == 0 || (long)(await db.ScalarAsync(
            "select count(*) from poi where id = any(@ids) and deleted_at is null and status = 'visible'", ct, ("ids", ids)))! == ids.Length;

    private static (string? Problem, string Name, string[] Ids) ValidateItinerary(ItineraryRequest request)
    {
        string name = request.Name?.Trim() ?? "";
        string[] ids = request.PoiIds ?? [];
        if (name.Length is < 1 or > 80) return ("Tên lộ trình từ 1 đến 80 ký tự.", name, ids);
        if (ids.Length > MaxItineraryItems) return ($"Tối đa {MaxItineraryItems} địa điểm.", name, ids);
        if (ids.Distinct().Count() != ids.Length) return ("Các địa điểm không được trùng nhau.", name, ids);
        return (null, name, ids);
    }

    private static async Task ReplaceItemsAsync(NpgsqlConnection conn, NpgsqlTransaction tx, long itineraryId, string[] ids, CancellationToken ct)
    {
        await using var delete = new NpgsqlCommand("delete from itinerary_item where itinerary_id = @i", conn, tx);
        delete.Parameters.AddWithValue("i", itineraryId);
        await delete.ExecuteNonQueryAsync(ct);
        if (ids.Length == 0) return;
        await using var insert = new NpgsqlCommand(
            "insert into itinerary_item (itinerary_id, position, poi_id) select @i, ord::int, pid from unnest(@ids) with ordinality as t(pid, ord)", conn, tx);
        insert.Parameters.AddWithValue("i", itineraryId);
        insert.Parameters.AddWithValue("ids", ids);
        await insert.ExecuteNonQueryAsync(ct);
    }
}
