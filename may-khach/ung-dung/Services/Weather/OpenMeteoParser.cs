using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Web.Script.Serialization;

namespace Hue_travel.Services.Weather
{
    /// <summary>Reads the "current" block of an Open-Meteo forecast response.</summary>
    public static class OpenMeteoParser
    {
        public static bool TryParse(string json, out WeatherSnapshot snapshot)
        {
            snapshot = null;
            if (string.IsNullOrWhiteSpace(json)) return false;
            try
            {
                var root = new JavaScriptSerializer().DeserializeObject(json) as Dictionary<string, object>;
                var current = root != null && root.ContainsKey("current") ? root["current"] as Dictionary<string, object> : null;
                if (current == null || !current.ContainsKey("temperature_2m") || !current.ContainsKey("weather_code")) return false;

                double temperature = ToDouble(current["temperature_2m"]);
                int code = Convert.ToInt32(current["weather_code"], CultureInfo.InvariantCulture);
                bool isDay = !current.ContainsKey("is_day") || Convert.ToInt32(current["is_day"], CultureInfo.InvariantCulture) != 0;
                snapshot = new WeatherSnapshot(
                    temperature,
                    ReadOptional(current, "apparent_temperature", temperature),
                    code,
                    isDay,
                    ReadOptional(current, "precipitation", 0),
                    ReadOptional(current, "wind_speed_10m", 0),
                    ReadOptional(current, "uv_index", 0),
                    DateTime.UtcNow);
                return true;
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException
                                       || ex is FormatException || ex is InvalidCastException || ex is OverflowException)
            {
                Debug.WriteLine("OpenMeteoParser: " + ex.Message);
                return false;
            }
        }

        private static double ToDouble(object value) => Convert.ToDouble(value, CultureInfo.InvariantCulture);

        private static double ReadOptional(Dictionary<string, object> block, string key, double fallback)
        {
            object value;
            return block.TryGetValue(key, out value) && value != null ? ToDouble(value) : fallback;
        }
    }
}
