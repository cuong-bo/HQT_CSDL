using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Hue_travel.Theme;

namespace Hue_travel.MapEngine.Overlays
{
    /// <summary>The 36px "N" compass tile (red N above a sky-600 navigation arrow) that resets the view north.</summary>
    public class CompassButton : Control
    {
        private bool isHovered;

        public CompassButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Size = new Size(36, 36);
        }

        protected override void OnMouseEnter(EventArgs e) { isHovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { isHovered = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = Ui.RoundedRect(new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f), 8))
            using (var fill = new SolidBrush(isHovered ? ThemeColors.Slate50 : Color.White))
            using (var pen = new Pen(ThemeColors.Slate200, 1f))
            {
                g.FillPath(fill, path);
                g.DrawPath(pen, path);
            }
            TextRenderer.DrawText(g, "N", ThemeFonts.Bold(10), new Rectangle(0, 3, Width, 12), ThemeColors.Red500,
                TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            g.DrawImage(IconFont.Get("navigation", 16, ThemeColors.Sky600), (Width - 16) / 2, 16, 16, 16);
        }
    }
}
