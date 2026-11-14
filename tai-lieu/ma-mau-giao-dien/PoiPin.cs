using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Hue_travel.MapEngine
{
    /// <summary>
    /// The teardrop pin that marks a place on the map. The bitmap is <c>px</c> wide and about 1.28 x <c>px</c> tall, and its TIP is the
    /// bottom centre: MapWinGIS stands a picture symbol on its point with the bottom centre on it, so the tip lands on the place.
    /// </summary>
    public static class PoiPin
    {
        private const float HeightRatio = 1.28f;

        public static int HeightOf(int px) => (int)Math.Round(px * HeightRatio);

        private static float Margin(int px) => Math.Max(2f, px * 0.06f);

        private static float HeadRadius(int px) => px * 0.40f;

        /// <summary>Distance from the tip (the place) up to the middle of the round head, in pixels: where the pulse ring and the hit test centre.</summary>
        public static float HeadCenterAboveTip(int px) => HeightOf(px) - Margin(px) - (Margin(px) + HeadRadius(px));

        /// <summary>Diameter of the round head, for the pulse ring.</summary>
        public static int HeadDiameter(int px) => (int)Math.Round(HeadRadius(px) * 2);

        /// <param name="glyph">The white icon drawn in the head (may be null).</param>
        public static Bitmap Draw(int px, Color body, Bitmap glyph)
        {
            int h = HeightOf(px);
            var bmp = new Bitmap(px, h, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                float m = Margin(px), r = HeadRadius(px);
                float cx = px / 2f, cy = m + r, tipY = h - m;

                // a small ground shadow under the tip
                using (var ground = new SolidBrush(Color.FromArgb(70, 36, 27, 47)))
                {
                    g.FillEllipse(ground, cx - px * 0.15f, tipY - px * 0.035f, px * 0.30f, px * 0.08f);
                }

                using (GraphicsPath pin = Teardrop(cx, cy, r, tipY))
                {
                    float ring = Math.Max(1.5f, px * 0.06f);
                    using (var white = new Pen(Color.White, ring * 2f) { LineJoin = LineJoin.Round }) g.DrawPath(white, pin);
                    using (var fill = new SolidBrush(body)) g.FillPath(fill, pin);
                }

                if (glyph != null)
                {
                    float size = r * 1.15f;
                    g.DrawImage(glyph, cx - size / 2f, cy - size / 2f, size, size);
                }
            }
            return bmp;
        }

        /// <summary>
        /// For the case that MapWinGIS CENTRES a picture symbol on its point instead of standing it on it (check this in step 8.2 of the
        /// guide): the pin goes in the top half of a bitmap twice as tall, so the tip lands on the bitmap's centre, i.e. on the place.
        /// </summary>
        public static Bitmap PadForCentreAnchor(Bitmap pin)
        {
            var padded = new Bitmap(pin.Width, pin.Height * 2, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(padded)) g.DrawImage(pin, 0, 0, pin.Width, pin.Height);
            return padded;
        }

        // A circle with two straight sides running down to a point: the sides touch the circle where a line from the tip is tangent.
        private static GraphicsPath Teardrop(float cx, float cy, float r, float tipY)
        {
            double d = tipY - cy;
            double alpha = Math.Acos(Math.Min(1.0, r / d)) * 180.0 / Math.PI;
            var path = new GraphicsPath();
            path.AddArc(cx - r, cy - r, r * 2, r * 2, (float)(90 + alpha), (float)(360 - 2 * alpha));
            path.AddLine(path.GetLastPoint(), new PointF(cx, tipY));
            path.CloseFigure();
            return path;
        }
    }
}
