using System.Drawing;
using Hue_travel.Models;
using Hue_travel.Theme;

namespace Hue_travel.MapEngine
{
    /// <summary>Colour and size of the map marker of each group; the colours match the icons on the left rail.</summary>
    public static class PoiMarkerStyle
    {
        public const int DiameterPixels = 12;

        public static Color ColorOf(PoiCategory category)
        {
            switch (category)
            {
                case PoiCategory.Heritage: return ThemeColors.Amber600;
                case PoiCategory.Scenery: return ThemeColors.Emerald700;
                case PoiCategory.Food: return ThemeColors.Orange600;
                case PoiCategory.Stay: return ThemeColors.Blue600;
                case PoiCategory.Service: return ThemeColors.Sky600;
                default: return ThemeColors.Cyan600;
            }
        }
    }
}
