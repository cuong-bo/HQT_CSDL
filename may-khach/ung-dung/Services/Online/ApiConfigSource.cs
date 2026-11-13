using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hue_travel.Services.Online
{
    /// <summary>Settings the server announces to every app (today: the oldest app version it still accepts).</summary>
    public sealed class ApiConfigSource
    {
        public string MinimumVersion { get; private set; } = "1.0.0";

        /// <summary>True when the server has a DeepSeek key and the assistant is switched on.</summary>
        public bool ChatEnabled { get; private set; }

        /// <summary>Reads /v1/config; a missing or unreadable answer gives the built-in defaults, never an error.</summary>
        public static async Task<ApiConfigSource> FetchAsync(ApiClient api, CancellationToken cancellationToken)
        {
            var result = new ApiConfigSource();
            ApiResponse r = await api.GetAsync("/v1/config", cancellationToken).ConfigureAwait(true);
            if (!r.IsSuccess || r.Object == null) return result;

            string minimum = JsonHelper.Str(r.Object, "minimumClientVersion");
            if (!string.IsNullOrWhiteSpace(minimum)) result.MinimumVersion = minimum.Trim();
            result.ChatEnabled = JsonHelper.Bool(r.Object, "chatEnabled");
            return result;
        }

        /// <summary>True when the running app is older than the oldest version the server accepts.</summary>
        public bool UpdateRequired(string currentVersion)
        {
            Version running, wanted;
            if (!Version.TryParse(currentVersion, out running) || !Version.TryParse(MinimumVersion, out wanted)) return false;
            return Normalize(running) < Normalize(wanted);
        }

        private static Version Normalize(Version version) =>
            new Version(version.Major, version.Minor, Math.Max(version.Build, 0), Math.Max(version.Revision, 0));
    }
}
