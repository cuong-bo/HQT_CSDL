using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hue_travel.Models;

namespace Hue_travel.Services.Online
{
    /// <summary>All places from the server (paged), and a copy kept on disk for when the server cannot be reached.</summary>
    public static class ApiPoiSource
    {
        private const int PageSize = 100;
        private const int MaxPages = 50;

        public static async Task<IReadOnlyList<Poi>> FetchAllAsync(ApiClient api, CancellationToken ct)
        {
            var all = new List<Poi>();
            for (int page = 1; page <= MaxPages; page++)
            {
                ApiResponse r = await api.GetAsync("/v1/pois?pageSize=" + PageSize + "&page=" + page, ct).ConfigureAwait(true);
                if (!r.IsSuccess) throw new ApiException(r.Error);
                IList<object> items = JsonHelper.List(r.Object, "items");
                if (items == null || items.Count == 0) break;
                all.AddRange(items.OfType<IDictionary<string, object>>().Select(PoiJson.ToPoi));
                if (all.Count >= JsonHelper.Long(r.Object, "total")) break;
            }
            return all;
        }
    }

    /// <summary>The last list of places received from the server, in %LocalAppData%\CoDoGIS\pois.json.</summary>
    public static class PoiCache
    {
        private static string DefaultPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CoDoGIS", "pois.json");

        public static void TrySave(IReadOnlyList<Poi> pois, string path = null)
        {
            path = path ?? DefaultPath;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, JsonHelper.Serialize(pois.Select(PoiJson.ToDictionary).ToList()));
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                Debug.WriteLine("PoiCache.TrySave: " + ex.Message);
            }
        }

        /// <summary>The cached places, or null when there is no usable cache.</summary>
        public static IReadOnlyList<Poi> TryLoad(string path = null)
        {
            path = path ?? DefaultPath;
            try
            {
                if (!File.Exists(path)) return null;
                IList<object> items = JsonHelper.AsList(JsonHelper.Parse(File.ReadAllText(path)));
                if (items == null) return null;
                List<Poi> pois = items.OfType<IDictionary<string, object>>().Select(PoiJson.ToPoi).Where(p => p.Id.Length > 0).ToList();
                return pois.Count > 0 ? pois : null;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                Debug.WriteLine("PoiCache.TryLoad: " + ex.Message);
                return null;
            }
        }
    }
}
