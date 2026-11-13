using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using Hue_travel.MapEngine;
using Hue_travel.Models;
using Hue_travel.Services.Data;
using Hue_travel.Theme;

namespace Hue_travel.Panels.Shared
{
    /// <summary>First photo of a place as a small rounded thumbnail, kept in a small cache (the file system is never asked while painting).</summary>
    internal static class PoiThumbs
    {
        private const int MaxEntries = 160;
        private static readonly Dictionary<string, Bitmap> Cache = new Dictionary<string, Bitmap>();   // key: id|size; a null value means "no photo"

        public static void Draw(Graphics g, Poi poi, Rectangle box, bool dim)
        {
            using (GraphicsPath clip = Ui.RoundedRect(box, Ui.S(ThemeMetrics.RadiusMd)))
            {
                Bitmap photo = Find(poi.Id, box.Width);
                GraphicsState state = g.Save();
                g.SetClip(clip, CombineMode.Intersect);
                if (photo != null)
                {
                    // "cover": fill the box, cropping the longer side
                    float scale = System.Math.Max(box.Width / (float)photo.Width, box.Height / (float)photo.Height);
                    float w = photo.Width * scale, h = photo.Height * scale;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(photo, box.X + (box.Width - w) / 2, box.Y + (box.Height - h) / 2, w, h);
                }
                else
                {
                    using (var fill = new SolidBrush(Ui.Lerp(PoiMarkerStyle.ColorOf(poi.Category), Color.White, 0.82f))) g.FillRectangle(fill, box);
                    int icon = (int)(box.Width * 0.5);
                    g.DrawImage(IconFont.Get(PoiMarkerIcons.IconOf(poi.Category), icon, PoiMarkerStyle.ColorOf(poi.Category)),
                        box.X + (box.Width - icon) / 2, box.Y + (box.Height - icon) / 2, icon, icon);
                }
                g.Restore(state);
            }
            if (dim)
            {
                using (GraphicsPath path = Ui.RoundedRect(box, Ui.S(ThemeMetrics.RadiusMd)))
                using (var veil = new SolidBrush(Color.FromArgb(150, ThemeColors.Paper))) g.FillPath(veil, path);
            }
        }

        private static Bitmap Find(string id, int size)
        {
            string key = id + "|" + size;
            lock (Cache)
            {
                Bitmap cached;
                if (Cache.TryGetValue(key, out cached)) return cached;
                if (Cache.Count > MaxEntries) Clear();
                IReadOnlyList<string> paths = PoiPhotos.PathsFor(id);
                Bitmap source = paths.Count > 0 ? PoiPhotos.Load(paths[0], size * 2) : null;
                Bitmap copy = null;
                if (source != null)
                {
                    // PoiPhotos may dispose its cached bitmap when its cache fills: keep our own copy.
                    copy = new Bitmap(source.Width, source.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    using (Graphics g = Graphics.FromImage(copy)) g.DrawImage(source, 0, 0);
                }
                Cache[key] = copy;   // a thumbnail is shared, never disposed here
                return copy;
            }
        }

        /// <summary>Forget what was found (call when the server's photos finished downloading).</summary>
        public static void Clear()
        {
            lock (Cache)
            {
                foreach (Bitmap bmp in Cache.Values) if (bmp != null) bmp.Dispose();
                Cache.Clear();
            }
        }
    }
}
