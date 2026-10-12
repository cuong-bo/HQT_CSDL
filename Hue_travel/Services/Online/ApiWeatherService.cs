using System;
using System.Threading;
using System.Threading.Tasks;
using Hue_travel.Services.Weather;

namespace Hue_travel.Services.Online
{
    /// <summary>Weather from our own server, which asks Open-Meteo at most once per 15 minutes for everybody.</summary>
    public sealed class ApiWeatherService : IWeatherService
    {
        private readonly ApiClient api;

        public ApiWeatherService(ApiClient api)
        {
            this.api = api;
        }

        public async Task<WeatherSnapshot> GetCurrentAsync(CancellationToken cancellationToken)
        {
            ApiResponse r = await api.GetAsync("/v1/weather", cancellationToken).ConfigureAwait(true);
            if (!r.IsSuccess) throw new WeatherServiceException(r.Error);
            var d = r.Object;
            if (d == null || JsonHelper.Str(d, "temperature") == null) throw new WeatherServiceException("Máy chủ trả về thời tiết không hợp lệ.");
            return new WeatherSnapshot(
                JsonHelper.Dbl(d, "temperature"), JsonHelper.Dbl(d, "apparentTemperature"), (int)JsonHelper.Dbl(d, "code"),
                !d.ContainsKey("isDay") || JsonHelper.Bool(d, "isDay"),
                JsonHelper.Dbl(d, "precipitation"), JsonHelper.Dbl(d, "windSpeed"), JsonHelper.Dbl(d, "uvIndex"), DateTime.UtcNow);
        }
    }
}
