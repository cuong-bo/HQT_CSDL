using System;
using System.Globalization;

namespace Hue_travel.Services.Weather
{
    /// <summary>Turns a WMO weather code and temperature into the Vietnamese status-bar sentence.</summary>
    public static class WeatherDescriber
    {
        private const double HotFromC = 35;
        private const double ColdUpToC = 15;

        private enum Condition { Clear, PartlyCloudy, Overcast, Fog, Drizzle, Rain, Thunderstorm, Other }

        /// <summary>For example "Nắng đẹp, 26°C - Rất thuận tiện tham quan".</summary>
        public static string Describe(int weatherCode, bool isDay, double temperatureC)
        {
            Condition condition = Classify(weatherCode);
            return string.Format(CultureInfo.InvariantCulture, "{0}, {1:0}°C - {2}",
                Label(condition, isDay), Math.Round(temperatureC), Advice(condition, temperatureC));
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

        private static string Advice(Condition condition, double temperatureC)
        {
            if (condition == Condition.Thunderstorm) return "Nên hạn chế đi ngoài trời";
            if (condition == Condition.Rain || condition == Condition.Drizzle) return "Nên mang theo áo mưa";
            if (condition == Condition.Fog) return "Nên đi chậm và chú ý quan sát";
            if (temperatureC >= HotFromC) return "Nắng nóng, nên mang theo nước và mũ";
            if (temperatureC <= ColdUpToC) return "Trời se lạnh, nên mang theo áo khoác";
            return "Rất thuận tiện tham quan";
        }
    }
}
