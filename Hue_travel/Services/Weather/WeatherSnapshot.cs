using System;

namespace Hue_travel.Services.Weather
{
    /// <summary>One reading of the current weather (immutable).</summary>
    public sealed class WeatherSnapshot
    {
        public WeatherSnapshot(double temperatureC, int weatherCode, bool isDay, DateTime fetchedAtUtc)
        {
            TemperatureC = temperatureC;
            WeatherCode = weatherCode;
            IsDay = isDay;
            FetchedAtUtc = fetchedAtUtc;
        }

        public double TemperatureC { get; }

        /// <summary>WMO weather interpretation code (0 = clear sky, 61 = rain, 95 = thunderstorm, ...).</summary>
        public int WeatherCode { get; }

        public bool IsDay { get; }

        public DateTime FetchedAtUtc { get; }
    }
}
