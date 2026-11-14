using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Hue_travel.Theme
{
    /// <summary>How high a card floats: the bigger the level, the longer and softer its shadow.</summary>
    public enum Elevation { Chip, Panel, Dialog }

    /// <summary>One floating card the <see cref="ChromeLayer"/> draws a shadow for.</summary>
    public sealed class ChromeCard
    {
        /// <summary>The card's rectangle in SCREEN pixels; Rectangle.Empty while the card is hidden.</summary>
        public Func<Rectangle> ScreenBounds = () => Rectangle.Empty;

        /// <summary>Corner radius in design pixels (use a ThemeMetrics.Radius* value).</summary>
        public int Radius = ThemeMetrics.RadiusLg;

        public Elevation Level = Elevation.Panel;

        /// <summary>0..1, so the shadow fades in and out with the card.</summary>
        public Func<float> Opacity = () => 1f;

        /// <summary>
        /// Panel forms sit ABOVE the layer: give their background colour and the layer paints an anti-aliased card of it, which
        /// hides the jagged edge of the form's Region. Controls on the map sit BELOW the layer: leave this empty and a hole is
        /// punched where the card is, so the shadow never darkens the control.
        /// </summary>
        public Color Fill = Color.Empty;

        /// <summary>A 1 px outline painted over the card's edge (covers the jagged Region edge of a control); Empty for none.</summary>
        public Color Ring = Color.Empty;
    }

    /// <summary>
    /// One click-through window with per-pixel transparency laid over the whole client area of the main window. It paints the
    /// soft shadow (and the smooth rounded edge) of every registered card, which neither the ActiveX map nor a WinForms control can.
    /// Same technique as <c>HighlightRing</c>: WS_EX_LAYERED | WS_EX_TRANSPARENT, updated with UpdateLayeredWindow.
    /// Create it with <see cref="For"/> BEFORE any panel is shown, so panels (shown later) stay above it.
    /// </summary>
    public sealed class ChromeLayer : Form
    {
        private const int WsExLayered = 0x80000, WsExTransparent = 0x20, WsExToolWindow = 0x80, WsExNoActivate = 0x08000000;
        private static readonly IntPtr HwndBottom = new IntPtr(1);
        private const uint SwpNoSize = 0x1, SwpNoMove = 0x2, SwpNoActivate = 0x10;

        private static readonly Dictionary<Form, ChromeLayer> Layers = new Dictionary<Form, ChromeLayer>();

        private readonly Form host;
        private readonly List<ChromeCard> cards = new List<ChromeCard>();
        private readonly Timer coalesce = new Timer { Interval = 16 };
        private readonly Dictionary<string, Bitmap> shadows = new Dictionary<string, Bitmap>();
        private Bitmap surface;
        private bool dirty;

        /// <summary>The layer of a main window (created on first use).</summary>
        public static ChromeLayer For(Form host)
        {
            ChromeLayer layer;
            if (Layers.TryGetValue(host, out layer)) return layer;
            layer = new ChromeLayer(host);
            Layers[host] = layer;
            return layer;
        }

        private ChromeLayer(Form host)
        {
            this.host = host;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Size = new Size(1, 1);
            coalesce.Tick += (s, e) => { coalesce.Stop(); if (dirty) Render(); };
            host.LocationChanged += (s, e) => RequestRender();
            host.SizeChanged += (s, e) => RequestRender();
            host.VisibleChanged += (s, e) => RequestRender();
            host.FormClosed += (s, e) => { Layers.Remove(host); Close(); };
        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WsExLayered | WsExTransparent | WsExToolWindow | WsExNoActivate;
                return cp;
            }
        }

        public void Add(ChromeCard card)
        {
            cards.Add(card);
            RequestRender();
        }

        public void Remove(ChromeCard card)
        {
            cards.Remove(card);
            RequestRender();
        }

        /// <summary>Call whenever a card moved, resized, showed, hid or changed opacity. Calls within 16 ms are merged into one repaint.</summary>
        public void RequestRender()
        {
            dirty = true;
            if (!coalesce.Enabled) coalesce.Start();
        }

        // ------------------------------------------------------------------ painting

        private void Render()
        {
            dirty = false;
            if (host.IsDisposed || !host.IsHandleCreated) return;
            if (!host.Visible) { if (Visible) Hide(); return; }
            if (host.WindowState == FormWindowState.Minimized) return;   // Windows hides the owned window together with its owner

            Size size = host.ClientSize;
            if (size.Width <= 0 || size.Height <= 0) return;
            Point origin = host.PointToScreen(Point.Empty);

            if (!Visible)
            {
                Show(host);
                // A layer shown late must not cover panels that were shown before: sink it to the bottom of the owned windows.
                SetWindowPos(Handle, HwndBottom, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpNoActivate);
            }

            if (surface == null || surface.Width != size.Width || surface.Height != size.Height)
            {
                if (surface != null) surface.Dispose();
                surface = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppPArgb);
            }

            using (Graphics g = Graphics.FromImage(surface))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                foreach (ChromeCard card in cards)
                {
                    Rectangle r = card.ScreenBounds();
                    if (r.IsEmpty) continue;
                    float opacity = Math.Max(0f, Math.Min(1f, card.Opacity()));
                    if (opacity < 0.01f) continue;
                    r.Offset(-origin.X, -origin.Y);
                    DrawCard(g, card, r, opacity);
                }
            }
            Present(surface, origin);
        }

        private void DrawCard(Graphics g, ChromeCard card, Rectangle r, float opacity)
        {
            int radius = Ui.S(card.Radius);
            int blur, offsetY, alpha;
            Metrics(card.Level, out blur, out offsetY, out alpha);
            int margin = blur * 2;

            Bitmap shadow = ShadowFor(r.Size, radius, card.Level, blur, alpha);
            var target = new Rectangle(r.X - margin, r.Y - margin + offsetY, shadow.Width, shadow.Height);
            using (var attributes = new ImageAttributes())
            {
                var matrix = new ColorMatrix { Matrix33 = opacity };
                attributes.SetColorMatrix(matrix);
                g.DrawImage(shadow, target, 0, 0, shadow.Width, shadow.Height, GraphicsUnit.Pixel, attributes);
            }

            var bounds = new RectangleF(r.X, r.Y, r.Width, r.Height);
            using (GraphicsPath path = Ui.RoundedRect(bounds, radius))
            {
                if (card.Fill.IsEmpty)
                {
                    // Punch a hole: the control below shows through, unshadowed.
                    CompositingMode old = g.CompositingMode;
                    g.CompositingMode = CompositingMode.SourceCopy;
                    using (var clear = new SolidBrush(Color.Transparent)) g.FillPath(clear, path);
                    g.CompositingMode = old;
                }
                else
                {
                    using (var fill = new SolidBrush(Color.FromArgb((int)(255 * opacity), card.Fill))) g.FillPath(fill, path);
                }
                if (!card.Ring.IsEmpty)
                {
                    using (var pen = new Pen(Color.FromArgb((int)(card.Ring.A * opacity), card.Ring), Math.Max(1f, Ui.S(1f))))
                    using (GraphicsPath edge = Ui.RoundedRect(new RectangleF(r.X + 0.5f, r.Y + 0.5f, r.Width - 1f, r.Height - 1f), radius))
                    {
                        g.DrawPath(pen, edge);
                    }
                }
            }
        }

        // Blur radius, vertical offset and peak opacity (0..255) of each level, in device pixels.
        private static void Metrics(Elevation level, out int blur, out int offsetY, out int alpha)
        {
            switch (level)
            {
                case Elevation.Chip: blur = Ui.S(8); offsetY = Ui.S(2); alpha = 70; break;
                case Elevation.Dialog: blur = Ui.S(40); offsetY = Ui.S(12); alpha = 90; break;
                default: blur = Ui.S(24); offsetY = Ui.S(8); alpha = 80; break;
            }
        }

        private Bitmap ShadowFor(Size card, int radius, Elevation level, int blur, int alpha)
        {
            string key = card.Width + "x" + card.Height + "|" + radius + "|" + level;
            Bitmap cached;
            if (shadows.TryGetValue(key, out cached)) return cached;
            if (shadows.Count > 48)
            {
                foreach (Bitmap old in shadows.Values) old.Dispose();
                shadows.Clear();
            }
            Bitmap created = BuildShadow(card, radius, blur, alpha);
            shadows[key] = created;
            return created;
        }

        /// <summary>The shadow of a rounded card as a premultiplied bitmap with a margin of 2 x blur: a rounded mask blurred three times.</summary>
        internal static Bitmap BuildShadow(Size card, int radius, int blur, int alpha)
        {
            int margin = blur * 2;
            int w = card.Width + margin * 2, h = card.Height + margin * 2;
            var mask = new float[w * h];

            using (var shape = new Bitmap(w, h, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(shape))
                using (GraphicsPath path = Ui.RoundedRect(new RectangleF(margin, margin, card.Width, card.Height), radius))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.FillPath(Brushes.White, path);
                }
                BitmapData data = shape.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                try
                {
                    var row = new byte[data.Stride];
                    for (int y = 0; y < h; y++)
                    {
                        Marshal.Copy(data.Scan0 + y * data.Stride, row, 0, data.Stride);
                        for (int x = 0; x < w; x++) mask[y * w + x] = row[x * 4 + 3] / 255f;
                    }
                }
                finally { shape.UnlockBits(data); }
            }

            // Three box blurs of radius blur/3 approximate a Gaussian of sigma about blur/2.
            int r = Math.Max(1, blur / 3);
            var scratch = new float[w * h];
            for (int pass = 0; pass < 3; pass++)
            {
                BoxBlur(mask, scratch, w, h, r, true);
                BoxBlur(scratch, mask, w, h, r, false);
            }

            var result = new Bitmap(w, h, PixelFormat.Format32bppPArgb);
            Color ink = ThemeColors.Ink;
            BitmapData outData = result.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);
            try
            {
                var row = new byte[outData.Stride];
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        int a = (int)Math.Round(Math.Max(0f, Math.Min(1f, mask[y * w + x])) * alpha);
                        row[x * 4 + 0] = (byte)(ink.B * a / 255);   // premultiplied B, G, R, A
                        row[x * 4 + 1] = (byte)(ink.G * a / 255);
                        row[x * 4 + 2] = (byte)(ink.R * a / 255);
                        row[x * 4 + 3] = (byte)a;
                    }
                    Marshal.Copy(row, 0, outData.Scan0 + y * outData.Stride, outData.Stride);
                }
            }
            finally { result.UnlockBits(outData); }
            return result;
        }

        // One box blur pass along rows (horizontal) or columns; pixels outside the image count as 0.
        private static void BoxBlur(float[] src, float[] dst, int w, int h, int r, bool horizontal)
        {
            int length = horizontal ? w : h, lines = horizontal ? h : w;
            float norm = 1f / (2 * r + 1);
            for (int line = 0; line < lines; line++)
            {
                int stride = horizontal ? 1 : w;
                int start = horizontal ? line * w : line;
                float sum = 0;
                for (int i = 0; i <= r && i < length; i++) sum += src[start + i * stride];
                for (int i = 0; i < length; i++)
                {
                    dst[start + i * stride] = sum * norm;
                    int add = i + r + 1, remove = i - r;
                    if (add < length) sum += src[start + add * stride];
                    if (remove >= 0) sum -= src[start + remove * stride];
                }
            }
        }

        private void Present(Bitmap bmp, Point topLeft)
        {
            IntPtr screenDc = GetDC(IntPtr.Zero), memDc = CreateCompatibleDC(screenDc), hBitmap = IntPtr.Zero, old = IntPtr.Zero;
            try
            {
                hBitmap = bmp.GetHbitmap(Color.FromArgb(0));
                old = SelectObject(memDc, hBitmap);
                var size = new SIZE { cx = bmp.Width, cy = bmp.Height };
                var source = new POINT();
                var position = new POINT { x = topLeft.X, y = topLeft.Y };
                var blend = new BLENDFUNCTION { BlendOp = 0, BlendFlags = 0, SourceConstantAlpha = 255, AlphaFormat = 1 };
                UpdateLayeredWindow(Handle, screenDc, ref position, ref size, memDc, ref source, 0, ref blend, 2);
            }
            finally
            {
                if (hBitmap != IntPtr.Zero) { SelectObject(memDc, old); DeleteObject(hBitmap); }
                DeleteDC(memDc);
                ReleaseDC(IntPtr.Zero, screenDc);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                coalesce.Dispose();
                if (surface != null) surface.Dispose();
                foreach (Bitmap b in shadows.Values) b.Dispose();
                shadows.Clear();
            }
            base.Dispose(disposing);
        }

        [StructLayout(LayoutKind.Sequential)] private struct POINT { public int x, y; }
        [StructLayout(LayoutKind.Sequential)] private struct SIZE { public int cx, cy; }
        [StructLayout(LayoutKind.Sequential)] private struct BLENDFUNCTION { public byte BlendOp, BlendFlags, SourceConstantAlpha, AlphaFormat; }

        [DllImport("user32.dll")] private static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst, ref SIZE psize, IntPtr hdcSrc, ref POINT pptSrc, int crKey, ref BLENDFUNCTION pblend, int dwFlags);
        [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);
        [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr hwnd);
        [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);
        [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr hdc);
        [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr hdc);
        [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);
        [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr obj);
    }
}
