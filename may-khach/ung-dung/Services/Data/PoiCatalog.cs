using System;
using System.Collections.Generic;
using System.IO;
using Hue_travel.Models;
using Hue_travel.SampleData;

namespace Hue_travel.Services.Data
{
    /// <summary>The points of interest the app shows: the real Shapefile data, or the built-in samples if it cannot be read.</summary>
    public static class PoiCatalog
    {
        private static IReadOnlyList<Poi> current;

        public static string DefaultDataDir => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "shp");

        /// <summary>Why the real data could not be used (null when it loaded, or has not been tried).</summary>
        public static string LastError { get; private set; }

        /// <summary>The places in use right now: the bundled data at first, replaced by the server's list when it arrives.</summary>
        public static IReadOnlyList<Poi> All => current ?? (current = Load(DefaultDataDir));

        /// <summary>Raised (on the UI thread) after <see cref="Replace"/>.</summary>
        public static event EventHandler Changed;

        public static void Replace(IReadOnlyList<Poi> pois)
        {
            if (pois == null || pois.Count == 0) return;
            current = pois;
            Changed?.Invoke(null, EventArgs.Empty);
        }

        public static IReadOnlyList<Poi> Load(string dataDir)
        {
            try
            {
                IReadOnlyList<Poi> real = ShapefilePoiRepository.Load(dataDir);
                if (real.Count > 0)
                {
                    LastError = null;
                    return real;
                }
                LastError = "No points found in " + dataDir;
            }
            catch (Exception ex)   // a fallback boundary: whatever is wrong with the files, the app must still start
            {
                LastError = ex.Message; // surfaced through LastError; the app stays usable on the samples
            }
            return SamplePois.All;
        }
    }
}
