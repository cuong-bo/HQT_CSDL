using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Hue_travel.Api.Audit;
using Hue_travel.Api.Auth;
using Hue_travel.Api.Data;
using Npgsql;

namespace Hue_travel.Api.Admin;

public sealed record AdminUserDto(long Id, string Username, string DisplayName, string Role, string Status, DateTimeOffset CreatedAt, DateTimeOffset? LastLoginAt,
    int OwnedPlaces = 0, int PendingClaims = 0);
public sealed record AdminUserUpdate(string? Role, string? Status);
public sealed record AdminCreateUser(string? Username, string? Password, string? DisplayName, string? Role);
public sealed record DecisionRequest(bool Approve, string? Note);
public sealed record StatusRequest(string? Status);
public sealed record PoiPatch(
    string? Name, string? Group, string? Type, string? Address, string? OpeningHours, string? PriceText, string? Phone,
    string? Website, string? Summary, string? Status, double? Lon, double? Lat);
public sealed record PoiCreate(
    string? Id, string? Group, string? Name, double? Lon, double? Lat, string? Type, string? Address, string? OpeningHours,
    string? PriceText, string? Phone, string? Website, string? Summary);

/// <summary>Everything an admin can do. Each change of shared data is written to the audit log.</summary>
public static partial class AdminEndpoints
{
    private const int PageSize = 50;
    private const string TempPasswordAlphabet = "abcdefghijkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private static readonly HashSet<string> Roles = ["user", "owner", "admin"];
    private static readonly HashSet<string> UserStatuses = ["active", "locked", "must_change_password"];
    private static readonly HashSet<string> Groups = ["heritage", "scenery", "food", "stay", "service", "transport"];
    private static readonly HashSet<string> PoiStatuses = ["visible", "hidden", "closed"];

    [GeneratedRegex("^[A-Za-z0-9_-]{2,20}$")]
    private static partial Regex PoiIdPattern();

    private static IResult Bad(string detail) => Results.Problem(detail: detail, statusCode: StatusCodes.Status400BadRequest);

    private static IResult NotFound(string detail) => Results.Problem(detail: detail, statusCode: StatusCodes.Status404NotFound);

    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder admin = app.MapGroup("/v1/admin").RequireAuthorization("admin");
        admin.MapGet("/users", ListUsersAsync);
        admin.MapPost("/users", CreateUserAsync);
        admin.MapPut("/users/{id:long}", UpdateUserAsync);
        admin.MapPost("/users/{id:long}/reset-password", ResetPasswordAsync);
        admin.MapGet("/claims", ListClaimsAsync);
        admin.MapPost("/claims/{userId:long}/{poiId}/decision", DecideClaimAsync);
        admin.MapGet("/edit-requests", ListEditRequestsAsync);
        admin.MapPost("/edit-requests/{id:long}/decision", DecideEditRequestAsync);
        admin.MapGet("/reviews", ListReviewsAsync);
        admin.MapPut("/reviews/{id:long}", SetReviewStatusAsync);
        admin.MapGet("/reports", ListReportsAsync);
        admin.MapPost("/reports/{id:long}/resolve", ResolveReportAsync);
        admin.MapPost("/pois", CreatePoiAsync);
        admin.MapPut("/pois/{id}", UpdatePoiAsync);
        admin.MapDelete("/pois/{id}", DeletePoiAsync);
        admin.MapGet("/audit", ListAuditAsync);
        admin.MapGet("/stats", StatsAsync);
        MapPlaceAndAccountEndpoints(admin);
    }

    // ------------------------------------------------------------ users
    private static async Task<IResult> ListUsersAsync(NpgsqlDataSource db, CancellationToken ct, string? q = null, string? role = null, string? status = null, int page = 1)
    {
        if (role != null && !Roles.Contains(role)) return Bad("role phải là user, owner hoặc admin.");
        if (status != null && !UserStatuses.Contains(status)) return Bad("status phải là active, locked hoặc must_change_password.");
        return Results.Ok(await db.ListAsync(
            "select u.id, u.username::text, u.display_name, u.role, u.status, u.created_at, u.last_login_at, " +
            "(select count(*) from place_owner o where o.user_id = u.id and o.status = 'approved')::int, " +
            "(select count(*) from place_owner o where o.user_id = u.id and o.status = 'pending')::int from app_user u " +
            "where (@q::text is null or u.username::text ilike '%' || @q::text || '%' or u.display_name ilike '%' || @q::text || '%') " +
            "and (@r::text is null or u.role = @r::text) and (@s::text is null or u.status = @s::text) " +
            "order by u.id limit @n offset @o",
            r => new AdminUserDto(r.GetInt64(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4), r.GetFieldValue<DateTimeOffset>(5),
                r.IsDBNull(6) ? null : r.GetFieldValue<DateTimeOffset>(6), r.GetInt32(7), r.GetInt32(8)), ct,
            ("q", string.IsNullOrWhiteSpace(q) ? null : q.Trim().Replace("%", "").Replace("_", "")), ("r", role), ("s", status), ("n", PageSize), ("o", (Math.Max(page, 1) - 1) * PageSize)));
    }

    /// <summary>Creates an account (for an owner, or another admin) with a password the admin chooses; the person must change it at first login.</summary>
    private static async Task<IResult> CreateUserAsync(AdminCreateUser request, ClaimsPrincipal admin, UserRepository users, NpgsqlDataSource db, CancellationToken ct)
    {
        string role = request.Role ?? "owner";
        if (!Roles.Contains(role)) return Bad("role phải là user, owner hoặc admin.");
        string? problem = AuthValidation.ValidateRegistration(request.Username, request.Password, request.DisplayName);
        if (problem != null) return Bad(problem);

        string username = request.Username!.Trim();
        AppUser? created = await users.CreateAsync(username, PasswordHasher.Hash(request.Password!),
            AuthValidation.DisplayNameOrUsername(request.DisplayName, username), ct, role, "must_change_password");
        if (created == null) return Results.Problem(detail: "Tên đăng nhập đã có người dùng.", statusCode: StatusCodes.Status409Conflict);
        await db.RecordAsync(admin.UserId(), "user.create", "app_user", created.Id.ToString(), null, new { username, role }, ct);
        return Results.Json(new UserDto(created.Id, created.Username, created.DisplayName, created.Role), statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> UpdateUserAsync(long id, AdminUserUpdate update, ClaimsPrincipal admin, UserRepository users, NpgsqlDataSource db, CancellationToken ct)
    {
        if (update.Role != null && !Roles.Contains(update.Role)) return Bad("role phải là user, owner hoặc admin.");
        if (update.Status != null && !UserStatuses.Contains(update.Status)) return Bad("status phải là active, locked hoặc must_change_password.");
        if (id == admin.UserId()) return Bad("Không tự đổi vai trò hay trạng thái của chính mình.");
        AppUser? before = await users.FindByIdAsync(id, ct);
        if (before == null) return NotFound("Không tìm thấy người dùng.");

        await db.ExecAsync("update app_user set role = coalesce(@r, role), status = coalesce(@s, status) where id = @id", ct,
            ("r", update.Role), ("s", update.Status), ("id", id));
        // Back to an ordinary user: the places they ran (or asked to run) stop being theirs, otherwise the owner screens would still answer.
        if (update.Role == "user")
            await db.ExecAsync("update place_owner set status = 'rejected', reviewed_by = @a, reviewed_at = now() where user_id = @id and status in ('approved', 'pending')", ct,
                ("a", admin.UserId()), ("id", id));
        if (update.Status == "locked") await users.RevokeRefreshTokensAsync(id, ct);
        await db.RecordAsync(admin.UserId(), "user.update", "app_user", id.ToString(), new { before.Role, before.Status }, update, ct);
        return Results.NoContent();
    }

    /// <summary>There is no "forgot password" e-mail: the admin sets a temporary password, shown once, and the user must change it.</summary>
    private static async Task<IResult> ResetPasswordAsync(long id, ClaimsPrincipal admin, UserRepository users, NpgsqlDataSource db, CancellationToken ct)
    {
        if (await users.FindByIdAsync(id, ct) == null) return NotFound("Không tìm thấy người dùng.");
        string temporary = RandomNumberGenerator.GetString(TempPasswordAlphabet, 12);
        await users.SetPasswordAsync(id, PasswordHasher.Hash(temporary), ct, mustChange: true);
        await db.RecordAsync(admin.UserId(), "user.reset_password", "app_user", id.ToString(), null, null, ct);
        return Results.Ok(new { temporaryPassword = temporary });
    }

    // ------------------------------------------------------------ owner claims
    private static async Task<IResult> ListClaimsAsync(NpgsqlDataSource db, CancellationToken ct, string status = "pending") =>
        Results.Ok(await db.ListAsync(
            "select o.user_id, u.username::text, o.poi_id, p.ten, o.status, o.created_at from place_owner o " +
            "join app_user u on u.id = o.user_id join poi p on p.id = o.poi_id where o.status = @s order by o.created_at limit @n",
            r => new { userId = r.GetInt64(0), username = r.GetString(1), poiId = r.GetString(2), poiName = r.GetString(3), status = r.GetString(4), createdAt = r.GetFieldValue<DateTimeOffset>(5) },
            ct, ("s", status), ("n", PageSize)));

    private static async Task<IResult> DecideClaimAsync(long userId, string poiId, DecisionRequest decision, ClaimsPrincipal admin, NpgsqlDataSource db, CancellationToken ct)
    {
        int rows = await db.ExecAsync(
            "update place_owner set status = @s, reviewed_by = @a, reviewed_at = now() where user_id = @u and poi_id = @p and status = 'pending'", ct,
            ("s", decision.Approve ? "approved" : "rejected"), ("a", admin.UserId()), ("u", userId), ("p", poiId));
        if (rows == 0) return NotFound("Không có yêu cầu đang chờ.");
        if (decision.Approve) await db.ExecAsync("update app_user set role = 'owner' where id = @u and role = 'user'", ct, ("u", userId));
        await db.RecordAsync(admin.UserId(), decision.Approve ? "claim.approve" : "claim.reject", "place_owner", poiId, null, new { userId, decision.Note }, ct);
        return Results.NoContent();
    }

    // ------------------------------------------------------------ edit requests
    private static async Task<IResult> ListEditRequestsAsync(NpgsqlDataSource db, CancellationToken ct, string status = "pending") =>
        Results.Ok(await db.ListAsync(
            "select e.id, e.poi_id, p.ten, u.username::text, e.changes::text, e.status, e.created_at from poi_edit_request e " +
            "join poi p on p.id = e.poi_id join app_user u on u.id = e.user_id where e.status = @s order by e.created_at limit @n",
            r => new { id = r.GetInt64(0), poiId = r.GetString(1), poiName = r.GetString(2), requestedBy = r.GetString(3), changes = JsonDocument.Parse(r.GetString(4)).RootElement.Clone(), status = r.GetString(5), createdAt = r.GetFieldValue<DateTimeOffset>(6) },
            ct, ("s", status), ("n", PageSize)));

    private const string ApplyChangesSql = @"
        with c as (select @c::jsonb as j)
        update poi set
            dia_chi    = case when jsonb_exists(c.j, 'dia_chi') then c.j->>'dia_chi' else dia_chi end,
            gio_mo_cua = case when jsonb_exists(c.j, 'gio_mo_cua') then c.j->>'gio_mo_cua' else gio_mo_cua end,
            gia_text   = case when jsonb_exists(c.j, 'gia_text') then c.j->>'gia_text' else gia_text end,
            sdt        = case when jsonb_exists(c.j, 'sdt') then c.j->>'sdt' else sdt end,
            website    = case when jsonb_exists(c.j, 'website') then c.j->>'website' else website end,
            mo_ta      = case when jsonb_exists(c.j, 'mo_ta') then c.j->>'mo_ta' else mo_ta end,
            gio_mo     = case when jsonb_exists(c.j, 'gio_mo_cua') then f_gio_mo(c.j->>'gio_mo_cua') else gio_mo end,
            gio_dong   = case when jsonb_exists(c.j, 'gio_mo_cua') then f_gio_dong(c.j->>'gio_mo_cua') else gio_dong end,
            gia_thap   = case when jsonb_exists(c.j, 'gia_text') then (f_gia_range(c.j->>'gia_text'))[1] else gia_thap end,
            gia_cao    = case when jsonb_exists(c.j, 'gia_text') then (f_gia_range(c.j->>'gia_text'))[2] else gia_cao end,
            version = version + 1, updated_by = @by, updated_at = now()
        from c where poi.id = @id and poi.deleted_at is null";

    private static async Task<IResult> DecideEditRequestAsync(long id, DecisionRequest decision, ClaimsPrincipal admin, NpgsqlDataSource db, CancellationToken ct)
    {
        long adminId = admin.UserId();
        await using NpgsqlConnection conn = await db.OpenConnectionAsync(ct);
        await using NpgsqlTransaction tx = await conn.BeginTransactionAsync(ct);

        string poiId, changes;
        await using (var claim = new NpgsqlCommand(
            "update poi_edit_request set status = @s, note = @n, reviewed_by = @a, reviewed_at = now() " +
            "where id = @i and status = 'pending' returning poi_id, changes::text", conn, tx))
        {
            claim.Parameters.AddWithValue("s", decision.Approve ? "approved" : "rejected");
            claim.Parameters.AddWithValue("n", (object?)decision.Note ?? DBNull.Value);
            claim.Parameters.AddWithValue("a", adminId);
            claim.Parameters.AddWithValue("i", id);
            await using NpgsqlDataReader r = await claim.ExecuteReaderAsync(ct);
            if (!await r.ReadAsync(ct)) return NotFound("Không có yêu cầu đang chờ.");
            poiId = r.GetString(0);
            changes = r.GetString(1);
        }

        JsonElement? before = null;
        if (decision.Approve)
        {
            await using (var old = new NpgsqlCommand(
                "select coalesce(jsonb_object_agg(e.key, to_jsonb(p) -> e.key), '{}')::text from poi p, jsonb_each(@c::jsonb) e where p.id = @id", conn, tx))
            {
                old.Parameters.AddWithValue("c", changes);
                old.Parameters.AddWithValue("id", poiId);
                before = JsonDocument.Parse((string)(await old.ExecuteScalarAsync(ct))!).RootElement.Clone();
            }
            await using var apply = new NpgsqlCommand(ApplyChangesSql, conn, tx);
            apply.Parameters.AddWithValue("c", changes);
            apply.Parameters.AddWithValue("by", adminId);
            apply.Parameters.AddWithValue("id", poiId);
            await apply.ExecuteNonQueryAsync(ct);
        }
        await tx.CommitAsync(ct);

        await db.RecordAsync(adminId, decision.Approve ? "edit_request.approve" : "edit_request.reject", "poi", poiId, before,
            JsonDocument.Parse(changes).RootElement.Clone(), ct);
        return Results.NoContent();
    }

    // ------------------------------------------------------------ moderation
    private static async Task<IResult> ListReviewsAsync(NpgsqlDataSource db, CancellationToken ct, string status = "visible") =>
        Results.Ok(await db.ListAsync(
            "select r.id, r.poi_id, r.rating, r.comment, u.username::text, r.status, r.created_at from review r join app_user u on u.id = r.user_id " +
            "where r.status = @s order by r.created_at desc limit @n",
            r => new { id = r.GetInt64(0), poiId = r.GetString(1), rating = (int)r.GetInt16(2), comment = r.Text(3), author = r.GetString(4), status = r.GetString(5), createdAt = r.GetFieldValue<DateTimeOffset>(6) },
            ct, ("s", status), ("n", PageSize)));

    private static async Task<IResult> SetReviewStatusAsync(long id, StatusRequest request, ClaimsPrincipal admin, NpgsqlDataSource db, CancellationToken ct)
    {
        if (request.Status is not ("visible" or "hidden")) return Bad("status phải là visible hoặc hidden.");
        int rows = await db.ExecAsync("update review set status = @s where id = @i", ct, ("s", request.Status), ("i", id));
        if (rows == 0) return NotFound("Không tìm thấy đánh giá.");
        await db.RecordAsync(admin.UserId(), "review.moderate", "review", id.ToString(), null, new { request.Status }, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ListReportsAsync(NpgsqlDataSource db, CancellationToken ct, string status = "open") =>
        Results.Ok(await db.ListAsync(
            "select r.id, r.poi_id, p.ten, r.kind, r.content, u.username::text, r.status, r.created_at from report r " +
            "join poi p on p.id = r.poi_id join app_user u on u.id = r.user_id where r.status = @s order by r.created_at limit @n",
            r => new { id = r.GetInt64(0), poiId = r.GetString(1), poiName = r.GetString(2), kind = r.GetString(3), content = r.Text(4), reporter = r.GetString(5), status = r.GetString(6), createdAt = r.GetFieldValue<DateTimeOffset>(7) },
            ct, ("s", status), ("n", PageSize)));

    private static async Task<IResult> ResolveReportAsync(long id, ClaimsPrincipal admin, NpgsqlDataSource db, CancellationToken ct)
    {
        int rows = await db.ExecAsync("update report set status = 'resolved', handled_by = @a where id = @i and status = 'open'", ct, ("a", admin.UserId()), ("i", id));
        if (rows == 0) return NotFound("Không có báo cáo đang mở.");
        await db.RecordAsync(admin.UserId(), "report.resolve", "report", id.ToString(), null, null, ct);
        return Results.NoContent();
    }

    // ------------------------------------------------------------ places
    private static async Task<IResult> CreatePoiAsync(PoiCreate request, ClaimsPrincipal admin, NpgsqlDataSource db, CancellationToken ct)
    {
        if (request.Id == null || !PoiIdPattern().IsMatch(request.Id)) return Bad("id gồm 2 đến 20 ký tự: chữ, số, gạch ngang hoặc gạch dưới.");
        if (request.Group == null || !Groups.Contains(request.Group)) return Bad("group không hợp lệ.");
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 200) return Bad("Tên từ 1 đến 200 ký tự.");
        if (!InputRules.IsHttpUrlOrEmpty(request.Website)) return Bad("Website phải là địa chỉ http:// hoặc https://.");
        if (request.Lon is not { } lon || request.Lat is not { } lat || lon is < 100 or > 112 || lat is < 8 or > 24)
            return Bad("lon và lat (độ) phải nằm trong lãnh thổ Việt Nam.");

        int rows = await db.ExecAsync(
            "insert into poi (id, nhom, loai_hinh, ten, dia_chi, gio_mo_cua, gio_mo, gio_dong, gia_text, gia_thap, gia_cao, sdt, website, mo_ta, updated_by, geom) " +
            "values (@id, @g, @t, @n, @a, @h, f_gio_mo(@h), f_gio_dong(@h), @p, (f_gia_range(@p))[1], (f_gia_range(@p))[2], @s, @w, @m, @by, " +
            "st_transform(st_setsrid(st_makepoint(@lon, @lat), 4326), 3405)) on conflict (id) do nothing", ct,
            ("id", request.Id), ("g", request.Group), ("t", request.Type), ("n", request.Name.Trim()), ("a", request.Address), ("h", request.OpeningHours),
            ("p", request.PriceText), ("s", request.Phone), ("w", request.Website), ("m", request.Summary), ("by", admin.UserId()), ("lon", lon), ("lat", lat));
        if (rows == 0) return Results.Problem(detail: "Mã địa điểm đã tồn tại.", statusCode: StatusCodes.Status409Conflict);

        await db.ScalarAsync("select snap_poi(@id)", ct, ("id", request.Id));
        await db.RecordAsync(admin.UserId(), "poi.create", "poi", request.Id, null, request, ct);
        return Results.Json(new { id = request.Id }, statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> UpdatePoiAsync(string id, PoiPatch patch, ClaimsPrincipal admin, NpgsqlDataSource db, CancellationToken ct)
    {
        if (patch.Group != null && !Groups.Contains(patch.Group)) return Bad("group không hợp lệ.");
        if (patch.Status != null && !PoiStatuses.Contains(patch.Status)) return Bad("status phải là visible, hidden hoặc closed.");
        if (patch.Name != null && (patch.Name.Trim().Length is < 1 or > 200)) return Bad("Tên từ 1 đến 200 ký tự.");
        if (!InputRules.IsHttpUrlOrEmpty(patch.Website)) return Bad("Website phải là địa chỉ http:// hoặc https://.");
        if ((patch.Lon == null) != (patch.Lat == null)) return Bad("Cần cả lon và lat để đổi vị trí.");
        if (patch.Lon is < 100 or > 112 || patch.Lat is < 8 or > 24) return Bad("lon và lat (độ) phải nằm trong lãnh thổ Việt Nam.");

        object? before = await db.ScalarAsync("select (to_jsonb(p) - 'geom' - 'ten_khong_dau')::text from poi p where id = @id and deleted_at is null", ct, ("id", id));
        if (before == null) return NotFound("Không tìm thấy địa điểm.");

        await db.ExecAsync(
            "update poi set ten = coalesce(@n, ten), nhom = coalesce(@g, nhom), loai_hinh = coalesce(@t, loai_hinh), dia_chi = coalesce(@a, dia_chi), " +
            "gio_mo_cua = coalesce(@h, gio_mo_cua), gia_text = coalesce(@p, gia_text), sdt = coalesce(@s, sdt), website = coalesce(@w, website), " +
            "mo_ta = coalesce(@m, mo_ta), status = coalesce(@st, status), " +
            "gio_mo = case when @h::text is not null then f_gio_mo(@h) else gio_mo end, gio_dong = case when @h::text is not null then f_gio_dong(@h) else gio_dong end, " +
            "gia_thap = case when @p::text is not null then (f_gia_range(@p))[1] else gia_thap end, gia_cao = case when @p::text is not null then (f_gia_range(@p))[2] else gia_cao end, " +
            "geom = case when @lon::float8 is not null then st_transform(st_setsrid(st_makepoint(@lon, @lat), 4326), 3405) else geom end, " +
            "version = version + 1, updated_by = @by, updated_at = now() where id = @id", ct,
            ("n", patch.Name?.Trim()), ("g", patch.Group), ("t", patch.Type), ("a", patch.Address), ("h", patch.OpeningHours), ("p", patch.PriceText),
            ("s", patch.Phone), ("w", patch.Website), ("m", patch.Summary), ("st", patch.Status), ("lon", patch.Lon), ("lat", patch.Lat),
            ("by", admin.UserId()), ("id", id));
        if (patch.Lon != null) await db.ScalarAsync("select snap_poi(@id)", ct, ("id", id));
        await db.RecordAsync(admin.UserId(), "poi.update", "poi", id, JsonDocument.Parse((string)before).RootElement.Clone(), patch, ct);
        return Results.NoContent();
    }

    /// <summary>Soft delete: the row stays (owner links and reviews hang on it) but it disappears from every list.</summary>
    private static async Task<IResult> DeletePoiAsync(string id, ClaimsPrincipal admin, NpgsqlDataSource db, CancellationToken ct)
    {
        int rows = await db.ExecAsync("update poi set deleted_at = now(), updated_by = @by, updated_at = now(), version = version + 1 where id = @id and deleted_at is null", ct,
            ("by", admin.UserId()), ("id", id));
        if (rows == 0) return NotFound("Không tìm thấy địa điểm.");
        await db.RecordAsync(admin.UserId(), "poi.delete", "poi", id, null, null, ct);
        return Results.NoContent();
    }

    // ------------------------------------------------------------ audit and numbers
    private static async Task<IResult> ListAuditAsync(NpgsqlDataSource db, CancellationToken ct, int page = 1, string? entity = null) =>
        Results.Ok(await db.ListAsync(
            "select a.id, a.at, u.username::text, a.action, a.entity, a.entity_id, a.old_value::text, a.new_value::text from audit_log a " +
            "left join app_user u on u.id = a.user_id where @e::text is null or a.entity = @e::text order by a.id desc limit @n offset @o",
            r => new
            {
                id = r.GetInt64(0), at = r.GetFieldValue<DateTimeOffset>(1), user = r.Text(2), action = r.GetString(3), entity = r.GetString(4), entityId = r.Text(5),
                oldValue = r.IsDBNull(6) ? (JsonElement?)null : JsonDocument.Parse(r.GetString(6)).RootElement.Clone(),
                newValue = r.IsDBNull(7) ? (JsonElement?)null : JsonDocument.Parse(r.GetString(7)).RootElement.Clone(),
            }, ct, ("e", entity), ("n", PageSize), ("o", (Math.Max(page, 1) - 1) * PageSize)));

    private static async Task<IResult> StatsAsync(NpgsqlDataSource db, CancellationToken ct)
    {
        await using NpgsqlCommand cmd = db.CreateCommand(
            "select (select count(*) from app_user), (select count(*) from poi where deleted_at is null), " +
            "(select count(*) from place_owner where status = 'pending'), (select count(*) from poi_edit_request where status = 'pending'), " +
            "(select count(*) from report where status = 'open'), (select count(*) from review), (select count(*) from poi_photo where status = 'pending')");
        await using NpgsqlDataReader r = await cmd.ExecuteReaderAsync(ct);
        await r.ReadAsync(ct);
        return Results.Ok(new { users = r.GetInt64(0), places = r.GetInt64(1), pendingClaims = r.GetInt64(2), pendingEditRequests = r.GetInt64(3), openReports = r.GetInt64(4), reviews = r.GetInt64(5), pendingPhotos = r.GetInt64(6) });
    }
}
