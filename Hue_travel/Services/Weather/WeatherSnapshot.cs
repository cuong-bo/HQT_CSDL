using System;

namespace Hue_travel.Services.Weather
{
    /// <summary>One reading of the current weather (immutable).</summary>
    public sealed class WeatherSnapshot
    {
        public WeatherSnapshot(double temperatureC, double apparentC, int weatherCode, bool isDay,
            double precipitationMm, double windKmh, double uvIndex, DateTime fetchedAtUtc)
        {
            TemperatureC = temperatureC;
            ApparentC = apparentC;
            WeatherCode = weatherCode;
            IsDay = isDay;
            PrecipitationMm = precipitationMm;
            WindKmh = windKmh;
            UvIndex = uvIndex;
            FetchedAtUtc = fetchedAtUtc;
        }

        public double TemperatureC { get; }

        /// <summary>"Feels like" temperature: heat plus humidity and wind (matters a lot in humid Hue).</summary>
        public double ApparentC { get; }

        /// <summary>WMO weather interpretation code (0 = clear sky, 61 = rain, 95 = thunderstorm, ...).</summary>
        public int WeatherCode { get; }

        public bool IsDay { get; }

        /// <summary>Precipitation measured in the last interval (mm).</summary>
        public double PrecipitationMm { get; }

        public double WindKmh { get; }

        public double UvIndex { get; }

        public DateTime FetchedAtUtc { get; }
    }
}
