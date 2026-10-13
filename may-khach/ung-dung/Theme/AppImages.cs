using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace Hue_travel.Theme
{
    /// <summary>Raster assets embedded in the assembly: the Hue Travel logo (sign-in picture, brand mark, application icon).</summary>
    public static class AppImages
    {
        private const string IconResource = "Hue_travel.Resources.hue-travel.ico";
        private static readonly object LogoCache = new object();
        private static Icon appIcon;

        /// <summary>The Hue Travel icon every window of the application shows (title bar, taskbar, Alt+Tab); shared, do not dispose.</summary>
        public static Icon AppIcon()
        {
            lock (LogoCache)
            {
                if (appIcon != null) return appIcon;
                try
                {
                    using (Stream stream = typeof(AppImages).Assembly.GetManifestResourceStream(IconResource))
                    {
                        appIcon = stream == null ? SystemIcons.Application : new Icon(stream);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("AppImages.AppIcon: " + ex.Message);
                    appIcon = SystemIcons.Application;
                }
                return appIcon;
            }
        }

        private static Bitmap loginBackground;

        /// <summary>The sign-in screen picture (photo, logo and the sign-in card, 1536 x 1024; shared, do not dispose).</summary>
        public static Bitmap LoginBackground()
        {
            lock (LogoCache)
            {
                if (loginBackground != null) return loginBackground;
                try
                {
                    using (Stream stream = typeof(AppImages).Assembly.GetManifestResourceStream("Hue_travel.Resources.dangnhap.jpg"))
                    {
                        if (stream == null) loginBackground = new Bitmap(1, 1);
                        else using (var source = new Bitmap(stream)) loginBackground = new Bitmap(source);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("AppImages.LoginBackground: " + ex.Message);
                    loginBackground = new Bitmap(1, 1);
                }
                return loginBackground;
            }
        }


        // The brand mark of the sign-in picture (house icon and "Hue Travel"), in source pixels of dangnhap.jpg.
        private static readonly Rectangle BrandMarkSource = new Rectangle(575, 195, 1110, 320);
        private static readonly Dictionary<int, Bitmap> BrandCache = new Dictionary<int, Bitmap>();

        /// <summary>Width / height of the brand mark.</summary>
        public static float BrandMarkAspect => BrandMarkSource.Width / (float)BrandMarkSource.Height;

        /// <summary>
        /// The "Hue Travel" mark cut out of the sign-in picture, with the sky around it made transparent, <paramref name="height"/> pixels
        /// high (shared, do not dispose).
        /// </summary>
        public static Bitmap BrandMark(int height)
        {
            lock (LogoCache)
            {
                Bitmap cached;
                if (BrandCache.TryGetValue(height, out cached)) return cached;
                int width = (int)Math.Round(height * BrandMarkAspect);
                var result = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                try
                {
                    using (var g = Graphics.FromImage(result))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        g.DrawImage(LoginBackground(), new Rectangle(0, 0, width, height), BrandMarkSource, GraphicsUnit.Pixel);
                    }
                    // The mark is dark purple and gold on a light sky: the darker the pixel, the more opaque it is.
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            Color c = result.GetPixel(x, y);
                            double alpha = Math.Max(0, Math.Min(1, (208 - Math.Min(c.R, Math.Min(c.G, c.B))) / 55.0));
                            result.SetPixel(x, y, Color.FromArgb((int)Math.Round(alpha * 255), c.R, c.G, c.B));
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("AppImages.BrandMark: " + ex.Message);
                }
                BrandCache[height] = result;
                return result;
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
    }
}
