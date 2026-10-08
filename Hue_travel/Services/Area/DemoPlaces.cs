using System.Collections.Generic;
using Hue_travel.Services.Routing;

namespace Hue_travel.Services.Area
{
    /// <summary>A named place; <see cref="Location"/> is null when the position is not known yet.</summary>
    public sealed class NamedPlace
    {
        public NamedPlace(string name, GeoPoint? location)
        {
            Name = name;
            Location = location;
        }

        public string Name { get; }

        public GeoPoint? Location { get; }
    }

    /// <summary>
    /// Real but approximate positions of the sample places, used by "show on map", the route planner and the area
    /// name until the PostGIS layers exist. Replace with the database, never extend.
    /// </summary>
    public static class DemoPlaces
    {
        private static readonly Dictionary<string, GeoPoint> Pois = new Dictionary<string, GeoPoint>
        {
            { "ga-hue", new GeoPoint(107.5727, 16.4559) },
            { "dai-noi", new GeoPoint(107.5786, 16.4698) },
            { "thien-mu", new GeoPoint(107.5445, 16.4536) },
            { "bun-bo-mu-roi", new GeoPoint(107.5880, 16.4650) },
            { "khai-dinh", new GeoPoint(107.5869, 16.3997) },
        };

        /// <summary>Places a trip can start from, in the order of the "Điểm bắt đầu đi" list.</summary>
        public static IReadOnlyList<NamedPlace> StartPoints { get; } = new List<NamedPlace>
        {
            new NamedPlace("Ga Huế (Đường Bùi Thị Xuân)", new GeoPoint(107.5727, 16.4559)),
            new NamedPlace("Bến thuyền Tòa Khâm (Đường Lê Lợi)", new GeoPoint(107.5936, 16.4605)),
            new NamedPlace("Sân bay Quốc tế Phú Bài", new GeoPoint(107.7025, 16.4014)),
            new NamedPlace("Bến xe phía Nam TP. Huế", new GeoPoint(107.5530, 16.4380)),
            new NamedPlace("Khách sạn đang ở", null),
        }.AsReadOnly();

        /// <summary>Position of a sample POI by its id; false when it is not one of the sample places.</summary>
        public static bool TryGetPoi(string poiId, out GeoPoint point)
        {
            point = default(GeoPoint);
            return poiId != null && Pois.TryGetValue(poiId, out point);
        }
    }
}
