using System.Text.Json;
using Hue_travel.Api.Data;
using Npgsql;

namespace Hue_travel.Api.Routing;

public sealed class RouteRepository(NpgsqlDataSource db)
{
    /// <summary>The ids (from those asked for) that are visible places snapped to the road network.</summary>
    public async Task<HashSet<string>> RoutableIdsAsync(string[] ids, CancellationToken ct)
    {
        await using NpgsqlCommand cmd = db.CreateCommand(
            "select p.id from poi p join poi_snap s on s.poi_id = p.id " +
            "where p.id = any(@ids) and p.deleted_at is null and p.status = 'visible'");
        cmd.Parameters.AddWithValue("ids", ids);
        var found = new HashSet<string>();
        await using NpgsqlDataReader r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct)) found.Add(r.GetString(0));
        return found;
    }

    /// <summary>Travel time in seconds between every pair; infinity where there is no road connection. Index follows <paramref name="ids"/>.</summary>
    public async Task<double[,]> TimeMatrixAsync(string[] ids, CancellationToken ct)
    {
        var index = new Dictionary<string, int>();
        for (int i = 0; i < ids.Length; i++) index[ids[i]] = i;

        var matrix = new double[ids.Length, ids.Length];
        for (int i = 0; i < ids.Length; i++)
            for (int j = 0; j < ids.Length; j++) matrix[i, j] = i == j ? 0 : double.PositiveInfinity;

        await using NpgsqlCommand cmd = db.CreateCommand("select from_id, to_id, cost from route_cost_matrix(@ids, true)");
        cmd.Parameters.AddWithValue("ids", ids);
        await using NpgsqlDataReader r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            if (r.IsDBNull(2)) continue;
            matrix[index[r.GetString(0)], index[r.GetString(1)]] = r.GetDouble(2);
        }
        return matrix;
    }

    public Task CountRouteAddsAsync(string[] ids, CancellationToken ct) =>
        db.ExecAsync("insert into poi_stats_daily (poi_id, route_adds) select unnest(@ids), 1 " +
                     "on conflict (poi_id, day) do update set route_adds = poi_stats_daily.route_adds + 1", ct, ("ids", ids));

    /// <summary>One leg along the roads: length, time and the line as GeoJSON in degrees.</summary>
    public async Task<RouteLeg> LegAsync(string fromId, string toId, CancellationToken ct)
    {
        await using NpgsqlCommand cmd = db.CreateCommand(
            "select coalesce(sum(length_m), 0), coalesce(sum(seconds), 0), " +
            "st_asgeojson(st_transform(st_linemerge(st_collect(geom order by seq)), 4326), 6), " +
            "st_asgeojson(st_linemerge(st_collect(geom order by seq)), 1) " +
            "from route_path(@f, @t)");
        cmd.Parameters.AddWithValue("f", fromId);
        cmd.Parameters.AddWithValue("t", toId);
        await using NpgsqlDataReader r = await cmd.ExecuteReaderAsync(ct);
        await r.ReadAsync(ct);
        JsonElement? geometry = r.IsDBNull(2) ? null : JsonDocument.Parse(r.GetString(2)).RootElement.Clone();
        JsonElement? geometryUtm = r.IsDBNull(3) ? null : JsonDocument.Parse(r.GetString(3)).RootElement.Clone();
        return new RouteLeg(fromId, toId, r.GetDouble(0), r.GetDouble(1), geometry, geometryUtm);
    }
}
