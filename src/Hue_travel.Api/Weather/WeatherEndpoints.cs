using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace Hue_travel.Api.Weather;

public sealed record WeatherDto(
    double Temperature, double ApparentTemperature, int Code, bool IsDay, double Precipitation, double WindSpeed, double UvIndex,
    DateTimeOffset FetchedAt);

/// <summary>Reads the "current" block of an Open-Meteo forecast response (the same fields the desktop app used to read itself).</summary>
public static class WeatherParser
{
    public static WeatherDto? TryParse(string json, DateTimeOffset now)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("current", out JsonElement c)) return null;
            if (!c.TryGetProperty("temperature_2m", out JsonElement temperature) || !c.TryGetProperty("weather_code", out JsonElement code))
                return null;

            double t = temperature.GetDouble();
            return new WeatherDto(
                t, Optional(c, "apparent_temperature", t), code.GetInt32(),
                !c.TryGetProperty("is_day", out JsonElement day) || day.GetInt32() != 0,
                Optional(c, "precipitation", 0), Optional(c, "wind_speed_10m", 0), Optional(c, "uv_index", 0), now);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException)
        {
            return null;
        }
    }

    private static double Optional(JsonElement block, string name, double fallback) =>
        block.TryGetProperty(name, out JsonElement v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : fallback;
}

public static class WeatherEndpoints
{
    public const double HueLatitude = 16.4637;
    public const double HueLongitude = 107.5909;
    private static readonly TimeSpan CacheFor = TimeSpan.FromMinutes(15);
    private const string CacheKey = "weather";

    public static void MapWeatherEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/weather", GetAsync);
    }

    /// <summary>Current weather in Hue. Open-Meteo is asked at most once per 15 minutes, whatever the number of app users.</summary>
    private static async Task<IResult> GetAsync(IHttpClientFactory clients, IMemoryCache cache, TimeProvider clock, ILogger<WeatherDto> log, CancellationToken ct)
    {
        if (cache.TryGetValue(CacheKey, out WeatherDto? cached) && cached != null) return Results.Ok(cached);

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
                return Results.Ok(fresh);
            }
            log.LogWarning("Open-Meteo returned an unexpected response");
        }
        catch (Exception ex) when ((ex is HttpRequestException or TaskCanceledException) && !ct.IsCancellationRequested)
        {
            log.LogWarning(ex, "Open-Meteo request failed");
        }
        return Results.Problem(detail: "Chưa lấy được thời tiết.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}
