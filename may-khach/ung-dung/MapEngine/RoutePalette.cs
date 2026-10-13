using System.Drawing;

namespace Hue_travel.MapEngine
{
    /// <summary>The colour of each leg of a trip, the same on the map, in the itinerary list and in the leg list.</summary>
    public static class RoutePalette
    {
        private static readonly Color[] Colors =
        {
            Color.FromArgb(0, 122, 204),    // blue
            Color.FromArgb(234, 88, 12),    // orange
            Color.FromArgb(22, 163, 74),    // green
            Color.FromArgb(147, 51, 234),   // purple
            Color.FromArgb(220, 38, 38),    // red
            Color.FromArgb(13, 148, 136),   // teal
            Color.FromArgb(219, 39, 119),   // pink
            Color.FromArgb(79, 70, 229),    // indigo
        };

        public static Color ColorOf(int leg) => Colors[((leg % Colors.Length) + Colors.Length) % Colors.Length];
    }
}
