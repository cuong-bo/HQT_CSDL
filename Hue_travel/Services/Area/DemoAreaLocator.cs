using Hue_travel.Services.Routing;

namespace Hue_travel.Services.Area
{
    /// <summary>Stand-in locator: the nearest landmark within reach names the area.</summary>
    public sealed class DemoAreaLocator : IAreaLocator
    {
        public const string DefaultArea = "Trung tâm Cố đô Huế & Bờ sông Hương";

        private const double ReachInKm = 0.25;

        private static readonly string[] Names =
        {
            "Khu vực Hoàng Thành Huế",
            "Khu vực chùa Thiên Mụ & sông Hương",
            "Khu vực Ga Huế",
            "Khu ẩm thực phố Huế",
        };

        private static readonly string[] PoiIds = { "dai-noi", "thien-mu", "ga-hue", "bun-bo-mu-roi" };

        public string Locate(double longitude, double latitude)
        {
            string best = DefaultArea;
            double bestDistance = ReachInKm;
            for (int i = 0; i < PoiIds.Length; i++)
            {
                GeoPoint place;
                if (!DemoPlaces.TryGetPoi(PoiIds[i], out place)) continue;
                double distance = GeoMath.DistanceKm(longitude, latitude, place.Longitude, place.Latitude);
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                best = Names[i];
            }
            return best;
        }
    }
}
