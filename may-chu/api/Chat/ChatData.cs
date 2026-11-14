using Hue_travel.Api.Pois;
using Hue_travel.Api.Routing;
using Hue_travel.Api.Weather;

namespace Hue_travel.Api.Chat;

/// <summary>The real data of the assistant's tools: the same repositories and services the REST endpoints use.</summary>
public sealed class ChatData(PoiRepository pois, RoutePlanService plans, WeatherService weather) : IChatData
{
    public async Task<IReadOnlyList<PoiDto>> SearchPlacesAsync(string? query, string? group, int limit, CancellationToken ct)
    {
        string[] groups = string.IsNullOrEmpty(group) ? [] : [group];
        string? text = string.IsNullOrWhiteSpace(query) ? null : query.Trim();
        PagedResult<PoiDto> page = await pois.SearchAsync(new PoiQuery(groups, text, null, 1, limit), ct);
        return page.Items;
    }

    public Task<PoiDto?> GetPlaceAsync(string id, CancellationToken ct) => pois.FindAsync(id, ct);

    // The assistant never counts "added to a route": that number is for people who plan a real trip in the app.
    public Task<RoutePlanOutcome> PlanRouteAsync(string startId, string[] stopIds, bool optimize, CancellationToken ct) =>
        plans.PlanAsync(new RouteRequest(startId, stopIds, optimize), false, ct);

    public Task<WeatherDto?> GetWeatherAsync(CancellationToken ct) => weather.GetAsync(ct);
}
