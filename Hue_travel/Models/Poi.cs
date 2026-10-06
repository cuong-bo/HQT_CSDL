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
    }
}
