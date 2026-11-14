using System.Globalization;
using Microsoft.Extensions.Caching.Memory;

namespace Hue_travel.Api.Weather;

/// <summary>
/// Current weather in Hue, from Open-Meteo, asked at most once per 15 minutes whatever the number of callers. This is the body of
/// GET /v1/weather, moved here unchanged so the assistant's weather tool reads the very same (cached) answer.
/// </summary>
public sealed class WeatherService(IHttpClientFactory clients, IMemoryCache cache, TimeProvider clock, ILogger<WeatherService> log)
{
    private static readonly TimeSpan CacheFor = TimeSpan.FromMinutes(15);
    private const string CacheKey = "weather";

    /// <summary>The weather, or null when Open-Meteo cannot be reached or answers with something unusable.</summary>
    public async Task<WeatherDto?> GetAsync(CancellationToken ct)
    {
        if (cache.TryGetValue(CacheKey, out WeatherDto? cached) && cached != null) return cached;

        string url = string.Create(CultureInfo.InvariantCulture,
            $"https://api.open-meteo.com/v1/forecast?latitude={WeatherEndpoints.HueLatitude}&longitude={WeatherEndpoints.HueLongitude}&current=temperature_2m,apparent_temperature,weather_code,is_day,precipitation,wind_speed_10m,uv_index&timezone=Asia%2FBangkok");
        try
        {
            using HttpClient client = clients.CreateClient("openmeteo");
            string body = await client.GetStringAsync(url, ct);
            WeatherDto? fresh = WeatherParser.TryParse(body, clock.GetUtcNow());
            if (fresh != null)
            {
                cache.Set(CacheKey, fresh, CacheFor);
                return fresh;
            }
            log.LogWarning("Open-Meteo returned an unexpected response");
        }
        catch (Exception ex) when ((ex is HttpRequestException or TaskCanceledException) && !ct.IsCancellationRequested)
        {
            log.LogWarning(ex, "Open-Meteo request failed");
        }
        return null;
    }
}
