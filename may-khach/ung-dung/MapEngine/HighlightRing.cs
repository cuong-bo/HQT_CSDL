using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Hue_travel.Theme;

namespace Hue_travel.MapEngine
{
    /// <summary>
    /// A soft pulse around the place the map was zoomed to: a faint glow with two rings that spread out from the marker and fade.
    /// It is a click-through window with per-pixel transparency owned by the main window, so it is smooth over the map.
    /// </summary>
    internal sealed class HighlightRing : Form
    {
        private const int WsExLayered = 0x80000, WsExTransparent = 0x20, WsExToolWindow = 0x80, WsExNoActivate = 0x08000000;
        private const int PeriodMs = 1700;
        private const int FrameMs = 33;

        private readonly Func<Point?> locate;
        private readonly Timer frame = new Timer { Interval = FrameMs };
        private readonly int markerRadius;
        private readonly DateTime started = DateTime.UtcNow;
        private Form ownerForm;

        /// <param name="diameter">Size of the whole pulse in device pixels.</param>
        /// <param name="markerDiameter">Size of the marker the pulse spreads out from.</param>
        /// <param name="locate">The screen position of the marker centre, or null while it is out of view.</param>
        public HighlightRing(int diameter, int markerDiameter, Func<Point?> locate)
        {
            this.locate = locate;
            markerRadius = markerDiameter / 2;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Size = new Size(diameter, diameter);
            frame.Tick += (s, e) => Step();
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

        /// <summary>Starts (or stops) following the place.</summary>
        public void Run(bool on, Form owner)
        {
            if (on)
            {
                ownerForm = owner;
                if (!frame.Enabled) frame.Start();
                Step();
            }
            else
            {
                frame.Stop();
                if (Visible) Hide();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) frame.Dispose();
            base.Dispose(disposing);
        }

        private void Step()
        {
            Point? center = locate();
            if (center == null || ownerForm == null || ownerForm.IsDisposed || !ownerForm.Visible || ownerForm.WindowState == FormWindowState.Minimized)
            {
                if (Visible) Hide();
                return;
            }
            if (!Visible) Show(ownerForm);
            double phase = ((DateTime.UtcNow - started).TotalMilliseconds % PeriodMs) / PeriodMs;
            using (Bitmap bmp = Render(phase))
            {
                Present(bmp, new Point(center.Value.X - Width / 2, center.Value.Y - Height / 2));
            }
        }

        private Bitmap Render(double phase)
        {
            var bmp = new Bitmap(Width, Height, PixelFormat.Format32bppPArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                float c = Width / 2f, max = c - 1f, start = markerRadius + 1f;
                Color blue = ThemeColors.Primary;

                // a soft glow under the marker, breathing a little
                float breath = (float)(0.5 + 0.5 * Math.Sin(phase * 2 * Math.PI));
                float glow = start + (max - start) * (0.30f + 0.10f * breath);
                using (var path = new GraphicsPath())
                {
                    path.AddEllipse(c - glow, c - glow, glow * 2, glow * 2);
                    using (var brush = new PathGradientBrush(path))
                    {
                        brush.CenterColor = Color.FromArgb(120, blue);
                        brush.SurroundColors = new[] { Color.FromArgb(0, blue) };
                        g.FillPath(brush, path);
                    }
                }

                // two rings spreading outwards, the second half a period behind
                for (int i = 0; i < 2; i++)
                {
                    double t = (phase + i * 0.5) % 1.0;
                    float radius = start + (max - start) * (float)(1 - Math.Pow(1 - t, 2));
                    int alpha = (int)(255 * Math.Pow(1 - t, 1.4));
                    float thickness = Math.Max(1.5f, markerRadius * 0.24f) * (float)(1 - 0.6 * t);
                    using (var pen = new Pen(Color.FromArgb(alpha, blue), thickness))
                    {
                        g.DrawEllipse(pen, c - radius, c - radius, radius * 2, radius * 2);
                    }
                }
            }
            return bmp;
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

        [StructLayout(LayoutKind.Sequential)] private struct POINT { public int x, y; }
        [StructLayout(LayoutKind.Sequential)] private struct SIZE { public int cx, cy; }
        [StructLayout(LayoutKind.Sequential)] private struct BLENDFUNCTION { public byte BlendOp, BlendFlags, SourceConstantAlpha, AlphaFormat; }

        [DllImport("user32.dll")] private static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst, ref SIZE psize, IntPtr hdcSrc, ref POINT pptSrc, int crKey, ref BLENDFUNCTION pblend, int dwFlags);
        [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr hwnd);
        [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);
        [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr hdc);
        [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr hdc);
        [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);
        [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr obj);
    }
}
