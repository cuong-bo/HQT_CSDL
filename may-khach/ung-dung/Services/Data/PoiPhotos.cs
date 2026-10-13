using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Hue_travel.Services.Data
{
    /// <summary>
    /// The photos of the places. Each place has its own folder, <c>Data\Photos\&lt;place id&gt;\</c> next to the program, with up to
    /// eight .jpg / .jpeg / .png files shown in name order (1.jpg, 2.jpg, ...). Photos are read lazily and scaled once, so the
    /// card and the full-size viewer stay quick.
    /// </summary>
    public static class PoiPhotos
    {
        public const int MaxPhotos = 8;

        private static readonly Regex SafeId = new Regex(@"^[A-Za-z0-9_\-]+$", RegexOptions.Compiled);
        private static readonly string[] Extensions = { ".jpg", ".jpeg", ".png" };
        private static readonly Dictionary<string, Bitmap> Cache = new Dictionary<string, Bitmap>();

        public static string Folder => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Photos");

        /// <summary>The photo files of a place in showing order; empty when it has none.</summary>
        public static IReadOnlyList<string> PathsFor(string placeId)
        {
            if (string.IsNullOrEmpty(placeId) || !SafeId.IsMatch(placeId)) return new string[0];   // an id never reaches outside the photo folder
            string folder = Path.Combine(Folder, placeId);
            IReadOnlyList<string> downloaded = PhotoStore.PathsFor(placeId);   // the server's approved photos come first
            try
            {
                if (!Directory.Exists(folder)) return downloaded;
                IEnumerable<string> shipped = Directory.GetFiles(folder)
                    .Where(f => Extensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
                    .OrderBy(f => f, StringComparer.OrdinalIgnoreCase);
                return downloaded.Concat(shipped).Take(MaxPhotos).ToList();
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                System.Diagnostics.Debug.WriteLine("PoiPhotos.PathsFor: " + ex.Message);
                return downloaded;
            }
        }

        /// <summary>The photo scaled down so its longer side is at most <paramref name="maxSide"/>; shared, do not dispose. Null when it cannot be read.</summary>
        public static Bitmap Load(string path, int maxSide)
        {
            string key = path + "|" + maxSide;
            lock (Cache)
            {
                Bitmap cached;
                if (Cache.TryGetValue(key, out cached)) return cached;
                if (Cache.Count > 48) ClearCache();
                Bitmap created = Read(path, maxSide);
                if (created != null) Cache[key] = created;
                return created;
            }
        }

        private static void ClearCache()
        {
            foreach (Bitmap bitmap in Cache.Values) bitmap.Dispose();
            Cache.Clear();
        }

        private static Bitmap Read(string path, int maxSide)
        {
            try
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (Image source = Image.FromStream(stream, false, true))
                {
                    float scale = Math.Min(1f, maxSide / (float)Math.Max(source.Width, source.Height));
                    int w = Math.Max(1, (int)Math.Round(source.Width * scale)), h = Math.Max(1, (int)Math.Round(source.Height * scale));
                    var result = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                    using (Graphics g = Graphics.FromImage(result))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        g.DrawImage(source, 0, 0, w, h);
                    }
                    return result;
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is OutOfMemoryException)
            {
                System.Diagnostics.Debug.WriteLine("PoiPhotos.Read: " + ex.Message);
                return null;   // a broken or missing file is skipped, never a crash
            }
        }
    }
}
