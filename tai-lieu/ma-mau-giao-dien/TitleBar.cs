using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Hue_travel.Theme;

namespace Hue_travel.Shell
{
    /// <summary>
    /// The slim title strip of the main window (40 px): the brand mark and name on a purple gradient, a greeting with the weather,
    /// and the window buttons. The search box is no longer here: it floats on the map (<see cref="SearchPill"/>).
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

        private readonly PictureBox picLogo = new PictureBox();
        private readonly IconButton btnMinimize = new IconButton();
        private readonly IconButton btnMaximize = new IconButton();
        private readonly IconButton btnClose = new IconButton();
        private string weather = "";

        public TitleBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Dock = DockStyle.Top;
            Height = DesignHeight;

            picLogo.Image = AppImages.BrandMark(Ui.S(30));
            picLogo.SizeMode = PictureBoxSizeMode.Zoom;
            picLogo.BackColor = Color.Transparent;
            picLogo.Enabled = false;   // clicks fall through to the bar, which drags the window

            StyleWindowButton(btnMinimize, "minimize", false);
            StyleWindowButton(btnMaximize, "crop_square", false);
            StyleWindowButton(btnClose, "close", true);
            btnMinimize.Click += (s, e) => MinimizeClicked?.Invoke(this, EventArgs.Empty);
            btnMaximize.Click += (s, e) => MaximizeClicked?.Invoke(this, EventArgs.Empty);
            btnClose.Click += (s, e) => CloseClicked?.Invoke(this, EventArgs.Empty);

            Controls.Add(picLogo);
            Controls.Add(btnMinimize);
            Controls.Add(btnMaximize);
            Controls.Add(btnClose);
            LayoutChildren();
        }

        /// <summary>Shows the "restore" icon while the window is maximised, "maximise" otherwise.</summary>
        public void SetMaximized(bool isMaximized) => btnMaximize.IconName = isMaximized ? "filter_none" : "crop_square";

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
            button.IconColor = Color.White;
            button.FillColor = Color.Transparent;
            button.HoverFillColor = isClose ? ThemeColors.Lacquer : Color.FromArgb(60, 255, 255, 255);
            if (isClose) button.HoverIconColor = Color.White;
        }

        private void LayoutChildren()
        {
            int w = Ui.S(46);
            btnClose.SetBounds(Width - w, 0, w, Height);
            btnMaximize.SetBounds(btnClose.Left - w, 0, w, Height);
            btnMinimize.SetBounds(btnMaximize.Left - w, 0, w, Height);
            int logo = Ui.S(28);
            picLogo.SetBounds(Ui.S(14), (Height - logo) / 2, (int)Math.Round(logo * AppImages.BrandMarkAspect), logo);
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
            using (var brush = new LinearGradientBrush(ClientRectangle, ThemeColors.Brand, ThemeColors.BrandDeep, LinearGradientMode.Horizontal))
            {
                g.FillRectangle(brush, ClientRectangle);
            }
            using (var gold = new SolidBrush(Color.FromArgb(150, ThemeColors.Gold))) g.FillRectangle(gold, 0, Height - 1, Width, 1);

            int x = picLogo.Right + Ui.S(10);
            TextRenderer.DrawText(g, "Huế Travel", ThemeFonts.Serif(17), new Rectangle(x, 0, Ui.S(180), Height), Color.White,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);

            if (weather.Length == 0) return;
            Font font = ThemeFonts.Medium(12);
            Size size = TextRenderer.MeasureText(weather, font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
            var chip = new Rectangle(btnMinimize.Left - Ui.S(16) - size.Width - Ui.S(28), (Height - Ui.S(26)) / 2, size.Width + Ui.S(28), Ui.S(26));
            using (GraphicsPath path = Ui.RoundedRect(chip, chip.Height / 2f))
            using (var fill = new SolidBrush(Color.FromArgb(46, 255, 255, 255)))
            {
                g.FillPath(fill, path);
            }
            TextRenderer.DrawText(g, weather, font, chip, Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
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
