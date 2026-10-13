using System.Security.Claims;
using Hue_travel.Api.Audit;
using Hue_travel.Api.Data;
using Npgsql;

namespace Hue_travel.Api.Admin;

public sealed record ImportRequest(PoiCreate[]? Places, bool? DryRun);
public sealed record ImportRow(string Id, string Action, string? Problem, string? Name);

/// <summary>What an admin does to the place data: look every place up, delete an account, and bring in a batch of places from a file.</summary>
public static partial class AdminEndpoints
{
    public const long MaxImportBodyBytes = 2 * 1024 * 1024;
    private const int MaxImportRows = 500;
    private const double SamePlaceDegrees = 0.000001;   // about 10 cm

    private static void MapPlaceAndAccountEndpoints(RouteGroupBuilder admin)
    {
        admin.MapGet("/pois", ListAllPoisAsync);
        admin.MapPost("/pois/import", ImportPoisAsync);
        admin.MapDelete("/users/{id:long}", DeleteUserAsync);
    }

    // ------------------------------------------------------------ places

    /// <summary>Every place that is not deleted, hidden and closed ones too (the public list shows only visible places).</summary>
    private static async Task<IResult> ListAllPoisAsync(NpgsqlDataSource db, CancellationToken ct, string? q = null, string? group = null, string? status = null, int page = 1)
    {
        if (group != null && !Groups.Contains(group)) return Bad("group không hợp lệ.");
        if (status != null && !PoiStatuses.Contains(status)) return Bad("status phải là visible, hidden hoặc closed.");
        string? text = string.IsNullOrWhiteSpace(q) ? null : q.Trim().Replace("%", "").Replace("_", "");
        var items = await db.ListAsync(
            "select p.id, p.nhom, p.loai_hinh, p.ten, p.dia_chi, p.gio_mo_cua, p.gia_text, p.sdt, p.website, p.mo_ta, p.status, p.version, " +
            "st_x(st_transform(p.geom, 4326)), st_y(st_transform(p.geom, 4326)), " +
            "(select count(*) from poi_photo f where f.poi_id = p.id and f.status = 'approved') " +
            "from poi p where p.deleted_at is null " +
            "and (@g::text is null or p.nhom = @g::text) and (@s::text is null or p.status = @s::text) " +
            "and (@q::text is null or p.ten_khong_dau like '%' || lower(f_unaccent(@q::text)) || '%' or p.id ilike '%' || @q::text || '%') " +
            "order by p.nhom, p.ten limit @n offset @o",
            r => new
            {
                id = r.GetString(0), group = r.GetString(1), type = r.Text(2), name = r.GetString(3), address = r.Text(4), openingHours = r.Text(5),
                priceText = r.Text(6), phone = r.Text(7), website = r.Text(8), summary = r.Text(9), status = r.GetString(10), version = r.GetInt32(11),
                lon = r.GetDouble(12), lat = r.GetDouble(13), photos = r.GetInt64(14),
            }, ct, ("g", group), ("s", status), ("q", text), ("n", PageSize), ("o", (Math.Max(page, 1) - 1) * PageSize));
        return Results.Ok(items);
    }

    /// <summary>
    /// Brings in a batch of places (a CSV or GeoJSON file read by the app). A place whose id exists is updated with the
    /// fields the file fills in (an empty cell leaves the field alone); an unknown id is created. Nothing is ever deleted.
    /// With dryRun the answer says what would happen and changes nothing, so the admin can look before applying.
    /// </summary>
    private static async Task<IResult> ImportPoisAsync(ImportRequest request, ClaimsPrincipal admin, NpgsqlDataSource db, CancellationToken ct)
    {
        PoiCreate[] places = request.Places ?? [];
        if (places.Length is < 1 or > MaxImportRows) return Bad($"Tệp cần từ 1 đến {MaxImportRows} địa điểm.");
        if (places.Select(p => p.Id).Where(id => id != null).GroupBy(id => id).Any(g => g.Count() > 1)) return Bad("Tệp có mã địa điểm bị trùng.");
        bool dryRun = request.DryRun ?? true;

        var rows = new List<ImportRow>(places.Length);
        await using NpgsqlConnection conn = await db.OpenConnectionAsync(ct);
        await using NpgsqlTransaction tx = await conn.BeginTransactionAsync(ct);
        foreach (PoiCreate place in places) rows.Add(await ImportOneAsync(place, admin.UserId(), dryRun, conn, tx, ct));

        if (dryRun) await tx.RollbackAsync(ct);
        else await tx.CommitAsync(ct);

        var counts = new Dictionary<string, int>
        {
            ["create"] = rows.Count(r => r.Action == "create"), ["update"] = rows.Count(r => r.Action == "update"),
            ["unchanged"] = rows.Count(r => r.Action == "unchanged"), ["invalid"] = rows.Count(r => r.Action == "invalid"),
        };
        if (!dryRun) await db.RecordAsync(admin.UserId(), "poi.import", "poi", null, null, new { counts, ids = rows.Where(r => r.Action is "create" or "update").Select(r => r.Id) }, ct);
        return Results.Ok(new { dryRun, counts, rows });
    }

    private static async Task<ImportRow> ImportOneAsync(PoiCreate place, long adminId, bool dryRun, NpgsqlConnection conn, NpgsqlTransaction tx, CancellationToken ct)
    {
        string id = place.Id?.Trim() ?? "";
        if (!PoiIdPattern().IsMatch(id)) return new ImportRow(id, "invalid", "Mã gồm 2 đến 20 ký tự: chữ, số, gạch ngang hoặc gạch dưới.", place.Name);
        if (place.Group != null && !Groups.Contains(place.Group)) return new ImportRow(id, "invalid", "Nhóm không hợp lệ (heritage, scenery, food, stay, service, transport).", place.Name);
        if (place.Name != null && place.Name.Trim().Length > 200) return new ImportRow(id, "invalid", "Tên tối đa 200 ký tự.", place.Name);
        if (!InputRules.IsHttpUrlOrEmpty(place.Website)) return new ImportRow(id, "invalid", "Website phải là địa chỉ http:// hoặc https://.", place.Name);
        if ((place.Lon == null) != (place.Lat == null)) return new ImportRow(id, "invalid", "Cần cả kinh độ và vĩ độ.", place.Name);
        if (place.Lon is < 100 or > 112 || place.Lat is < 8 or > 24) return new ImportRow(id, "invalid", "Kinh độ và vĩ độ phải nằm trong lãnh thổ Việt Nam.", place.Name);

        string? name = Blank(place.Name), type = Blank(place.Type), address = Blank(place.Address), hours = Blank(place.OpeningHours),
                price = Blank(place.PriceText), phone = Blank(place.Phone), website = Blank(place.Website), summary = Blank(place.Summary);

        // What the place looks like now.
        string? oldName = null, oldGroup = null, oldType = null, oldAddress = null, oldHours = null, oldPrice = null, oldPhone = null, oldWebsite = null, oldSummary = null;
        double oldLon = 0, oldLat = 0;
        bool exists;
        await using (var find = new NpgsqlCommand(
            "select ten, nhom, loai_hinh, dia_chi, gio_mo_cua, gia_text, sdt, website, mo_ta, st_x(st_transform(geom, 4326)), st_y(st_transform(geom, 4326)) " +
            "from poi where id = @id and deleted_at is null", conn, tx))
        {
            find.Parameters.AddWithValue("id", id);
            await using NpgsqlDataReader r = await find.ExecuteReaderAsync(ct);
            exists = await r.ReadAsync(ct);
            if (exists)
            {
                oldName = r.GetString(0); oldGroup = r.GetString(1); oldType = r.Text(2); oldAddress = r.Text(3); oldHours = r.Text(4);
                oldPrice = r.Text(5); oldPhone = r.Text(6); oldWebsite = r.Text(7); oldSummary = r.Text(8); oldLon = r.GetDouble(9); oldLat = r.GetDouble(10);
            }
        }

        if (!exists)
        {
            if (place.Group == null || name == null || place.Lon == null) return new ImportRow(id, "invalid", "Địa điểm mới cần có nhóm, tên, kinh độ và vĩ độ.", name);
            if (!dryRun)
            {
                await using var insert = new NpgsqlCommand(
                    "insert into poi (id, nhom, loai_hinh, ten, dia_chi, gio_mo_cua, gio_mo, gio_dong, gia_text, gia_thap, gia_cao, sdt, website, mo_ta, updated_by, geom) " +
                    "values (@id, @g, @t, @n, @a, @h, f_gio_mo(@h), f_gio_dong(@h), @p, (f_gia_range(@p))[1], (f_gia_range(@p))[2], @s, @w, @m, @by, " +
                    "st_transform(st_setsrid(st_makepoint(@lon, @lat), 4326), 3405))", conn, tx);
                Sql.Fill(insert, [("id", id), ("g", place.Group), ("t", type), ("n", name), ("a", address), ("h", hours), ("p", price), ("s", phone), ("w", website),
                    ("m", summary), ("by", adminId), ("lon", place.Lon), ("lat", place.Lat)]);
                await insert.ExecuteNonQueryAsync(ct);
                await SnapAsync(id, conn, tx, ct);
            }
            return new ImportRow(id, "create", null, name);
        }

        bool moved = place.Lon != null && (Math.Abs(place.Lon.Value - oldLon) > SamePlaceDegrees || Math.Abs(place.Lat!.Value - oldLat) > SamePlaceDegrees);
        bool changed = moved
            || (name != null && name != oldName) || (place.Group != null && place.Group != oldGroup) || (type != null && type != oldType)
            || (address != null && address != oldAddress) || (hours != null && hours != oldHours) || (price != null && price != oldPrice)
            || (phone != null && phone != oldPhone) || (website != null && website != oldWebsite) || (summary != null && summary != oldSummary);
        if (!changed) return new ImportRow(id, "unchanged", null, oldName);

        if (!dryRun)
        {
            await using var update = new NpgsqlCommand(
                "update poi set ten = coalesce(@n, ten), nhom = coalesce(@g, nhom), loai_hinh = coalesce(@t, loai_hinh), dia_chi = coalesce(@a, dia_chi), " +
                "gio_mo_cua = coalesce(@h, gio_mo_cua), gia_text = coalesce(@p, gia_text), sdt = coalesce(@s, sdt), website = coalesce(@w, website), mo_ta = coalesce(@m, mo_ta), " +
                "gio_mo = case when @h::text is not null then f_gio_mo(@h) else gio_mo end, gio_dong = case when @h::text is not null then f_gio_dong(@h) else gio_dong end, " +
                "gia_thap = case when @p::text is not null then (f_gia_range(@p))[1] else gia_thap end, gia_cao = case when @p::text is not null then (f_gia_range(@p))[2] else gia_cao end, " +
                "geom = case when @lon::float8 is not null then st_transform(st_setsrid(st_makepoint(@lon, @lat), 4326), 3405) else geom end, " +
                "version = version + 1, updated_by = @by, updated_at = now() where id = @id", conn, tx);
            Sql.Fill(update, [("id", id), ("g", place.Group), ("t", type), ("n", name), ("a", address), ("h", hours), ("p", price), ("s", phone), ("w", website),
                ("m", summary), ("by", adminId), ("lon", place.Lon), ("lat", place.Lat)]);
            await update.ExecuteNonQueryAsync(ct);
            if (moved) await SnapAsync(id, conn, tx, ct);
        }
        return new ImportRow(id, "update", null, name ?? oldName);
    }

    private static async Task SnapAsync(string id, NpgsqlConnection conn, NpgsqlTransaction tx, CancellationToken ct)
    {
        await using var snap = new NpgsqlCommand("select snap_poi(@id)", conn, tx);
        snap.Parameters.AddWithValue("id", id);
        await snap.ExecuteScalarAsync(ct);
    }

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    // ------------------------------------------------------------ accounts

    /// <summary>
    /// Removes an account for good: only one that was disabled (locked) first, never your own. The person's favourites,
    /// saved itineraries, reviews, owner links and sessions go with it; the audit trail keeps the lines but no longer names anyone.
    /// </summary>
    private static async Task<IResult> DeleteUserAsync(long id, ClaimsPrincipal admin, Auth.UserRepository users, NpgsqlDataSource db, CancellationToken ct)
    {
        if (id == admin.UserId()) return Bad("Không tự xóa tài khoản của chính mình.");
        Auth.AppUser? user = await users.FindByIdAsync(id, ct);
        if (user == null) return NotFound("Không tìm thấy người dùng.");
        if (user.Status != "locked") return Bad("Hãy vô hiệu hóa tài khoản trước, rồi mới xóa.");
        await db.ExecAsync("delete from app_user where id = @id", ct, ("id", id));
        await db.RecordAsync(admin.UserId(), "user.delete", "app_user", id.ToString(), new { user.Username, user.Role }, null, ct);
        return Results.NoContent();
    }
}
