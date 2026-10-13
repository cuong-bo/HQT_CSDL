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
        private readonly PressFeedback press;

        public CompassButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Size = new Size(36, 36);
            press = new PressFeedback(this);
        }

        protected override void OnMouseEnter(EventArgs e) { isHovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { isHovered = false; press.Up(); Invalidate(); base.OnMouseLeave(e); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) press.Down(0, e.Location);
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            press.Up();
            base.OnMouseUp(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) press.Dispose();
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Color fill = press.Fill(0, isHovered ? ThemeColors.Slate50 : Color.White, ThemeColors.Slate200);
            using (GraphicsPath path = Ui.RoundedRect(new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f), Ui.S(8)))
            using (var brush = new SolidBrush(fill))
            using (var pen = new Pen(ThemeColors.Slate200, 1f))
            {
                g.FillPath(brush, path);
                press.PaintRipple(g, 0, path, ThemeColors.Sky700, 70);
                g.DrawPath(pen, path);
            }

            // Sizes follow the control (design size 36) so the tile looks the same at any screen scale.
            int sink = press.SinkFor(0);
            int iconPx = Width * 16 / 36;
            TextRenderer.DrawText(g, "N", ThemeFonts.Bold(10), new Rectangle(0, Height / 12 + sink, Width, Height / 3), ThemeColors.Red500,
                TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            g.DrawImage(IconFont.Get("navigation", iconPx, ThemeColors.Sky600), (Width - iconPx) / 2, Height * 16 / 36 + sink, iconPx, iconPx);
        }
    }
}
