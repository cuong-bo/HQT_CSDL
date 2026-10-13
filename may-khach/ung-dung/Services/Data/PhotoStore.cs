using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Hue_travel.Services.Data
{
    /// <summary>
    /// The photos downloaded from the server, kept on this computer so a place shows them at once and without a network:
    /// <c>%LocalAppData%\CoDoGIS\photos\&lt;place id&gt;\&lt;photo id&gt;.jpg</c>, with <c>order.txt</c> listing the photo ids in showing order.
    /// </summary>
    public static class PhotoStore
    {
        private static readonly Regex SafeId = new Regex(@"^[A-Za-z0-9_\-]+$", RegexOptions.Compiled);
        private const string OrderFile = "order.txt";

        public static string Root => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CoDoGIS", "photos");

        private static string FolderOf(string placeId) => Path.Combine(Root, placeId);

        public static string PathOf(string placeId, long photoId) => Path.Combine(FolderOf(placeId), photoId + ".jpg");

        /// <summary>The downloaded photos of a place in showing order; empty when there are none.</summary>
        public static IReadOnlyList<string> PathsFor(string placeId)
        {
            if (string.IsNullOrEmpty(placeId) || !SafeId.IsMatch(placeId)) return new string[0];   // an id never reaches outside the photo folder
            try
            {
                string order = Path.Combine(FolderOf(placeId), OrderFile);
                if (!File.Exists(order)) return new string[0];
                var paths = new List<string>();
                foreach (string line in File.ReadAllLines(order))
                {
                    long id;
                    if (!long.TryParse(line.Trim(), out id)) continue;
                    string file = PathOf(placeId, id);
                    if (File.Exists(file)) paths.Add(file);
                }
                return paths;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                System.Diagnostics.Debug.WriteLine("PhotoStore.PathsFor: " + ex.Message);
                return new string[0];
            }
        }

        public static bool Has(string placeId, long photoId) => SafeId.IsMatch(placeId ?? "") && File.Exists(PathOf(placeId, photoId));

        /// <summary>Saves one downloaded photo.</summary>
        public static void Save(string placeId, long photoId, byte[] jpeg)
        {
            if (!SafeId.IsMatch(placeId ?? "")) return;
            Directory.CreateDirectory(FolderOf(placeId));
            File.WriteAllBytes(PathOf(placeId, photoId), jpeg);
        }

        /// <summary>Records the showing order and removes photos the server no longer lists. True when anything changed.</summary>
        public static bool SetOrder(string placeId, IReadOnlyList<long> ids)
        {
            if (!SafeId.IsMatch(placeId ?? "")) return false;
            string folder = FolderOf(placeId);
            string order = Path.Combine(folder, OrderFile);
            string text = string.Join(Environment.NewLine, ids);
            bool changed = !File.Exists(order) || File.ReadAllText(order).Trim() != text.Trim();
            if (ids.Count == 0)
            {
                if (Directory.Exists(folder)) { Directory.Delete(folder, true); changed = true; }
                return changed;
            }
            Directory.CreateDirectory(folder);
            foreach (string file in Directory.GetFiles(folder, "*.jpg"))
            {
                long id;
                if (!long.TryParse(Path.GetFileNameWithoutExtension(file), out id) || !ids.Contains(id)) { File.Delete(file); changed = true; }
            }
            File.WriteAllText(order, text);
            return changed;
        }
    }
}
