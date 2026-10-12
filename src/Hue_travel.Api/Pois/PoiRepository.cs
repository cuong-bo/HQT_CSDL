using Hue_travel.Api.Data;
using Npgsql;
using NpgsqlTypes;

namespace Hue_travel.Api.Pois;

public sealed class PoiRepository(NpgsqlDataSource db)
{
    public const string Columns =
        "id, nhom, loai_hinh, ten, dia_chi, gio_mo_cua, gia_text, gia_thap, gia_cao, sdt, website, mo_ta, nguon, " +
        "st_x(st_transform(geom, 4326)), st_y(st_transform(geom, 4326)), st_x(geom), st_y(geom)";

    public async Task<PagedResult<PoiDto>> SearchAsync(PoiQuery query, CancellationToken ct)
    {
        const string sql =
            "select " + Columns + ", count(*) over() " +
            "from poi where deleted_at is null and status = 'visible' " +
            "and (cardinality(@groups) = 0 or nhom = any(@groups)) " +
            "and (@q::text is null or strpos(ten_khong_dau, lower(f_unaccent(@q::text))) > 0) " +
            "and (@hasBox = false or geom && st_makeenvelope(@minx, @miny, @maxx, @maxy, 3405)) " +
            "order by ten_khong_dau, id limit @size offset @offset";

        await using NpgsqlCommand cmd = db.CreateCommand(sql);
        cmd.Parameters.AddWithValue("groups", query.Groups);
        cmd.Parameters.Add(new NpgsqlParameter("q", NpgsqlDbType.Text) { Value = (object?)query.Text ?? DBNull.Value });
        double[] box = query.Bbox ?? [0, 0, 0, 0];
        cmd.Parameters.AddWithValue("hasBox", query.Bbox != null);
        cmd.Parameters.AddWithValue("minx", box[0]);
        cmd.Parameters.AddWithValue("miny", box[1]);
        cmd.Parameters.AddWithValue("maxx", box[2]);
        cmd.Parameters.AddWithValue("maxy", box[3]);
        cmd.Parameters.AddWithValue("size", query.PageSize);
        cmd.Parameters.AddWithValue("offset", (query.Page - 1) * query.PageSize);

        var items = new List<PoiDto>();
        long total = 0;
        await using NpgsqlDataReader r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            items.Add(Map(r));
            total = r.GetInt64(17);
        }
        return new PagedResult<PoiDto>(items, total, query.Page, query.PageSize);
    }

    public async Task<PoiDto?> FindAsync(string id, CancellationToken ct)
    {
        await using NpgsqlCommand cmd = db.CreateCommand(
            "select " + Columns + " from poi where id = @id and deleted_at is null and status = 'visible'");
        cmd.Parameters.AddWithValue("id", id);
        await using NpgsqlDataReader r = await cmd.ExecuteReaderAsync(ct);
        return await r.ReadAsync(ct) ? Map(r) : null;
    }

    /// <summary>The places a user marked as favourites, newest first.</summary>
    public async Task<IReadOnlyList<PoiDto>> FavoritesAsync(long userId, CancellationToken ct) =>
        await ListAsync("select " + Columns + " from favorite f join poi p on p.id = f.poi_id where f.user_id = @u " +
                        "and p.deleted_at is null and p.status = 'visible' order by f.created_at desc", userId, ct);

    /// <summary>The places an owner manages (approved claims only).</summary>
    public async Task<IReadOnlyList<PoiDto>> OwnedAsync(long userId, CancellationToken ct) =>
        await ListAsync("select " + Columns + " from place_owner o join poi p on p.id = o.poi_id where o.user_id = @u " +
                        "and o.status = 'approved' and p.deleted_at is null order by p.ten_khong_dau", userId, ct);

    public Task CountViewAsync(string id, CancellationToken ct) =>
        db.ExecAsync("insert into poi_stats_daily (poi_id, views) values (@p, 1) on conflict (poi_id, day) do update set views = poi_stats_daily.views + 1", ct, ("p", id));

    private async Task<IReadOnlyList<PoiDto>> ListAsync(string sql, long userId, CancellationToken ct)
    {
        await using NpgsqlCommand cmd = db.CreateCommand(sql);
        cmd.Parameters.AddWithValue("u", userId);
        var items = new List<PoiDto>();
        await using NpgsqlDataReader r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct)) items.Add(Map(r));
        return items;
    }

    private static string? Text(NpgsqlDataReader r, int i) => r.IsDBNull(i) ? null : r.GetString(i);

    private static int? Number(NpgsqlDataReader r, int i) => r.IsDBNull(i) ? null : r.GetInt32(i);

    private static PoiDto Map(NpgsqlDataReader r) => new(
        r.GetString(0), r.GetString(1), Text(r, 2), r.GetString(3), Text(r, 4), Text(r, 5),
        Text(r, 6), Number(r, 7), Number(r, 8), Text(r, 9), Text(r, 10), Text(r, 11), Text(r, 12),
        r.GetDouble(13), r.GetDouble(14), r.GetDouble(15), r.GetDouble(16));
}
