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
        private static readonly Lazy<IReadOnlyList<Poi>> Cached =
            new Lazy<IReadOnlyList<Poi>>(() => Load(DefaultDataDir));

        public static string DefaultDataDir => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

        /// <summary>Why the real data could not be used (null when it loaded, or has not been tried).</summary>
        public static string LastError { get; private set; }

        public static IReadOnlyList<Poi> All => Cached.Value;

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
