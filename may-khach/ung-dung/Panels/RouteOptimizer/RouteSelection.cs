using System.Collections.Generic;
using System.Linq;
using Hue_travel.Models;

namespace Hue_travel.Panels.RouteOptimizer
{
    /// <summary>Pure renumbering logic, separated from the UserControl. "Order" always reflects current list position (1..N), never the original click order with gaps.</summary>
    public static class RouteSelection
    {
        public static IReadOnlyList<(int Order, Poi Poi)> BuildOrderedSummary(IReadOnlyList<Poi> selectedInClickOrder)
        {
            if (selectedInClickOrder == null) return new List<(int, Poi)>();

            return selectedInClickOrder
                .Select((poi, index) => (Order: index + 1, Poi: poi))
                .ToList();
        }
    }
}
