using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Hue_travel.Theme
{
    /// <summary>
    /// The title strip of a floating panel: leading icon, a serif title, and the minimise / close buttons. <see cref="Prominent"/>
    /// selects the brand gradient (white text) over the light paper strip with a hairline.
    /// </summary>
    public class PanelHeader : Control
    {
        private readonly IconButton minimizeButton = new IconButton();
        private readonly IconButton closeButton = new IconButton();
        private string iconName = "";
        private bool prominent = true;

        public event EventHandler CloseClicked;
        public event EventHandler MinimizeClicked;

        public PanelHeader()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Height = 52;
            Dock = DockStyle.Top;
            Font = ThemeFonts.Serif(17);

            ConfigureButton(minimizeButton, "minimize");
            ConfigureButton(closeButton, "close");
            closeButton.HoverFillColor = ThemeColors.Lacquer;
            closeButton.HoverIconColor = Color.White;
            minimizeButton.Click += (s, e) =>
            {
                (FindForm() as FloatingPanelForm)?.ToggleCollapsed(Height);
                MinimizeClicked?.Invoke(this, EventArgs.Empty);
            };
            Cursor = Cursors.SizeAll;
            closeButton.Click += (s, e) => CloseClicked?.Invoke(this, EventArgs.Empty);
            Controls.Add(minimizeButton);
            minimizeButton.Visible = false; // only the main window keeps a minimise mark
            Controls.Add(closeButton);
            ApplyStyle();
            LayoutButtons();
        }

        [Category("Appearance")]
        public string IconName { get { return iconName; } set { iconName = value ?? ""; Invalidate(); } }

        /// <summary>True: brand gradient with white text. False: paper strip with dark text.</summary>
        [Category("Appearance")]
        public bool Prominent { get { return prominent; } set { prominent = value; ApplyStyle(); Invalidate(); } }

        /// <summary>False for dialog-like cards that make no sense collapsed.</summary>
        [Category("Behavior"), DefaultValue(false)]
        public bool ShowMinimize
        {
            get { return minimizeButton.Visible; }
            set { minimizeButton.Visible = value; Invalidate(); }
        }

        [Browsable(true), EditorBrowsable(EditorBrowsableState.Always)]
        public override string Text { get { return base.Text; } set { base.Text = value; Invalidate(); } }

        // Round 28 px buttons (radius = half the size is a circle).
        private static void ConfigureButton(IconButton button, string icon)
        {
            button.AutoWidth = false;
            button.IconName = icon;
            button.IconSize = 14;
            button.Radius = ThemeMetrics.RadiusFull;
            button.Size = new Size(28, 28);
            button.FillColor = Color.Transparent;
        }

        private void ApplyStyle()
        {
            Color iconColor = prominent ? Color.White : ThemeColors.InkSoft;
            Color hover = prominent ? Color.FromArgb(70, 255, 255, 255) : ThemeColors.BrandSoft;
            minimizeButton.IconColor = iconColor;
            minimizeButton.HoverFillColor = hover;
            closeButton.IconColor = iconColor;
        }

        private void LayoutButtons()
        {
            int top = (Height - closeButton.Height) / 2;
            closeButton.Location = new Point(Width - Ui.S(14) - closeButton.Width, top);
            minimizeButton.Location = new Point(closeButton.Left - Ui.S(4) - minimizeButton.Width, top);
        }

        private Point dragCursorStart;
        private Point dragFormStart;
        private bool isDragging;

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Form form = FindForm();
            if (e.Button != MouseButtons.Left || form == null) return;
            isDragging = true;
            dragCursorStart = Cursor.Position;
            dragFormStart = form.Location;
            Capture = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Form form = FindForm();
            if (!isDragging || form == null) return;

            int left = dragFormStart.X + Cursor.Position.X - dragCursorStart.X;
            int top = dragFormStart.Y + Cursor.Position.Y - dragCursorStart.Y;
            Form host = form.Owner;
            if (host != null)
            {
                // Keep part of the header reachable inside the host window.
                var area = new Rectangle(host.PointToScreen(Point.Empty), host.ClientSize);
                left = Math.Max(area.Left - form.Width + Ui.S(80), Math.Min(area.Right - Ui.S(80), left));
                top = Math.Max(area.Top, Math.Min(area.Bottom - Height, top));
            }
            form.Location = new Point(left, top);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (!isDragging) return;
            isDragging = false;
            Capture = false;
            (FindForm() as FloatingPanelForm)?.RememberPosition();
        }

        // Control.Scale grows children after OnResize ran, so lay out again once scaling is done.
        protected override void OnLayout(LayoutEventArgs e) { base.OnLayout(e); LayoutButtons(); }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutButtons();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Color fore = prominent ? Color.White : ThemeColors.Ink;
            if (prominent)
            {
                using (var brush = new LinearGradientBrush(ClientRectangle, ThemeColors.Brand, ThemeColors.BrandDeep, LinearGradientMode.Horizontal))
                {
                    g.FillRectangle(brush, ClientRectangle);
                }
                // a thin gold line under the header: the royal touch
                using (var gold = new SolidBrush(Color.FromArgb(200, ThemeColors.Gold)))
                {
                    g.FillRectangle(gold, 0, Height - Math.Max(2, Ui.S(2)), Width, Math.Max(2, Ui.S(2)));
                }
            }
            else
            {
                using (var brush = new SolidBrush(ThemeColors.Paper)) g.FillRectangle(brush, ClientRectangle);
                using (var pen = new Pen(ThemeColors.Line)) g.DrawLine(pen, 0, Height - 1, Width, Height - 1);
            }

            int x = Ui.S(16);
            if (iconName.Length > 0)
            {
                Color iconColor = prominent ? Color.White : ThemeColors.Brand;
                int iconPx = Ui.S(20);
                g.DrawImage(IconFont.Get(iconName, iconPx, iconColor), x, (Height - iconPx) / 2, iconPx, iconPx);
                x += iconPx + Ui.S(10);
            }
            int right = (minimizeButton.Visible ? minimizeButton.Left : closeButton.Left) - Ui.S(8);
            TextRenderer.DrawText(g, Text, Font, new Rectangle(x, 0, Math.Max(0, right - x), Height), fore,
                TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
        }
    }
}
