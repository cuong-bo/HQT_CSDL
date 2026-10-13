using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using Hue_travel.Models;
using Hue_travel.Theme;

namespace Hue_travel.MapEngine
{
    /// <summary>
    /// The round badge of each group of places (a white ring, the group's colour and its icon in white): the marker on the
    /// map and the small icon in lists. Drawn at the size asked for, so it stays sharp at any screen scale. The map engine
    /// loads pictures from files, so <see cref="FilePath"/> saves the badge as a PNG once.
    /// </summary>
    public static class PoiMarkerIcons
    {
        private static readonly Dictionary<string, Bitmap> Cache = new Dictionary<string, Bitmap>();

        public static string IconOf(PoiCategory category)
        {
            switch (category)
            {
                case PoiCategory.Heritage: return "account_balance";
                case PoiCategory.Scenery: return "landscape";
                case PoiCategory.Food: return "restaurant";
                case PoiCategory.Stay: return "hotel";
                case PoiCategory.Service: return "support_agent";
                default: return "train";
            }
        }

        public static string NameOf(PoiCategory category)
        {
            switch (category)
            {
                case PoiCategory.Heritage: return "Di tích";
                case PoiCategory.Scenery: return "Cảnh quan";
                case PoiCategory.Food: return "Ẩm thực";
                case PoiCategory.Stay: return "Lưu trú";
                case PoiCategory.Service: return "Dịch vụ";
                default: return "Giao thông";
            }
        }

        /// <summary>The second line of a list row: the type text, led by the group name unless the type already starts with it.</summary>
        public static string Describe(Poi poi)
        {
            string group = NameOf(poi.Category);
            if (string.IsNullOrEmpty(poi.TypeLabel)) return group;
            return poi.TypeLabel.StartsWith(group, StringComparison.CurrentCultureIgnoreCase) ? poi.TypeLabel : group + " · " + poi.TypeLabel;
        }

        /// <summary>The badge as a square bitmap of <paramref name="px"/> pixels (shared, do not dispose).</summary>
        public static Bitmap Badge(PoiCategory category, int px)
        {
            string key = category + "|" + px;
            lock (Cache)
            {
                Bitmap cached;
                if (Cache.TryGetValue(key, out cached)) return cached;
                Bitmap created = Draw(category, px);
                Cache[key] = created;
                return created;
            }
        }

        /// <summary>The badge saved as a PNG file (for the map engine); null when it cannot be written.</summary>
        public static string FilePath(PoiCategory category, int px)
        {
            try
            {
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HueTravel", "markers");
                Directory.CreateDirectory(folder);
                string path = Path.Combine(folder, category.ToString().ToLowerInvariant() + "_" + px + ".png");
                if (!File.Exists(path)) Badge(category, px).Save(path, ImageFormat.Png);
                return path;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("PoiMarkerIcons.FilePath: " + ex.Message);
                return null;
            }
        }

        private static Bitmap Draw(PoiCategory category, int px)
        {
            var bmp = new Bitmap(px, px, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                float margin = Math.Max(1.5f, px * 0.09f);
                var disc = new RectangleF(margin, margin, px - margin * 2f, px - margin * 2f);

                using (var shadow = new SolidBrush(Color.FromArgb(55, 15, 23, 42)))
                {
                    g.FillEllipse(shadow, disc.X, disc.Y + px * 0.04f, disc.Width, disc.Height);
                }
                g.FillEllipse(Brushes.White, disc);
                float ring = Math.Max(1.5f, px * 0.075f);
                var inner = new RectangleF(disc.X + ring, disc.Y + ring, disc.Width - ring * 2f, disc.Height - ring * 2f);
                using (var fill = new SolidBrush(PoiMarkerStyle.ColorOf(category))) g.FillEllipse(fill, inner);

                int glyph = (int)Math.Round(inner.Width * 0.62f);
                g.DrawImage(IconFont.Get(IconOf(category), glyph, Color.White), inner.X + (inner.Width - glyph) / 2f, inner.Y + (inner.Height - glyph) / 2f, glyph, glyph);
            }
            return bmp;
        }
    }
}
