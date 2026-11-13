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
                case PoiCategory.Heritage: return ThemeColors.CatHeritage;
                case PoiCategory.Scenery: return ThemeColors.CatScenery;
                case PoiCategory.Food: return ThemeColors.CatFood;
                case PoiCategory.Stay: return ThemeColors.CatStay;
                case PoiCategory.Service: return ThemeColors.CatService;
                default: return ThemeColors.CatTransport;
            }
        }
    }
}
