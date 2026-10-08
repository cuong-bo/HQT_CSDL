using System.Collections.Generic;

namespace Hue_travel.MapEngine
{
    /// <summary>One selectable base map: either drawn by the app (palette) or streamed as web tiles.</summary>
    public sealed class BaseMapInfo
    {
        public BaseMapInfo(string id, string name, string tileUrl, int providerId, string attribution)
        {
            Id = id;
            Name = name;
            TileUrl = tileUrl;
            ProviderId = providerId;
            Attribution = attribution;
        }

        public string Id { get; }

        public string Name { get; }

        /// <summary>MapWinGIS tile URL pattern ({x}, {y}, {zoom}); null for maps the app draws itself.</summary>
        public string TileUrl { get; }

        /// <summary>Id of the custom MapWinGIS tile provider (tile maps only).</summary>
        public int ProviderId { get; }

        /// <summary>Credit line shown on the map for tile maps.</summary>
        public string Attribution { get; }

        public bool IsTile => TileUrl != null;
    }

    /// <summary>
    /// The base maps of the "Chon lop nen" switcher. TEMPORARY: the tile maps use Google's public tile addresses,
    /// which Google's terms do not allow in a shipped product. Replace the URL patterns below (Esri, MapTiler, an
    /// own tile server) before the app is distributed; nothing else needs to change.
    /// </summary>
    public static class BaseMapCatalog
    {
        public const int MinZoom = 1;
        public const int MaxZoom = 20;

        private const string GoogleCredit = "Bản đồ © Google";
        private const int FirstProviderId = 10001; // MapWinGIS reserves the low ids for its built-in providers

        public static IReadOnlyList<BaseMapInfo> All { get; } = new List<BaseMapInfo>
        {
            new BaseMapInfo("standard", "Chuẩn sáng", null, 0, null),
            new BaseMapInfo("roadmap", "Đường phố", "https://mt1.google.com/vt/lyrs=m&x={x}&y={y}&z={zoom}", FirstProviderId, GoogleCredit),
            new BaseMapInfo("satellite", "Vệ tinh", "https://mt1.google.com/vt/lyrs=s&x={x}&y={y}&z={zoom}", FirstProviderId + 1, GoogleCredit),
            new BaseMapInfo("hybrid", "Lai", "https://mt1.google.com/vt/lyrs=y&x={x}&y={y}&z={zoom}", FirstProviderId + 2, GoogleCredit),
            new BaseMapInfo("terrain", "Địa hình", "https://mt1.google.com/vt/lyrs=p&x={x}&y={y}&z={zoom}", FirstProviderId + 3, GoogleCredit),
            new BaseMapInfo("night", "Ngắm đêm", null, 0, null),
        }.AsReadOnly();

        /// <summary>The base map with this id, or null.</summary>
        public static BaseMapInfo Find(string id)
        {
            foreach (BaseMapInfo info in All)
            {
                if (info.Id == id) return info;
            }
            return null;
        }
    }
}
