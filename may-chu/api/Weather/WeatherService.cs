using System.Globalization;
using Microsoft.Extensions.Caching.Memory;

namespace Hue_travel.Api.Weather;

/// <summary>Current weather in Hue, cached for 15 minutes; shared by the HTTP endpoint and the chat tools.</summary>
public sealed class WeatherService(IHttpClientFactory clients, IMemoryCache cache, TimeProvider clock, ILogger<WeatherService> log)
{
    public const double HueLatitude = 16.4637;
    public const double HueLongitude = 107.5909;
    private static readonly TimeSpan CacheFor = TimeSpan.FromMinutes(15);
    private const string CacheKey = "weather";

    public async Task<WeatherDto?> GetAsync(CancellationToken ct)
    {
        if (cache.TryGetValue(CacheKey, out WeatherDto? cached) && cached != null) return cached;

        string url = string.Create(CultureInfo.InvariantCulture,
            $"https://api.open-meteo.com/v1/forecast?latitude={HueLatitude}&longitude={HueLongitude}&current=temperature_2m,apparent_temperature,weather_code,is_day,precipitation,wind_speed_10m,uv_index&timezone=Asia%2FBangkok");
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
