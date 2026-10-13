using System;

namespace Hue_travel.Services.Data
{
    /// <summary>
    /// Converts map metres in VN-2000 / UTM zone 48N (central meridian 105 degrees, scale 0.9996, WGS84 ellipsoid) to degrees.
    /// Some Shapefile layers carry no longitude / latitude columns, only the point geometry.
    /// </summary>
    public static class VnUtm
    {
        private const double SemiMajor = 6378137.0;
        private const double Flattening = 1 / 298.257223563;
        private const double CentralMeridianDegrees = 105.0;
        private const double Scale = 0.9996;
        private const double FalseEasting = 500000.0;

        /// <summary>Map metres of a position in degrees (the other direction of <see cref="ToDegrees"/>).</summary>
        public static void ToMetres(double longitude, double latitude, out double easting, out double northing)
        {
            double e2 = Flattening * (2 - Flattening);
            double ep2 = e2 / (1 - e2);
            double lat = latitude * Math.PI / 180;
            double dLon = (longitude - CentralMeridianDegrees) * Math.PI / 180;

            double sin = Math.Sin(lat), cos = Math.Cos(lat), tan = Math.Tan(lat);
            double n = SemiMajor / Math.Sqrt(1 - e2 * sin * sin);
            double t = tan * tan;
            double c = ep2 * cos * cos;
            double a = dLon * cos;
            double m = SemiMajor * ((1 - e2 / 4 - 3 * e2 * e2 / 64 - 5 * e2 * e2 * e2 / 256) * lat
                - (3 * e2 / 8 + 3 * e2 * e2 / 32 + 45 * e2 * e2 * e2 / 1024) * Math.Sin(2 * lat)
                + (15 * e2 * e2 / 256 + 45 * e2 * e2 * e2 / 1024) * Math.Sin(4 * lat)
                - (35 * e2 * e2 * e2 / 3072) * Math.Sin(6 * lat));

            easting = FalseEasting + Scale * n * (a + (1 - t + c) * Math.Pow(a, 3) / 6
                + (5 - 18 * t + t * t + 72 * c - 58 * ep2) * Math.Pow(a, 5) / 120);
            northing = Scale * (m + n * tan * (a * a / 2 + (5 - t + 9 * c + 4 * c * c) * Math.Pow(a, 4) / 24
                + (61 - 58 * t + t * t + 600 * c - 330 * ep2) * Math.Pow(a, 6) / 720));
        }

        /// <summary>Longitude and latitude (degrees) of a point in map metres.</summary>
        public static void ToDegrees(double easting, double northing, out double longitude, out double latitude)
        {
            double e2 = Flattening * (2 - Flattening);
            double ep2 = e2 / (1 - e2);
            double x = easting - FalseEasting;
            double m = northing / Scale;

            double mu = m / (SemiMajor * (1 - e2 / 4 - 3 * e2 * e2 / 64 - 5 * e2 * e2 * e2 / 256));
            double e1 = (1 - Math.Sqrt(1 - e2)) / (1 + Math.Sqrt(1 - e2));
            double footprint = mu
                + (3 * e1 / 2 - 27 * Math.Pow(e1, 3) / 32) * Math.Sin(2 * mu)
                + (21 * e1 * e1 / 16 - 55 * Math.Pow(e1, 4) / 32) * Math.Sin(4 * mu)
                + (151 * Math.Pow(e1, 3) / 96) * Math.Sin(6 * mu)
                + (1097 * Math.Pow(e1, 4) / 512) * Math.Sin(8 * mu);

            double sin = Math.Sin(footprint), cos = Math.Cos(footprint), tan = Math.Tan(footprint);
            double c1 = ep2 * cos * cos;
            double t1 = tan * tan;
            double n1 = SemiMajor / Math.Sqrt(1 - e2 * sin * sin);
            double r1 = SemiMajor * (1 - e2) / Math.Pow(1 - e2 * sin * sin, 1.5);
            double d = x / (n1 * Scale);

            double lat = footprint - (n1 * tan / r1) * (d * d / 2
                - (5 + 3 * t1 + 10 * c1 - 4 * c1 * c1 - 9 * ep2) * Math.Pow(d, 4) / 24
                + (61 + 90 * t1 + 298 * c1 + 45 * t1 * t1 - 252 * ep2 - 3 * c1 * c1) * Math.Pow(d, 6) / 720);
            double lon = (d - (1 + 2 * t1 + c1) * Math.Pow(d, 3) / 6
                + (5 - 2 * c1 + 28 * t1 - 3 * c1 * c1 + 8 * ep2 + 24 * t1 * t1) * Math.Pow(d, 5) / 120) / cos;

            latitude = lat * 180 / Math.PI;
            longitude = CentralMeridianDegrees + lon * 180 / Math.PI;
        }
    }
}
