using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Hue_travel.Theme;

namespace Hue_travel.Shell
{
    /// <summary>
    /// The slim title strip of the main window (40 px): the brand mark on the left, the window buttons on the right. The search box is no
    /// longer here: it floats on the map (<see cref="SearchPill"/>). Written in code, without a designer file; the colours are the ones the
    /// bar always had.
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

        public TitleBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Dock = DockStyle.Top;
            Height = DesignHeight;
            BackColor = ThemeColors.SurfaceContainerLow;

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

        private static void StyleWindowButton(IconButton button, string icon, bool isClose)
        {
            button.AutoWidth = false;
            button.TabStop = false;
            button.IconName = icon;
            button.IconSize = 18;
            button.Radius = 0;
            button.IconColor = ThemeColors.OnSurfaceVariant;
            button.FillColor = Color.Transparent;
            button.HoverFillColor = isClose ? ThemeColors.Error : ThemeColors.SurfaceContainerHigh;
            if (isClose) button.HoverIconColor = Color.White;
        }

        private void LayoutChildren()
        {
            int w = Ui.S(48);
            btnClose.SetBounds(Width - w, 0, w, Height - 1);
            btnMaximize.SetBounds(btnClose.Left - w, 0, w, Height - 1);
            btnMinimize.SetBounds(btnMaximize.Left - w, 0, w, Height - 1);
            int logo = Ui.S(30);
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
            e.Graphics.Clear(BackColor);
            using (var pen = new Pen(ThemeColors.SurfaceContainer)) e.Graphics.DrawLine(pen, 0, Height - 1, Width, Height - 1);
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
