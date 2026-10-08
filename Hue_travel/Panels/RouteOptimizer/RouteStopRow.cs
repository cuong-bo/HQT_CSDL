using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Hue_travel.Theme;

namespace Hue_travel.Panels.RouteOptimizer
{
    /// <summary>
    /// A selectable stop in the route list: checkbox, bold title, grey subtitle and an
    /// order badge ("Thứ tự: 1") that is replaced by "Chưa chọn" when unchecked.
    /// </summary>
    public class RouteStopRow : Control
    {
        private const TextFormatFlags MeasureFlags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;
        private const TextFormatFlags LeftFlags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine;

        private bool isChecked;
        private int order;

        public event EventHandler CheckedChanged;

        public RouteStopRow()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Size = new Size(320, 44);
            Subtitle = "";
        }

        [Category("Appearance")] public string Subtitle { get; set; }

        [Category("Appearance")]
        public bool Checked
        {
            get { return isChecked; }
            set
            {
                if (isChecked == value) return;
                isChecked = value;
                Invalidate();
                CheckedChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>Visit order shown in the badge (1-based); ignored while unchecked.</summary>
        [Category("Appearance")]
        public int Order { get { return order; } set { order = value; Invalidate(); } }

        [Browsable(true), EditorBrowsable(EditorBrowsableState.Always)]
        public override string Text { get { return base.Text; } set { base.Text = value; Invalidate(); } }

        protected override void OnClick(EventArgs e) { Checked = !Checked; base.OnClick(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (isChecked)
            {
                using (GraphicsPath card = Ui.RoundedRect(new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f), 4))
                using (var fill = new SolidBrush(Color.White))
                using (var pen = new Pen(ThemeColors.Slate200, 1f))
                {
                    g.FillPath(fill, card);
                    g.DrawPath(pen, card);
                }
            }

            var box = new RectangleF(8.5f, (Height - 16) / 2f + 0.5f, 15f, 15f);
            using (GraphicsPath path = Ui.RoundedRect(box, 3))
            {
                if (isChecked)
                {
                    using (var brush = new SolidBrush(ThemeColors.Primary)) g.FillPath(brush, path);
                    g.DrawImage(IconFont.Get("check", 14, Color.White), 9, (Height - 14) / 2, 14, 14);
                }
                else
                {
                    using (var brush = new SolidBrush(Color.White)) g.FillPath(brush, path);
                    using (var pen = new Pen(ThemeColors.Slate300, 1f)) g.DrawPath(pen, path);
                }
            }

            // order badge / "Chưa chọn"
            string badgeText = isChecked ? "Thứ tự: " + order : "Chưa chọn";
            Font badgeFont = isChecked ? ThemeFonts.Bold(11) : ThemeFonts.Regular(11);
            int badgeTextWidth = TextRenderer.MeasureText(badgeText, badgeFont, Size.Empty, MeasureFlags).Width;
            int right = Width - 8;
            if (isChecked)
            {
                var badge = new RectangleF(right - badgeTextWidth - 16, (Height - 20) / 2f, badgeTextWidth + 16, 20);
                using (GraphicsPath path = Ui.RoundedRect(badge, 4))
                using (var fill = new SolidBrush(ThemeColors.Sky50))
                using (var pen = new Pen(ThemeColors.Sky200, 1f))
                {
                    g.FillPath(fill, path);
                    g.DrawPath(pen, path);
                }
                TextRenderer.DrawText(g, badgeText, badgeFont, Rectangle.Round(badge), ThemeColors.Sky700,
                    TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.SingleLine);
            }
            else
            {
                TextRenderer.DrawText(g, badgeText, badgeFont, new Rectangle(right - badgeTextWidth, 0, badgeTextWidth, Height), ThemeColors.Slate400, LeftFlags);
            }

            int textRight = right - badgeTextWidth - (isChecked ? 24 : 8);
            Font titleFont = isChecked ? ThemeFonts.SemiBold(12) : ThemeFonts.Medium(12);
            Color titleColor = isChecked ? ThemeColors.Slate800 : ThemeColors.Slate700;
            TextRenderer.DrawText(g, Text, titleFont, new Rectangle(34, 5, Math.Max(0, textRight - 34), 17), titleColor, LeftFlags);
            TextRenderer.DrawText(g, Subtitle, ThemeFonts.Regular(11), new Rectangle(34, 22, Math.Max(0, textRight - 34), 16), ThemeColors.Slate500, LeftFlags);
        }
    }
}
