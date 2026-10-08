using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace Hue_travel.Theme
{
    /// <summary>Raster assets embedded in the assembly (the Cố Đô GIS logo from the mockup).</summary>
    public static class AppImages
    {
        private const string LogoResource = "Hue_travel.Resources.logo.jpg";
        private static readonly Dictionary<int, Bitmap> LogoCache = new Dictionary<int, Bitmap>();

        /// <summary>The logo scaled to a square of <paramref name="px"/>; shared, do not dispose.</summary>
        public static Bitmap Logo(int px)
        {
            lock (LogoCache)
            {
                Bitmap cached;
                if (LogoCache.TryGetValue(px, out cached)) return cached;
                Bitmap created = ScaleLogo(px);
                LogoCache[px] = created;
                return created;
            }
        }

        private static Bitmap poiThumbnail;

        /// <summary>The Đại Nội photo the mockup uses for result cards and the identify card (shared, do not dispose).</summary>
        public static Bitmap PoiThumbnail()
        {
            lock (LogoCache)
            {
                if (poiThumbnail != null) return poiThumbnail;
                try
                {
                    using (Stream stream = typeof(AppImages).Assembly.GetManifestResourceStream("Hue_travel.Resources.poi_thumb.jpg"))
                    {
                        if (stream == null) poiThumbnail = new Bitmap(1, 1);
                        else using (var source = new Bitmap(stream)) poiThumbnail = new Bitmap(source);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("AppImages.PoiThumbnail: " + ex.Message);
                    poiThumbnail = new Bitmap(1, 1);
                }
                return poiThumbnail;
            }
        }

        /// <summary>A new bitmap of the POI photo cropped to w x h (object-fit: cover) with rounded corners.</summary>
        public static Bitmap PoiPhoto(int width, int height, int radius)
        {
            var result = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(result))
            using (GraphicsPath clip = Ui.RoundedRect(new RectangleF(0, 0, width, height), radius))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.SetClip(clip, CombineMode.Intersect);
                DrawCover(g, PoiThumbnail(), new Rectangle(0, 0, width, height));
            }
            return result;
        }

        /// <summary>Draws <paramref name="image"/> into <paramref name="target"/> like CSS object-fit: cover.</summary>
        public static void DrawCover(Graphics g, Image image, Rectangle target)
        {
            float scale = Math.Max(target.Width / (float)image.Width, target.Height / (float)image.Height);
            float w = image.Width * scale, h = image.Height * scale;
            var dest = new RectangleF(target.X + (target.Width - w) / 2f, target.Y + (target.Height - h) / 2f, w, h);
            var state = g.Save();
            g.SetClip(target, CombineMode.Intersect);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(image, dest);
            g.Restore(state);
        }

        private static Bitmap ScaleLogo(int px)
        {
            var result = new Bitmap(px, px, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            try
            {
                using (Stream stream = typeof(AppImages).Assembly.GetManifestResourceStream(LogoResource))
                {
                    if (stream == null) return result;
                    using (var source = new Bitmap(stream))
                    using (var g = Graphics.FromImage(result))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        g.DrawImage(source, new Rectangle(0, 0, px, px));
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("AppImages.Logo: " + ex.Message);
            }
            return result;
        }
    }
}
