using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Hue_travel.Theme;

namespace Hue_travel.Panels.TravelGuide
{
    /// <summary>A suggested tour: bold title with a coloured badge on the right, grey description below.</summary>
    public class TourCard : Control
    {
        private const TextFormatFlags MeasureFlags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;
        private const TextFormatFlags LeftFlags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine;

        private bool isHovered;

        public TourCard()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Size = new Size(356, 52);
            Description = "";
            Badge = "";
            BadgeFillColor = ThemeColors.Sky100;
            BadgeTextColor = ThemeColors.Primary;
        }

        [Category("Appearance")] public string Description { get; set; }
        /// <summary>The small badge text ("Khuyên dùng", "19:00 - 21:00", ...).</summary>
        [Category("Appearance")] public string Badge { get; set; }
        [Category("Appearance")] public Color BadgeFillColor { get; set; }
        [Category("Appearance")] public Color BadgeTextColor { get; set; }

        [Browsable(true), EditorBrowsable(EditorBrowsableState.Always)]
        public override string Text { get { return base.Text; } set { base.Text = value; Invalidate(); } }

        protected override void OnMouseEnter(EventArgs e) { isHovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { isHovered = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath card = Ui.RoundedRect(new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f), Ui.S(4)))
            using (var fill = new SolidBrush(isHovered ? ThemeColors.Sky50 : ThemeColors.Slate50))
            using (var pen = new Pen(ThemeColors.Slate200, 1f))
            {
                g.FillPath(fill, card);
                g.DrawPath(pen, card);
            }

            Font badgeFont = ThemeFonts.SemiBold(10);
            int badgeWidth = string.IsNullOrEmpty(Badge) ? 0 : TextRenderer.MeasureText(Badge, badgeFont, Size.Empty, MeasureFlags).Width + Ui.S(12);
            if (badgeWidth > 0)
            {
                var badge = new Rectangle(Width - Ui.S(8) - badgeWidth, Ui.S(8), badgeWidth, Ui.S(18));
                using (GraphicsPath path = Ui.RoundedRect(badge, Ui.S(4)))
                using (var brush = new SolidBrush(BadgeFillColor))
                {
                    g.FillPath(brush, path);
                }
                TextRenderer.DrawText(g, Badge, badgeFont, badge, BadgeTextColor,
                    TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.SingleLine);
            }

            TextRenderer.DrawText(g, Text, ThemeFonts.Bold(12), new Rectangle(Ui.S(8), Ui.S(6), Math.Max(0, Width - Ui.S(24) - badgeWidth), Ui.S(20)), ThemeColors.Slate800, LeftFlags);
            TextRenderer.DrawText(g, Description, ThemeFonts.Regular(11), new Rectangle(Ui.S(8), Ui.S(28), Width - Ui.S(16), Ui.S(18)), ThemeColors.Slate600, LeftFlags);
        }
    }
}
