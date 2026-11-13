using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Hue_travel.Theme;

namespace Hue_travel.Shell
{
    /// <summary>
    /// The slim title strip of the main window (40 px): the brand mark and name on a light sky-blue strip, the account and
    /// weather chips, and the window buttons. The search box floats on the map (<see cref="SearchPill"/>).
    /// Written in code, without a designer file. Same events as before for the window buttons.
    /// </summary>
    public sealed class TitleBar : Control
    {
        public const int DesignHeight = 40;

        private const int WmNcLButtonDown = 0xA1;
        private const int HtCaption = 0x2;

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        public event EventHandler MinimizeClicked;
        public event EventHandler MaximizeClicked;
        public event EventHandler CloseClicked;

        private readonly IconButton btnMinimize = new IconButton();
        private readonly IconButton btnMaximize = new IconButton();
        private readonly IconButton btnClose = new IconButton();
        private string weather = "";
        private string user = "";

        public TitleBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Dock = DockStyle.Top;
            Height = DesignHeight;

            StyleWindowButton(btnMinimize, "minimize", false);
            StyleWindowButton(btnMaximize, "crop_square", false);
            StyleWindowButton(btnClose, "close", true);
            btnMinimize.Click += (s, e) => MinimizeClicked?.Invoke(this, EventArgs.Empty);
            btnMaximize.Click += (s, e) => MaximizeClicked?.Invoke(this, EventArgs.Empty);
            btnClose.Click += (s, e) => CloseClicked?.Invoke(this, EventArgs.Empty);

            Controls.Add(btnMinimize);
            Controls.Add(btnMaximize);
            Controls.Add(btnClose);
            LayoutChildren();
        }

        /// <summary>Shows the "restore" icon while the window is maximised, "maximise" otherwise.</summary>
        public void SetMaximized(bool isMaximized) => btnMaximize.IconName = isMaximized ? "filter_none" : "crop_square";

        /// <summary>Who is using the app: "Khách", the signed-in person's name, or empty (offline) to show nothing.</summary>
        public void SetUser(string text)
        {
            user = text ?? "";
            Invalidate();
        }

        /// <summary>The weather chip on the right ("27°C · Trời nắng"); empty text hides it.</summary>
        public void SetWeather(string text)
        {
            weather = text ?? "";
            Invalidate();
        }

        private static void StyleWindowButton(IconButton button, string icon, bool isClose)
        {
            button.AutoWidth = false;
            button.TabStop = false;
            button.IconName = icon;
            button.IconSize = 16;
            button.Radius = 0;
            button.IconColor = ThemeColors.Slate600;
            button.FillColor = Color.Transparent;
            button.HoverFillColor = isClose ? ThemeColors.Lacquer : ThemeColors.Slate100;
            if (isClose) button.HoverIconColor = Color.White;
        }

        private void LayoutChildren()
        {
            int w = Ui.S(46);
            btnClose.SetBounds(Width - w, 0, w, Height);
            btnMaximize.SetBounds(btnClose.Left - w, 0, w, Height);
            btnMinimize.SetBounds(btnMaximize.Left - w, 0, w, Height);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutChildren();
        }

        // Control.Scale grows children after OnResize ran, so lay out again once scaling is done.
        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            LayoutChildren();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var brush = new SolidBrush(ThemeColors.SurfaceContainerLowest)) g.FillRectangle(brush, ClientRectangle);
            using (var line = new SolidBrush(ThemeColors.Slate200)) g.FillRectangle(line, 0, Height - 1, Width, 1);

            int icon = Ui.S(22);
            int x = Ui.S(16);
            g.DrawImage(IconFont.Get("place", icon, ThemeColors.Primary), x, (Height - icon) / 2, icon, icon);
            x += icon + Ui.S(8);
            TextRenderer.DrawText(g, "Huế Travel", ThemeFonts.Serif(17), new Rectangle(x, 0, Ui.S(180), Height), ThemeColors.Slate900,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);

            if (weather.Length == 0 && user.Length == 0) return;
            int chipRight = btnMinimize.Left - Ui.S(16);
            if (weather.Length > 0) chipRight = DrawChip(g, weather, chipRight);
            if (user.Length > 0) DrawChip(g, user, chipRight - Ui.S(6));
        }

        // A light sky pill with slate text, right-aligned at <paramref name="right"/>; returns its left edge.
        private int DrawChip(Graphics g, string text, int right)
        {
            Font font = ThemeFonts.Medium(12);
            Size size = TextRenderer.MeasureText(text, font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
            var chip = new Rectangle(right - size.Width - Ui.S(28), (Height - Ui.S(26)) / 2, size.Width + Ui.S(28), Ui.S(26));
            using (GraphicsPath path = Ui.RoundedRect(chip, chip.Height / 2f))
            using (var fill = new SolidBrush(ThemeColors.Sky50))
            using (var pen = new Pen(ThemeColors.Sky200))
            {
                g.FillPath(fill, path);
                g.DrawPath(pen, path);
            }
            TextRenderer.DrawText(g, text, font, chip, ThemeColors.Slate700,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
            return chip.Left;
        }

        // FormBorderStyle.None has no caption: dragging the bar moves the window, double-clicking maximises / restores.
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Form form = FindForm();
            if (e.Button != MouseButtons.Left || form == null || form.WindowState == FormWindowState.Maximized) return;
            ReleaseCapture();
            SendMessage(form.Handle, WmNcLButtonDown, HtCaption, 0);
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            if (e.Button == MouseButtons.Left) MaximizeClicked?.Invoke(this, EventArgs.Empty);
        }
    }
}
