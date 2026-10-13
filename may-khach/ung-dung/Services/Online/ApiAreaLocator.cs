using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Hue_travel.Services.Area;

namespace Hue_travel.Services.Online
{
    /// <summary>
    /// Names the commune under the map centre from the server. The answer is looked up in the background and kept per
    /// 500 m cell; until it arrives the previous (or the demo) name is shown, so panning never waits on the network.
    /// </summary>
    public sealed class ApiAreaLocator : IAreaLocator
    {
        private const double CellDegrees = 0.005;
        private const int MaxCached = 500;

        private readonly ApiClient api;
        private readonly IAreaLocator fallback;
        private readonly Dictionary<string, string> cache = new Dictionary<string, string>();
        private string lastKnown;
        private bool fetching;
        private string pendingKey;
        private double pendingLongitude, pendingLatitude;

        public ApiAreaLocator(ApiClient api, IAreaLocator fallback)
        {
            this.api = api;
            this.fallback = fallback;
        }

        /// <summary>Raised (on the UI thread) when a new name has arrived.</summary>
        public event EventHandler Updated;

        public string Locate(double longitude, double latitude)
        {
            string key = Math.Floor(longitude / CellDegrees).ToString(CultureInfo.InvariantCulture) + ":" + Math.Floor(latitude / CellDegrees).ToString(CultureInfo.InvariantCulture);
            string name;
            if (cache.TryGetValue(key, out name)) return lastKnown = name;
            Fetch(key, longitude, latitude);
            return lastKnown ?? fallback.Locate(longitude, latitude);
        }

        // Fire and forget by design (it feeds a status-bar label); every failure is absorbed here.
        private async void Fetch(string key, double longitude, double latitude)
        {
            if (fetching)
            {
                pendingKey = key;   // fetch it as soon as the current call ends
                pendingLongitude = longitude;
                pendingLatitude = latitude;
                return;
            }
            fetching = true;
            try
            {
                ApiResponse r = await api.GetAsync(string.Format(CultureInfo.InvariantCulture, "/v1/area?lon={0}&lat={1}", longitude, latitude)).ConfigureAwait(true);
                string name = r.IsSuccess ? JsonHelper.Str(r.Object, "name") : null;
                if (name == null && r.StatusCode != 404) return;   // unreachable or a server error: do not remember it, try again later
                if (cache.Count >= MaxCached) cache.Clear();
                cache[key] = name ?? fallback.Locate(longitude, latitude);
                fetching = false;
                Updated?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ApiAreaLocator: " + ex.Message);
            }
            finally
            {
                fetching = false;
                string next = pendingKey;
                pendingKey = null;
                if (next != null && !cache.ContainsKey(next)) Fetch(next, pendingLongitude, pendingLatitude);
            }
        }
    }
}
