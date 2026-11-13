using System.Drawing;

namespace Hue_travel.MapEngine
{
    /// <summary>The colour of each leg of a trip, the same on the map, in the itinerary list and in the leg list.</summary>
    public static class RoutePalette
    {
        private static readonly Color[] Colors =
        {
            Color.FromArgb(0, 122, 204),    // xanh dương
            Color.FromArgb(234, 88, 12),    // cam
            Color.FromArgb(22, 163, 74),    // xanh lá
            Color.FromArgb(220, 38, 38),    // đỏ
            Color.FromArgb(13, 148, 136),   // teal
            Color.FromArgb(219, 39, 119),   // hồng
            Color.FromArgb(79, 70, 229),    // chàm
            Color.FromArgb(202, 138, 4),    // vàng đất
        };

        public static Color ColorOf(int leg) => Colors[((leg % Colors.Length) + Colors.Length) % Colors.Length];
    }
}
