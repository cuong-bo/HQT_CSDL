using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Hue_travel.Theme
{
    // These methods are written to be pasted into IconFont.cs next to DrawClock / DrawTag; each takes the empty bitmap, its size and the colour.
    public static class IconsExtra
    {
        private static Pen Stroke(Color color, float s) =>
            new Pen(color, Math.Max(1.2f, 1.5f * s)) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };

        // "favorite": a heart, outline only, or filled when the name is "favorite_fill".
        public static void DrawHeart(Bitmap bmp, int px, Color color, bool filled)
        {
            float s = px / 16f;
            using (Graphics g = Graphics.FromImage(bmp))
            using (GraphicsPath heart = new GraphicsPath())
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                heart.AddBezier(8f * s, 14f * s, 0.5f * s, 8.6f * s, 1.2f * s, 3f * s, 4.8f * s, 3f * s);
                heart.AddBezier(4.8f * s, 3f * s, 6.6f * s, 3f * s, 7.6f * s, 4.2f * s, 8f * s, 5.2f * s);
                heart.AddBezier(8f * s, 5.2f * s, 8.4f * s, 4.2f * s, 9.4f * s, 3f * s, 11.2f * s, 3f * s);
                heart.AddBezier(11.2f * s, 3f * s, 14.8f * s, 3f * s, 15.5f * s, 8.6f * s, 8f * s, 14f * s);
                heart.CloseFigure();
                if (filled) using (var fill = new SolidBrush(color)) g.FillPath(fill, heart);
                else using (Pen pen = Stroke(color, s)) g.DrawPath(pen, heart);
            }
        }

        // "bookmark": a ribbon with a notch at the bottom.
        public static void DrawBookmark(Bitmap bmp, int px, Color color, bool filled)
        {
            float s = px / 16f;
            PointF[] ribbon = { new PointF(3.8f * s, 2f * s), new PointF(12.2f * s, 2f * s), new PointF(12.2f * s, 14f * s), new PointF(8f * s, 10.8f * s), new PointF(3.8f * s, 14f * s) };
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                if (filled) using (var fill = new SolidBrush(color)) g.FillPolygon(fill, ribbon);
                else using (Pen pen = Stroke(color, s)) g.DrawPolygon(pen, ribbon);
            }
        }

        // "share": three dots joined by two lines.
        public static void DrawShare(Bitmap bmp, int px, Color color)
        {
            float s = px / 16f;
            PointF a = new PointF(12.6f * s, 3.2f * s), b = new PointF(3.4f * s, 8f * s), c = new PointF(12.6f * s, 12.8f * s);
            using (Graphics g = Graphics.FromImage(bmp))
            using (Pen pen = Stroke(color, s))
            using (var dot = new SolidBrush(color))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.DrawLine(pen, a, b);
                g.DrawLine(pen, b, c);
                float r = 2.1f * s;
                foreach (PointF p in new[] { a, b, c }) g.FillEllipse(dot, p.X - r, p.Y - r, r * 2, r * 2);
            }
        }

        // "content_copy": two overlapping rounded squares.
        public static void DrawCopy(Bitmap bmp, int px, Color color)
        {
            float s = px / 16f;
            using (Graphics g = Graphics.FromImage(bmp))
            using (Pen pen = Stroke(color, s))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath back = Ui.RoundedRect(new RectangleF(2.6f * s, 2.6f * s, 7f * s, 8f * s), 1.5f * s)) g.DrawPath(pen, back);
                using (GraphicsPath front = Ui.RoundedRect(new RectangleF(6.4f * s, 5.8f * s, 7f * s, 8f * s), 1.5f * s)) g.DrawPath(pen, front);
            }
        }

        // "language": a globe (circle, a meridian ellipse, the equator).
        public static void DrawGlobe(Bitmap bmp, int px, Color color)
        {
            float s = px / 16f;
            using (Graphics g = Graphics.FromImage(bmp))
            using (Pen pen = Stroke(color, s))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.DrawEllipse(pen, 2f * s, 2f * s, 12f * s, 12f * s);
                g.DrawEllipse(pen, 5.4f * s, 2f * s, 5.2f * s, 12f * s);
                g.DrawLine(pen, 2f * s, 8f * s, 14f * s, 8f * s);
            }
        }

        // "assistant": a speech bubble with a tail and three dots (the chat tab).
        public static void DrawAssistant(Bitmap bmp, int px, Color color)
        {
            float s = px / 16f;
            using (Graphics g = Graphics.FromImage(bmp))
            using (Pen pen = Stroke(color, s))
            using (var dot = new SolidBrush(color))
            using (var bubble = new GraphicsPath())
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                bubble.AddArc(2f * s, 2.2f * s, 3f * s, 3f * s, 180, 90);
                bubble.AddArc(11f * s, 2.2f * s, 3f * s, 3f * s, 270, 90);
                bubble.AddArc(11f * s, 8f * s, 3f * s, 3f * s, 0, 90);
                bubble.AddLine(9f * s, 11f * s, 5.2f * s, 13.8f * s);
                bubble.AddLine(5.2f * s, 13.8f * s, 5.6f * s, 11f * s);
                bubble.AddArc(2f * s, 8f * s, 3f * s, 3f * s, 90, 90);
                bubble.CloseFigure();
                g.DrawPath(pen, bubble);
                foreach (float x in new[] { 5.6f, 8f, 10.4f }) g.FillEllipse(dot, (x - 0.9f) * s, 6.2f * s, 1.8f * s, 1.8f * s);
            }
        }

        // "moments": a photo frame with a sun and two hills (the Khoảnh khắc tab).
        public static void DrawMoments(Bitmap bmp, int px, Color color)
        {
            float s = px / 16f;
            using (Graphics g = Graphics.FromImage(bmp))
            using (Pen pen = Stroke(color, s))
            using (var dot = new SolidBrush(color))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath frame = Ui.RoundedRect(new RectangleF(1.8f * s, 2.8f * s, 12.4f * s, 10.4f * s), 2f * s)) g.DrawPath(pen, frame);
                g.FillEllipse(dot, 9.4f * s, 5f * s, 2.4f * s, 2.4f * s);
                g.DrawLines(pen, new[] { new PointF(2.4f * s, 11.6f * s), new PointF(6f * s, 8f * s), new PointF(9f * s, 10.8f * s), new PointF(11f * s, 9.2f * s), new PointF(13.6f * s, 11.8f * s) });
            }
        }
    }
}
