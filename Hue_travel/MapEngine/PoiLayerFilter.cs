using System.Collections.Generic;
using Hue_travel.Models;

namespace Hue_travel.MapEngine
{
    /// <summary>
    /// Decides which points-of-interest layers are shown when the left rail picks a category
    /// (null = every category). The layers themselves are added by the data code (PostGIS) later.
    /// </summary>
    public sealed class PoiLayerFilter
    {
        private readonly Dictionary<int, PoiCategory> categoryByLayer = new Dictionary<int, PoiCategory>();
        private PoiCategory? active;

        /// <summary>Handles of every layer registered so far.</summary>
        public IReadOnlyCollection<int> Handles => categoryByLayer.Keys;

        public void Add(PoiCategory category, int layerHandle)
        {
            categoryByLayer[layerHandle] = category;
        }

        /// <summary>Shows only the layers of this category; null shows all of them.</summary>
        public void ShowOnly(PoiCategory? category)
        {
            active = category;
        }

        /// <summary>True when the layer is registered and its category is currently shown.</summary>
        public bool IsVisible(int layerHandle)
        {
            PoiCategory category;
            return categoryByLayer.TryGetValue(layerHandle, out category) && (active == null || active == category);
        }
    }
}
