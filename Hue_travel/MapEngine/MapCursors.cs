using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Hue_travel.Theme;

namespace Hue_travel.MapEngine
{
    /// <summary>
    /// The pointers over the map, as in other web maps: an open hand to drag the map, a smaller closed-looking hand while dragging,
    /// the pointing hand over a place. Drawn at the screen's resolution with a white edge so they read on any map colour.
    /// </summary>
    internal static class MapCursors
    {
        private static IntPtr openHand, grabbing;

        /// <summary>Handle of the open hand (map idle).</summary>
        public static IntPtr OpenHand => openHand != IntPtr.Zero ? openHand : (openHand = Create(Ui.S(30), 1.0f));

        /// <summary>Handle of the hand while the map is being dragged.</summary>
        public static IntPtr Grabbing => grabbing != IntPtr.Zero ? grabbing : (grabbing = Create(Ui.S(30), 0.8f));

        /// <summary>Handle of the system pointing hand (over a place you can click).</summary>
        public static IntPtr Pointing => Cursors.Hand.Handle;

        private static IntPtr Create(int size, float glyphScale)
        {
            try
            {
                using (var bmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        int glyph = (int)(size * 0.86f * glyphScale);
                        int x = (size - glyph) / 2, y = (size - glyph) / 2 + (glyphScale < 1f ? Ui.S(2) : 0);
                        // white edge: the glyph in white drawn offset in every direction first
                        Bitmap halo = IconFont.Get("pan_tool", glyph, Color.White);
                        int d = Math.Max(1, Ui.S(1));
                        foreach (var o in new[] { new Point(-d, 0), new Point(d, 0), new Point(0, -d), new Point(0, d), new Point(-d, -d), new Point(d, d), new Point(-d, d), new Point(d, -d) })
                        {
                            g.DrawImage(halo, x + o.X, y + o.Y, glyph, glyph);
                        }
                        g.DrawImage(IconFont.Get("pan_tool", glyph, ThemeColors.Slate800), x, y, glyph, glyph);
                    }
                    return ToCursorHandle(bmp, size / 2, size / 2);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("MapCursors.Create: " + ex.Message);
                return Cursors.Hand.Handle;
            }
        }

        private static IntPtr ToCursorHandle(Bitmap bmp, int hotX, int hotY)
        {
            IntPtr color = bmp.GetHbitmap(Color.FromArgb(0));
            using (var mask = new Bitmap(bmp.Width, bmp.Height))
            {
                IntPtr maskHandle = mask.GetHbitmap();
                var info = new IconInfo { fIcon = false, xHotspot = hotX, yHotspot = hotY, hbmMask = maskHandle, hbmColor = color };
                IntPtr cursor = CreateIconIndirect(ref info);
                DeleteObject(color);
                DeleteObject(maskHandle);
                return cursor;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IconInfo
        {
            public bool fIcon;
            public int xHotspot, yHotspot;
            public IntPtr hbmMask, hbmColor;
        }

        [DllImport("user32.dll")] private static extern IntPtr CreateIconIndirect(ref IconInfo info);
        [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr obj);
    }
}
