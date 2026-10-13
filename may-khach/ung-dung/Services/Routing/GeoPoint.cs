namespace Hue_travel.Services.Routing
{
    /// <summary>A position in degrees (WGS84 / VN-2000 are the same to well under a metre for this purpose).</summary>
    public readonly struct GeoPoint
    {
        public GeoPoint(double longitude, double latitude)
        {
            Longitude = longitude;
            Latitude = latitude;
        }

        public double Longitude { get; }

        public double Latitude { get; }
    }
}
