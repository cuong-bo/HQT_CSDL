using System.Collections.Generic;

namespace Hue_travel.MapEngine
{
    /// <summary>One selectable base map: web tiles streamed through a MapWinGIS custom tile provider.</summary>
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

        /// <summary>MapWinGIS tile URL pattern ({x}, {y}, {zoom}).</summary>
        public string TileUrl { get; }

        /// <summary>Id of the custom MapWinGIS tile provider.</summary>
        public int ProviderId { get; }

        /// <summary>Credit line shown on the map.</summary>
        public string Attribution { get; }
    }

    /// <summary>
    /// The base maps of the "Chon lop nen" switcher. TEMPORARY: the tiles are Google's public tile addresses,
    /// which Google's terms do not allow in a shipped product. Replace the URL patterns below (Esri, MapTiler, an
    /// own tile server) before the app is distributed; nothing else needs to change.
    /// </summary>
    public static class BaseMapCatalog
    {
        public const string DefaultId = "roadmap";
        public const int MinZoom = 1;
        public const int MaxZoom = 20;

        private const string GoogleCredit = "Bản đồ © Google";
        private const int FirstProviderId = 10001; // MapWinGIS reserves the low ids for its built-in providers

        private const int ProviderIdsPerGeneration = 10;

        private static readonly BaseMapInfo[] Builtin =
        {
            new BaseMapInfo("roadmap", "Đường phố", "http://mt1.google.com/vt/lyrs=m&x={x}&y={y}&z={zoom}", FirstProviderId, GoogleCredit),
            new BaseMapInfo("satellite", "Vệ tinh", "http://mt1.google.com/vt/lyrs=s&x={x}&y={y}&z={zoom}", FirstProviderId + 1, GoogleCredit),
            new BaseMapInfo("hybrid", "Lai", "http://mt1.google.com/vt/lyrs=y&x={x}&y={y}&z={zoom}", FirstProviderId + 2, GoogleCredit),
            new BaseMapInfo("terrain", "Địa hình", "http://mt1.google.com/vt/lyrs=p&x={x}&y={y}&z={zoom}", FirstProviderId + 3, GoogleCredit),
        };

        private static int generation;

        public static IReadOnlyList<BaseMapInfo> All { get; private set; } = Builtin;

        /// <summary>
        /// Replaces the tile addresses (and credit line) with the ones the server announces, so a licensed tile source can
        /// be switched on without a new release. Entries that are not a valid http(s) address with {x}, {y} and {zoom} are
        /// ignored. Fresh provider ids are used because MapWinGIS cannot change a provider once it is registered.
        /// Returns true when something changed and the current base map should be applied again.
        /// </summary>
        public static bool ApplyOverrides(IDictionary<string, string> urls, string attribution)
        {
            if (urls == null || urls.Count == 0) return false;
            var next = new List<BaseMapInfo>();
            bool changed = false;
            for (int i = 0; i < Builtin.Length; i++)
            {
                BaseMapInfo basis = Builtin[i];
                string url;
                if (!urls.TryGetValue(basis.Id, out url) || !IsValidTileUrl(url))
                {
                    next.Add(basis);
                    continue;
                }
                if (!changed) generation++;
                changed = true;
                next.Add(new BaseMapInfo(basis.Id, basis.Name, url.Trim(), FirstProviderId + generation * ProviderIdsPerGeneration + i,
                    string.IsNullOrWhiteSpace(attribution) ? string.Empty : attribution.Trim()));   // never keep Google's credit for another source
            }
            if (!changed) return false;
            All = next.AsReadOnly();
            return true;
        }

        internal static bool IsValidTileUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            url = url.Trim();
            if (!url.StartsWith("http://", System.StringComparison.OrdinalIgnoreCase) && !url.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase)) return false;
            return url.Contains("{x}") && url.Contains("{y}") && url.Contains("{zoom}");
        }

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
