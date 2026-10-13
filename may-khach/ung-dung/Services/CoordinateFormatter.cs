using System;
using System.Globalization;

namespace Hue_travel.Services
{
    /// <summary>Formats a map position for the status bar, either as VN-2000 UTM metres or as latitude / longitude.</summary>
    public static class CoordinateFormatter
    {
        /// <summary>For example "E 775,116 · N 1,822,771".</summary>
        public static string FormatUtm(double easting, double northing)
        {
            return string.Format(CultureInfo.InvariantCulture, "E {0:N0} \u00B7 N {1:N0}", easting, northing);
        }

        /// <summary>For example "16.4698°N, 107.5786°E".</summary>
        public static string FormatDegrees(double longitude, double latitude)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:0.0000}\u00B0{1}, {2:0.0000}\u00B0{3}",
                Math.Abs(latitude), latitude < 0 ? "S" : "N",
                Math.Abs(longitude), longitude < 0 ? "W" : "E");
        }
    }
}
