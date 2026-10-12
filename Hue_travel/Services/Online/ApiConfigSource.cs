using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hue_travel.Services.Online
{
    /// <summary>Settings the server announces to every app (today: where the base map tiles come from).</summary>
    public sealed class ApiConfigSource
    {
        public IDictionary<string, string> TileUrls { get; private set; } = new Dictionary<string, string>();

        public string TileAttribution { get; private set; } = string.Empty;

        /// <summary>Reads /v1/config; a missing or unreadable answer gives empty settings, never an error.</summary>
        public static async Task<ApiConfigSource> FetchAsync(ApiClient api, CancellationToken cancellationToken)
        {
            var result = new ApiConfigSource();
            ApiResponse r = await api.GetAsync("/v1/config", cancellationToken).ConfigureAwait(true);
            if (!r.IsSuccess || r.Object == null) return result;

            result.TileAttribution = JsonHelper.Str(r.Object, "tileAttribution") ?? string.Empty;
            IDictionary<string, object> urls = JsonHelper.Obj(r.Object, "tileUrls");
            if (urls == null) return result;
            foreach (KeyValuePair<string, object> pair in urls)
            {
                string url = pair.Value as string;
                if (!string.IsNullOrWhiteSpace(url)) result.TileUrls[pair.Key] = url;
            }
            return result;
        }
    }
}
