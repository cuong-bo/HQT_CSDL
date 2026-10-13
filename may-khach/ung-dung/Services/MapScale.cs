using System;
using System.Globalization;

namespace Hue_travel.Services
{
    /// <summary>Turns a map view into a printed-map style scale such as "1 : 15,000".</summary>
    public static class MapScale
    {
        private const double MetresPerInch = 0.0254;
        private const double ExactBelow = 100;
        private const string Unknown = "1 : --";

        /// <summary>
        /// Scale denominator for a view <paramref name="viewWidthMetres"/> wide drawn across <paramref name="pixelWidth"/>
        /// pixels at <paramref name="dpi"/>. NaN when any input is not positive.
        /// </summary>
        public static double Denominator(double viewWidthMetres, int pixelWidth, double dpi)
        {
            if (viewWidthMetres <= 0 || pixelWidth <= 0 || dpi <= 0) return double.NaN;
            double screenWidthMetres = pixelWidth / dpi * MetresPerInch;
            return viewWidthMetres / screenWidthMetres;
        }

        /// <summary>Formats the denominator rounded to two significant digits so it does not flicker while panning.</summary>
        public static string Format(double denominator)
        {
            if (double.IsNaN(denominator) || double.IsInfinity(denominator) || denominator <= 0) return Unknown;
            double rounded = denominator < ExactBelow ? Math.Round(denominator) : RoundToTwoSignificantDigits(denominator);
            return "1 : " + rounded.ToString("N0", CultureInfo.InvariantCulture);
        }

        private static double RoundToTwoSignificantDigits(double value)
        {
            double factor = Math.Pow(10, Math.Floor(Math.Log10(value)) - 1);
            return Math.Round(value / factor) * factor;
        }
    }
}
