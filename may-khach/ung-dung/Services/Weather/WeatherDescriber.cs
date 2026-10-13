using System;
using System.Globalization;

namespace Hue_travel.Services.Weather
{
    /// <summary>Turns a weather reading into the Vietnamese status-bar sentence and a sightseeing hint.</summary>
    public static class WeatherDescriber
    {
        private const double FeelsLikeGapC = 3;       // show "cảm giác" only when it differs this much from the air temperature
        private const double HotApparentFromC = 35;
        private const double ColdApparentUpToC = 15;
        private const double StrongWindFromKmh = 40;
        private const double HighUvFrom = 8;
        private const double RainFromMm = 0.1;

        private enum Condition { Clear, PartlyCloudy, Overcast, Fog, Drizzle, Rain, Thunderstorm, Other }

        /// <summary>For example "Nắng đẹp, 26°C - Rất thuận tiện tham quan".</summary>
        public static string Describe(WeatherSnapshot reading)
        {
            if (reading == null) throw new ArgumentNullException(nameof(reading));

            Condition condition = Classify(reading.WeatherCode);
            string feelsLike = Math.Abs(reading.ApparentC - reading.TemperatureC) >= FeelsLikeGapC
                ? string.Format(CultureInfo.InvariantCulture, " (cảm giác {0:0}°C)", Math.Round(reading.ApparentC))
                : string.Empty;
            return string.Format(CultureInfo.InvariantCulture, "{0}, {1:0}°C{2} - {3}",
                Label(condition, reading.IsDay), Math.Round(reading.TemperatureC), feelsLike, Advice(condition, reading));
        }

        private static Condition Classify(int code)
        {
            if (code == 0) return Condition.Clear;
            if (code == 1 || code == 2) return Condition.PartlyCloudy;
            if (code == 3) return Condition.Overcast;
            if (code == 45 || code == 48) return Condition.Fog;
            if (code >= 51 && code <= 57) return Condition.Drizzle;
            if ((code >= 61 && code <= 67) || (code >= 80 && code <= 82)) return Condition.Rain;
            if (code >= 95 && code <= 99) return Condition.Thunderstorm;
            return Condition.Other;
        }

        private static string Label(Condition condition, bool isDay)
        {
            switch (condition)
            {
                case Condition.Clear: return isDay ? "Nắng đẹp" : "Trời quang";
                case Condition.PartlyCloudy: return "Ít mây";
                case Condition.Overcast: return "Nhiều mây";
                case Condition.Fog: return "Có sương mù";
                case Condition.Drizzle: return "Mưa phùn";
                case Condition.Rain: return "Mưa";
                case Condition.Thunderstorm: return "Dông";
                default: return "Thời tiết thay đổi";
            }
        }

        // The most important warning wins; "very convenient" is only said when nothing speaks against going out.
        private static string Advice(Condition condition, WeatherSnapshot reading)
        {
            if (condition == Condition.Thunderstorm) return "Nên hạn chế đi ngoài trời";
            if (condition == Condition.Rain || condition == Condition.Drizzle || reading.PrecipitationMm >= RainFromMm) return "Nên mang theo áo mưa";
            if (condition == Condition.Fog) return "Nên đi chậm và chú ý quan sát";
            if (reading.WindKmh >= StrongWindFromKmh) return "Gió mạnh, nên cẩn thận khi di chuyển ngoài trời";
            if (reading.ApparentC >= HotApparentFromC) return "Nắng nóng, nên mang theo nước và mũ";
            if (reading.IsDay && reading.UvIndex >= HighUvFrom) return "Chỉ số UV cao, nên che nắng";
            if (reading.ApparentC <= ColdApparentUpToC) return "Trời se lạnh, nên mang theo áo khoác";
            if (!reading.IsDay) return "Trời tối, phù hợp dạo phố và ngắm sông Hương";
            return "Rất thuận tiện tham quan";
        }
    }
}
