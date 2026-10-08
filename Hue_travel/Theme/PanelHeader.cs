using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Hue_travel.Theme
{
    /// <summary>
    /// The h-8 title strip of a floating panel: leading icon, 12px semibold title, and the
    /// minimise / close buttons. <see cref="Prominent"/> selects the solid primary header
    /// (white text) over the light sky-50 one used by the layer panel.
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
            Height = 32;
            Dock = DockStyle.Top;
            Font = ThemeFonts.SemiBold(12);

            ConfigureButton(minimizeButton, "minimize");
            ConfigureButton(closeButton, "close");
            closeButton.HoverFillColor = ThemeColors.Error;
            closeButton.HoverIconColor = Color.White;
            minimizeButton.Click += (s, e) =>
            {
                (FindForm() as FloatingPanelForm)?.ToggleCollapsed(Height);
                MinimizeClicked?.Invoke(this, EventArgs.Empty);
            };
            Cursor = Cursors.SizeAll; // the mockup's cursor-move title strip
            closeButton.Click += (s, e) => CloseClicked?.Invoke(this, EventArgs.Empty);
            Controls.Add(minimizeButton);
            Controls.Add(closeButton);
            ApplyStyle();
            LayoutButtons();
        }

        [Category("Appearance")]
        public string IconName { get { return iconName; } set { iconName = value ?? ""; Invalidate(); } }

        /// <summary>True: solid primary bar with white text. False: sky-50 bar with dark text.</summary>
        [Category("Appearance")]
        public bool Prominent { get { return prominent; } set { prominent = value; ApplyStyle(); Invalidate(); } }

        [Browsable(true), EditorBrowsable(EditorBrowsableState.Always)]
        public override string Text { get { return base.Text; } set { base.Text = value; Invalidate(); } }

        private static void ConfigureButton(IconButton button, string icon)
        {
            button.AutoWidth = false;
            button.IconName = icon;
            button.IconSize = 12;
            button.Radius = 4;
            button.Size = new Size(20, 20);
            button.FillColor = Color.Transparent;
        }

        private void ApplyStyle()
        {
            Color iconColor = prominent ? Color.White : ThemeColors.Slate500;
            Color hover = prominent ? ThemeColors.Sky700 : ThemeColors.Sky100;
            minimizeButton.IconColor = iconColor;
            minimizeButton.HoverFillColor = hover;
            closeButton.IconColor = iconColor;
        }

        private void LayoutButtons()
        {
            int top = (Height - closeButton.Height) / 2;
            closeButton.Location = new Point(Width - 12 - closeButton.Width, top);
            minimizeButton.Location = new Point(closeButton.Left - 4 - minimizeButton.Width, top);
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
                left = Math.Max(area.Left - form.Width + 80, Math.Min(area.Right - 80, left));
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

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutButtons();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Color back = prominent ? ThemeColors.Primary : ThemeColors.Sky50;
            Color fore = prominent ? Color.White : ThemeColors.OnSurface;
            using (var brush = new SolidBrush(back))
            {
                e.Graphics.FillRectangle(brush, ClientRectangle);
            }
            if (!prominent)
            {
                using (var pen = new Pen(ThemeColors.Sky100))
                {
                    e.Graphics.DrawLine(pen, 0, Height - 1, Width, Height - 1);
                }
            }

            int x = 12;
            if (iconName.Length > 0)
            {
                Color iconColor = prominent ? Color.White : ThemeColors.Primary;
                e.Graphics.DrawImage(IconFont.Get(iconName, 16, iconColor), x, (Height - 16) / 2, 16, 16);
                x += 16 + 6;
            }
            TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(x, 0, Math.Max(0, minimizeButton.Left - x), Height), fore,
                TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
        }
    }
}
