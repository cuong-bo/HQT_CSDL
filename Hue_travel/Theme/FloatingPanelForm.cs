using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Hue_travel.Theme
{
    /// <summary>
    /// Base for the floating panels (LayerControl, RouteOptimizer, ...): a borderless white Form
    /// with a 1px slate-200 border and rounded corners. Each panel is a top-level window owned by
    /// the main window (child controls cannot be shadowed over the MapWinGIS ActiveX map), so the
    /// system can draw a soft shadow and anti-aliased rounded corners (Windows 11) and the panel
    /// can fade in. <see cref="AttachTo"/> records which edges of the host it follows.
    /// </summary>
    public class FloatingPanelForm : Form
    {
        private const int CornerRadius = 8;
        private const double FadeStep = 0.2;
        private const int DwmNcRenderingPolicy = 2;
        private const int DwmWindowCornerPreference = 33;
        private const int DwmRoundCorners = 2;

        [StructLayout(LayoutKind.Sequential)]
        private struct Margins
        {
            public int Left, Right, Top, Bottom;
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref Margins margins);

        public event EventHandler CloseRequested;

        /// <summary>Raised when the panel wants the host window to show a toast message.</summary>
        public event EventHandler<ToastMessage> MessageRaised;

        private readonly Timer fadeTimer = new Timer { Interval = 15 };
        private Form host;
        private AnchorStyles anchor = AnchorStyles.Top | AnchorStyles.Left;
        private int leftOffset, rightOffset, topOffset, bottomOffset, centerXOffset, centerYOffset;
        private bool hasSystemCorners;
        private int expandedHeight;
        private bool isCollapsed;

        public FloatingPanelForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = ThemeColors.SurfaceContainerLowest;
            SetStyle(ControlStyles.ResizeRedraw, true);
            fadeTimer.Tick += (s, e) => StepFade();
        }

        /// <summary>Opening a panel must not steal focus from the map.</summary>
        protected override bool ShowWithoutActivation => true;

        /// <summary>
        /// Ties the panel to a host window: <paramref name="clientLocation"/> is where it sits in the host's
        /// client area and <paramref name="anchorStyles"/> says which edges it keeps its distance from
        /// (no horizontal edge = stays centred horizontally, no vertical edge = centred vertically).
        /// </summary>
        public void AttachTo(Form hostForm, Point clientLocation, AnchorStyles anchorStyles)
        {
            host = hostForm;
            anchor = anchorStyles;
            Size client = hostForm.ClientSize;
            leftOffset = clientLocation.X;
            topOffset = clientLocation.Y;
            rightOffset = client.Width - (clientLocation.X + Width);
            bottomOffset = client.Height - (clientLocation.Y + Height);
            centerXOffset = clientLocation.X + Width / 2 - client.Width / 2;
            centerYOffset = clientLocation.Y + Height / 2 - client.Height / 2;
            host.LocationChanged += (s, e) => Reposition();
            host.SizeChanged += (s, e) => Reposition();
        }

        /// <summary>Call after the panel was moved or resized by the user so it keeps that spot when the host moves.</summary>
        public void RememberPosition()
        {
            if (host == null || !host.IsHandleCreated) return;
            Point origin = host.PointToScreen(Point.Empty);
            Size client = host.ClientSize;
            int x = Left - origin.X, y = Top - origin.Y;
            leftOffset = x;
            topOffset = y;
            rightOffset = client.Width - (x + Width);
            bottomOffset = client.Height - (y + Height);
            centerXOffset = x + Width / 2 - client.Width / 2;
            centerYOffset = y + Height / 2 - client.Height / 2;
        }

        private void Reposition()
        {
            if (host == null || !host.IsHandleCreated || host.WindowState == FormWindowState.Minimized) return;
            Point origin = host.PointToScreen(Point.Empty);
            Size client = host.ClientSize;
            bool left = (anchor & AnchorStyles.Left) != 0, right = (anchor & AnchorStyles.Right) != 0;
            bool top = (anchor & AnchorStyles.Top) != 0, bottom = (anchor & AnchorStyles.Bottom) != 0;

            int x = left ? origin.X + leftOffset
                  : right ? origin.X + client.Width - rightOffset - Width
                  : origin.X + client.Width / 2 + centerXOffset - Width / 2;
            int y = top ? origin.Y + topOffset
                  : bottom ? origin.Y + client.Height - bottomOffset - Height
                  : origin.Y + client.Height / 2 + centerYOffset - Height / 2;
            Location = new Point(x, y);
        }

        protected override void SetVisibleCore(bool value)
        {
            if (value && host != null && !DesignMode)
            {
                if (Owner == null) Owner = host;
                Reposition();
                Opacity = 0;
                fadeTimer.Start();
            }
            base.SetVisibleCore(value);
        }

        private void StepFade()
        {
            double next = Opacity + FadeStep;
            if (next >= 1)
            {
                Opacity = 1;
                fadeTimer.Stop();
            }
            else
            {
                Opacity = next;
            }
        }

        /// <summary>Collapses the panel to its header strip, or restores it. Bottom-anchored panels keep their bottom edge.</summary>
        public void ToggleCollapsed(int collapsedHeight)
        {
            bool keepBottom = (anchor & AnchorStyles.Bottom) != 0 && (anchor & AnchorStyles.Top) == 0;
            int bottom = Bottom;
            if (!isCollapsed)
            {
                expandedHeight = Height;
                Height = collapsedHeight;
            }
            else
            {
                Height = expandedHeight;
            }
            isCollapsed = !isCollapsed;

            // Only the header strip stays; docked body/footer would otherwise overlap it.
            foreach (Control child in Controls)
            {
                if (!(child is PanelHeader)) child.Visible = !isCollapsed;
            }
            if (keepBottom) Top = bottom - Height;
            RememberPosition();
        }

        protected void Notify(string text, ToastKind kind = ToastKind.Info) => MessageRaised?.Invoke(this, new ToastMessage(text, kind));

        protected void RaiseCloseRequested() => CloseRequested?.Invoke(this, EventArgs.Empty);

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (DesignMode) return;
            try
            {
                // Windows draws the soft shadow of a borderless window once its frame is extended by 1px.
                int policy = DwmNcRenderingPolicy;
                DwmSetWindowAttribute(Handle, DwmNcRenderingPolicy, ref policy, sizeof(int));
                var margins = new Margins { Left = 1, Right = 1, Top = 1, Bottom = 1 };
                DwmExtendFrameIntoClientArea(Handle, ref margins);

                // Windows 11 rounds the window itself (anti-aliased); older versions reject the attribute.
                int round = DwmRoundCorners;
                hasSystemCorners = DwmSetWindowAttribute(Handle, DwmWindowCornerPreference, ref round, sizeof(int)) == 0;
            }
            catch (DllNotFoundException) { }
            catch (EntryPointNotFoundException) { }
            UpdateRegion();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateRegion();
        }

        // Without system rounding, clip the window to a rounded rectangle instead (no shadow in that case).
        private void UpdateRegion()
        {
            if (hasSystemCorners)
            {
                if (Region != null) Region = null;
                return;
            }
            if (Width <= 0 || Height <= 0) return;
            using (GraphicsPath path = Ui.RoundedRect(new RectangleF(0, 0, Width, Height), CornerRadius))
            {
                Region = new Region(path);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) fadeTimer.Dispose();
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = Ui.RoundedRect(new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f), CornerRadius))
            using (var pen = new Pen(ThemeColors.Slate200, 1f))
            {
                e.Graphics.DrawPath(pen, path);
            }
        }
    }
}
