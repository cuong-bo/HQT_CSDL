namespace Hue_travel.Models
{
    public sealed class Poi
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public PoiCategory Category { get; set; }
        public string Address { get; set; }
        public string OpeningHours { get; set; }
        public string TicketPrice { get; set; }
        public double Rating { get; set; }
        public double DistanceKm { get; set; }
        public string Description { get; set; }

        // Real-data fields (Shapefile catalogue)
        public string TypeLabel { get; set; }     // the data set's own type text, e.g. "Khách sạn"
        public string Summary { get; set; }       // one line shown under the name in search results
        public string Phone { get; set; }
        public string Website { get; set; }
        public double Longitude { get; set; }     // degrees, WGS84 / VN-2000 (0 when unknown)
        public double Latitude { get; set; }
        public double X { get; set; }             // VN-2000 UTM 48N, metres (0 when unknown)
        public double Y { get; set; }

        public bool HasLocation => Longitude != 0 || Latitude != 0;
    }
}
