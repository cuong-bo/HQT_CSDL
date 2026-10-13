namespace Hue_travel.Models
{
    public sealed class RouteStep
    {
        public int Order { get; set; }
        public Poi From { get; set; }
        public Poi To { get; set; }
        public double DistanceKm { get; set; }
        public int DurationMinutes { get; set; }
    }
}
