using Npgsql;

namespace Hue_travel.Api.Area;

public sealed record AreaDto(string Code, string Name);

public static class AreaEndpoints
{
    private const double NearestWithinMetres = 3000;

    public static void MapAreaEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/area", GetAsync);
    }

    /// <summary>The commune (xã / phường) at a position given in degrees; the nearest one within 3 km if it lies between boundaries.</summary>
    private static async Task<IResult> GetAsync(NpgsqlDataSource db, double lon, double lat, CancellationToken ct)
    {
        if (!double.IsFinite(lon) || !double.IsFinite(lat) || lon is < -180 or > 180 || lat is < -90 or > 90)
            return Results.Problem(detail: "lon và lat (độ) không hợp lệ.", statusCode: StatusCodes.Status400BadRequest);

        const string sql =
            "with p as (select st_transform(st_setsrid(st_makepoint(@lon, @lat), 4326), 3405) as g) " +
            "select ma_xa, ten_xa from ranh_xa, p " +
            "where st_dwithin(geom, p.g, @near) " +
            "order by st_contains(geom, p.g) desc, st_distance(geom, p.g) limit 1";
        await using NpgsqlCommand cmd = db.CreateCommand(sql);
        cmd.Parameters.AddWithValue("lon", lon);
        cmd.Parameters.AddWithValue("lat", lat);
        cmd.Parameters.AddWithValue("near", NearestWithinMetres);
        await using NpgsqlDataReader r = await cmd.ExecuteReaderAsync(ct);
        return await r.ReadAsync(ct)
            ? Results.Ok(new AreaDto(r.GetString(0), r.GetString(1)))
            : Results.Problem(detail: "Vị trí nằm ngoài vùng dữ liệu.", statusCode: StatusCodes.Status404NotFound);
    }
}
