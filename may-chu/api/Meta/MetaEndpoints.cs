using Hue_travel.Api.Options;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Hue_travel.Api.Meta;

public sealed record ClientConfigDto(
    string ApiVersion, string MinimumClientVersion, string TileUrl, string TileAttribution, IReadOnlyDictionary<string, string> TileUrls);

public static class MetaEndpoints
{
    public const string ApiVersion = "1.0";
    public const string DefaultBasemapDirectory = "/data/basemap";
    private const long MaxManifestBytes = 1_000_000;   // a few dozen files: tens of kilobytes at most

    public static void MapMetaEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/healthz", async (NpgsqlDataSource db, CancellationToken ct) =>
        {
            await using NpgsqlCommand cmd = db.CreateCommand("select 1");
            await cmd.ExecuteScalarAsync(ct);
            return Results.Ok(new { status = "ok" });
        });

        app.MapGet("/v1/config", (IOptions<ClientSettings> client) =>
            Results.Ok(new ClientConfigDto(ApiVersion, client.Value.MinimumVersion, client.Value.TileUrl, client.Value.TileAttribution, client.Value.TileUrls)));

        // The base map the app downloads: latest.json, written by scripts/pack-basemap.py, is sent as it is. Because the answer goes through
        // the /v1 signing, the app can trust the checksums in it (and so the package it names) as much as any other reply.
        app.MapGet("/v1/basemap/manifest", async (IConfiguration configuration, CancellationToken ct) =>
        {
            string directory = configuration["Basemap:Directory"] ?? DefaultBasemapDirectory;
            string path = Path.Combine(directory, "latest.json");
            FileInfo file = new(path);
            if (!file.Exists || file.Length > MaxManifestBytes)
                return Results.Json(new { error = "Máy chủ chưa đóng gói dữ liệu bản đồ (chạy scripts/deploy.sh pack)." }, statusCode: StatusCodes.Status404NotFound);
            return Results.Text(await File.ReadAllTextAsync(path, ct), "application/json; charset=utf-8");
        });
    }
}
