using System.Drawing;
using System.Drawing.Drawing2D;

namespace Hue_travel.Theme
{
    /// <summary>Small GDI+ helpers shared by owner-drawn controls and map overlays.</summary>
    public static class Ui
    {
        /// <summary>A rounded-rectangle path. Radius is clamped to half the shorter side (pill shape).</summary>
        public static GraphicsPath RoundedRect(RectangleF bounds, float radius)
        {
            var path = new GraphicsPath();
            float r = System.Math.Max(0f, System.Math.Min(radius, System.Math.Min(bounds.Width, bounds.Height) / 2f));
            if (r < 0.5f)
            {
                path.AddRectangle(bounds);
                return path;
            }

            float d = r * 2f;
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        /// <summary>Linear blend of two colours (all four channels); t = 0 gives <paramref name="from"/>, 1 gives <paramref name="to"/>.</summary>
        public static Color Lerp(Color from, Color to, float t)
        {
            t = System.Math.Max(0f, System.Math.Min(1f, t));
            int Mix(int a, int b) => (int)System.Math.Round(a + (b - a) * t);
            return Color.FromArgb(Mix(from.A, to.A), Mix(from.R, to.R), Mix(from.G, to.G), Mix(from.B, to.B));
        }

        /// <summary>Blends <paramref name="color"/> over white by <paramref name="alpha"/> (0..1), giving an opaque colour.</summary>
        public static Color OverWhite(Color color, double alpha)
        {
            int Mix(int c) => (int)System.Math.Round(255 + (c - 255) * alpha);
            return Color.FromArgb(Mix(color.R), Mix(color.G), Mix(color.B));
        }
    }
}
