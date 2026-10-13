using Hue_travel.Api.Options;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Hue_travel.Api.Meta;

public sealed record ClientConfigDto(
    string ApiVersion, string MinimumClientVersion, string TileUrl, string TileAttribution, IReadOnlyDictionary<string, string> TileUrls);

public static class MetaEndpoints
{
    public const string ApiVersion = "1.0";

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
    }
}
