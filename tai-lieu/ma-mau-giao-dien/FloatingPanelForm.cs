using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Hue_travel.Theme
{
    /// <summary>
    /// Base for the floating panels: a borderless Form, rounded (<see cref="ThemeMetrics.RadiusLg"/>), with a soft shadow, that rises
    /// and fades in when it opens. Each panel is a top-level window owned by the main window (child controls cannot be shadowed over
    /// the MapWinGIS ActiveX map). The shadow and the smooth rounded edge are painted by the <see cref="ChromeLayer"/> UNDER the
    /// panel: the form itself is clipped to a rounded Region one pixel inside the card, so the jagged edge of the Region is never
    /// seen. <see cref="AttachTo"/> / <see cref="AttachToSide"/> record which edges of the host the panel follows.
    /// </summary>
    public class FloatingPanelForm : Form
    {
        /// <summary>
        /// True: the shadow and the corners come from the ChromeLayer, on every Windows version. False: the old behaviour (Windows 11
        /// rounds the window itself, 8 px, with a system shadow). Set it to false in Program.Main to roll the new look back.
        /// </summary>
        public static bool SoftChrome = true;

        private const int MinStretchedHeight = 360;   // a side panel never gets shorter than this (design pixels)
        private const int SlideDistance = 16;         // design pixels a panel rises while it appears
        private const double AnimationStep = 0.12;    // about 8 frames at 15 ms
        private const int DwmNcRenderingPolicy = 2;
        private const int DwmWindowCornerPreference = 33;
        private const int DwmDoNotRound = 1;
        private const int DwmRound = 2;

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
        private bool isScaled;
        private bool stretchesVertically;
        private double animation = 1;                 // 0 just opened .. 1 settled
        private ChromeCard card;

        public FloatingPanelForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = ThemeColors.Paper;
            SetStyle(ControlStyles.ResizeRedraw, true);
            fadeTimer.Tick += (s, e) => StepAnimation();
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
            ScaleToScreen();
            RegisterCard();
        }

        /// <summary>
        /// Pins the panel to the left side of the host like a web map's side panel: <paramref name="left"/> and
        /// <paramref name="top"/> from the host's client corner, <paramref name="bottom"/> above its bottom edge, all in
        /// design pixels. The panel is as tall as that leaves, so it follows the window when it is resized.
        /// </summary>
        public void AttachToSide(Form hostForm, int left, int top, int bottom)
        {
            host = hostForm;
            anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            leftOffset = left;
            topOffset = top;
            bottomOffset = bottom;
            stretchesVertically = true;
            host.LocationChanged += (s, e) => Reposition();
            host.SizeChanged += (s, e) => Reposition();
            ScaleToScreen();
            RegisterCard();
        }

        // The panel's shadow and smooth edge are drawn by the host's ChromeLayer.
        private void RegisterCard()
        {
            if (!SoftChrome || card != null || host == null || DesignMode) return;
            card = new ChromeCard
            {
                Radius = ThemeMetrics.RadiusLg,
                Level = Elevation.Panel,
                Fill = ThemeColors.Paper,
                Ring = ThemeColors.Line,   // the 1 px outer ring the Region leaves uncovered becomes the panel's border
                Opacity = () => (float)Opacity,
                ScreenBounds = () => Visible && !IsDisposed ? Bounds : Rectangle.Empty,
            };
            ChromeLayer layer = ChromeLayer.For(host);
            layer.Add(card);
            LocationChanged += (s, e) => layer.RequestRender();
            SizeChanged += (s, e) => layer.RequestRender();
            VisibleChanged += (s, e) => layer.RequestRender();
        }

        /// <summary>Panels are laid out in 96-DPI design pixels; grow the form and its children once to the screen DPI.</summary>
        private void ScaleToScreen()
        {
            if (isScaled || Ui.Scale <= 1.001f) return;
            isScaled = true;
            Scale(new SizeF(Ui.Scale, Ui.Scale));
        }

        /// <summary>Call after the panel was moved or resized by the user so it keeps that spot when the host moves.</summary>
        public void RememberPosition()
        {
            if (host == null || !host.IsHandleCreated) return;
            Point origin = host.PointToScreen(Point.Empty);
            Size client = host.ClientSize;
            int x = Left - origin.X, y = Top - origin.Y;
            // Offsets are kept in design pixels so they survive a DPI change of the layout.
            leftOffset = Design(x);
            topOffset = Design(y);
            rightOffset = Design(client.Width - (x + Width));
            bottomOffset = Design(client.Height - (y + Height));
            centerXOffset = Design(x + Width / 2 - client.Width / 2);
            centerYOffset = Design(y + Height / 2 - client.Height / 2);
        }

        private static int Design(int devicePixels) => (int)Math.Round(devicePixels / Ui.Scale);

        private void Reposition()
        {
            if (host == null || !host.IsHandleCreated || host.WindowState == FormWindowState.Minimized) return;
            Point origin = host.PointToScreen(Point.Empty);
            Size client = host.ClientSize;
            bool left = (anchor & AnchorStyles.Left) != 0, right = (anchor & AnchorStyles.Right) != 0;
            bool top = (anchor & AnchorStyles.Top) != 0, bottom = (anchor & AnchorStyles.Bottom) != 0;

            if (stretchesVertically && !isCollapsed)
            {
                int stretched = client.Height - Ui.S(topOffset) - Ui.S(bottomOffset);
                if (stretched >= Ui.S(MinStretchedHeight) && stretched != Height) Height = stretched;
            }

            int x = left ? origin.X + Ui.S(leftOffset)
                  : right ? origin.X + client.Width - Ui.S(rightOffset) - Width
                  : origin.X + client.Width / 2 + Ui.S(centerXOffset) - Width / 2;
            int y = top ? origin.Y + Ui.S(topOffset)
                  : bottom ? origin.Y + client.Height - Ui.S(bottomOffset) - Height
                  : origin.Y + client.Height / 2 + Ui.S(centerYOffset) - Height / 2;
            y += (int)Math.Round(Ui.S(SlideDistance) * (1 - Ease(animation)));   // still rising while it opens
            Location = new Point(x, y);
        }

        private static double Ease(double t) => 1 - Math.Pow(1 - Math.Max(0, Math.Min(1, t)), 3);   // fast at first, slow at the end

        protected override void SetVisibleCore(bool value)
        {
            if (value && host != null && !DesignMode)
            {
                if (Owner == null) Owner = host;
                // People who switched Windows animations off (SystemInformation.UIEffectsEnabled) get the panel at once.
                animation = SystemInformation.UIEffectsEnabled ? 0 : 1;
                Opacity = Ease(animation);
                Reposition();
                if (animation < 1) fadeTimer.Start();
            }
            base.SetVisibleCore(value);
        }

        private void StepAnimation()
        {
            animation = Math.Min(1, animation + AnimationStep);
            Opacity = Ease(animation);
            Reposition();
            if (animation >= 1) fadeTimer.Stop();
            if (card != null && host != null) ChromeLayer.For(host).RequestRender();   // the shadow fades with the panel
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
                if (SoftChrome)
                {
                    // The ChromeLayer draws the shadow and the edge, so Windows must not round or shadow the window as well.
                    int none = DwmDoNotRound;
                    DwmSetWindowAttribute(Handle, DwmWindowCornerPreference, ref none, sizeof(int));   // rejected on Windows 10: harmless
                }
                else
                {
                    // Windows draws the soft shadow of a borderless window once its frame is extended by 1px.
                    int policy = DwmNcRenderingPolicy;
                    DwmSetWindowAttribute(Handle, DwmNcRenderingPolicy, ref policy, sizeof(int));
                    var margins = new Margins { Left = 1, Right = 1, Top = 1, Bottom = 1 };
                    DwmExtendFrameIntoClientArea(Handle, ref margins);

                    // Windows 11 rounds the window itself (anti-aliased, fixed ~8 px); older versions reject the attribute.
                    int round = DwmRound;
                    hasSystemCorners = DwmSetWindowAttribute(Handle, DwmWindowCornerPreference, ref round, sizeof(int)) == 0;
                }
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

        // SoftChrome: clip to a rounded rectangle ONE pixel inside the card (the card underneath is smooth, so the jagged Region edge
        // is hidden against it). Otherwise: no Region on Windows 11, a plain rounded Region elsewhere (no shadow then).
        private void UpdateRegion()
        {
            if (Width <= 0 || Height <= 0) return;
            if (SoftChrome)
            {
                float radius = Math.Max(1, Ui.S(ThemeMetrics.RadiusLg) - 1);
                using (GraphicsPath path = Ui.RoundedRect(new RectangleF(1, 1, Width - 2, Height - 2), radius))
                {
                    Region = new Region(path);
                }
                return;
            }
            if (hasSystemCorners)
            {
                if (Region != null) Region = null;
                return;
            }
            using (GraphicsPath path = Ui.RoundedRect(new RectangleF(0, 0, Width, Height), Ui.S(8)))
            {
                Region = new Region(path);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                fadeTimer.Dispose();
                if (card != null && host != null && !host.IsDisposed) ChromeLayer.For(host).Remove(card);
            }
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (SoftChrome) return;   // the border is the ChromeLayer's ring
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = Ui.RoundedRect(new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f), Ui.S(8)))
            using (var pen = new Pen(ThemeColors.Slate200, 1f))
            {
                e.Graphics.DrawPath(pen, path);
            }
        }
    }
}
