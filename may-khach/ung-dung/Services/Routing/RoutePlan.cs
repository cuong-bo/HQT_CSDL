using System.Collections.Generic;

namespace Hue_travel.Services.Routing
{
    /// <summary>The best visiting order for a set of stops and what it costs.</summary>
    public sealed class RoutePlan
    {
        public RoutePlan(IReadOnlyList<int> order, IReadOnlyList<double> legsKm, double totalKm, int travelMinutes, int savedMinutes)
        {
            Order = order;
            LegsKm = legsKm;
            TotalKm = totalKm;
            TravelMinutes = travelMinutes;
            SavedMinutes = savedMinutes;
        }

        /// <summary>Indexes into the stop list, in the order they should be visited.</summary>
        public IReadOnlyList<int> Order { get; }

        /// <summary>Length of each hop: start to first stop (when there is a start), then stop to stop.</summary>
        public IReadOnlyList<double> LegsKm { get; }

        public double TotalKm { get; }

        public int TravelMinutes { get; }

        /// <summary>Minutes saved against visiting the stops in the order they were listed (never negative).</summary>
        public int SavedMinutes { get; }
    }
}
