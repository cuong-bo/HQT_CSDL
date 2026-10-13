using System;

namespace Hue_travel.Services.Routing
{
    public static class GeoMath
    {
        private const double EarthRadiusKm = 6371.0;

        /// <summary>Great-circle (straight line) distance between two positions in kilometres (haversine).</summary>
        public static double DistanceKm(double longitude1, double latitude1, double longitude2, double latitude2)
        {
            double dLat = ToRadians(latitude2 - latitude1);
            double dLon = ToRadians(longitude2 - longitude1);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                       + Math.Cos(ToRadians(latitude1)) * Math.Cos(ToRadians(latitude2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return 2 * EarthRadiusKm * Math.Asin(Math.Min(1, Math.Sqrt(a)));
        }

        public static double Between(GeoPoint from, GeoPoint to) => DistanceKm(from.Longitude, from.Latitude, to.Longitude, to.Latitude);

        private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
    }
}
