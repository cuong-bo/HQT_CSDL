using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Hue_travel.Theme;

namespace Hue_travel.Panels.LayerControl
{
    public enum SwatchShape
    {
        Circle,
        RoundedSquare,
        Line
    }

    /// <summary>
    /// One row of the layer list: 16px checkbox, colour swatch, 12px medium name and a
    /// right-aligned 11px count ("38 điểm"). Unchecked rows are drawn muted like the mockup.
    /// </summary>
    public class LayerRow : Control
    {
        private const TextFormatFlags LeftFlags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine;
        private const TextFormatFlags RightFlags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter | TextFormatFlags.Right | TextFormatFlags.SingleLine;

        private bool isChecked;
        private bool isHovered;

        public event EventHandler CheckedChanged;

        public LayerRow()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Size = new Size(304, 28);
            Cursor = Cursors.Hand;
            Font = ThemeFonts.Medium(12);
            SwatchColor = ThemeColors.Primary;
            CountText = "";
        }

        [Category("Appearance")] public Color SwatchColor { get; set; }
        [Category("Appearance")] public SwatchShape SwatchShape { get; set; }
        [Category("Appearance")] public string CountText { get; set; }

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

        [Browsable(true), EditorBrowsable(EditorBrowsableState.Always)]
        public override string Text { get { return base.Text; } set { base.Text = value; Invalidate(); } }

        protected override void OnMouseEnter(EventArgs e) { isHovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { isHovered = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnClick(EventArgs e) { Checked = !Checked; base.OnClick(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (isHovered)
            {
                using (GraphicsPath hover = Ui.RoundedRect(new RectangleF(0, 0, Width, Height), 6))
                using (var brush = new SolidBrush(ThemeColors.Slate50))
                {
                    g.FillPath(brush, hover);
                }
            }

            // checkbox
            var box = new RectangleF(6.5f, (Height - 16) / 2f + 0.5f, 15f, 15f);
            using (GraphicsPath path = Ui.RoundedRect(box, 3))
            {
                if (isChecked)
                {
                    using (var brush = new SolidBrush(ThemeColors.Primary)) g.FillPath(brush, path);
                    g.DrawImage(IconFont.Get("check", 14, Color.White), 7, (Height - 14) / 2, 14, 14);
                }
                else
                {
                    using (var brush = new SolidBrush(Color.White)) g.FillPath(brush, path);
                    using (var pen = new Pen(ThemeColors.Slate300, 1f)) g.DrawPath(pen, path);
                }
            }

            // swatch
            using (var brush = new SolidBrush(SwatchColor))
            {
                if (SwatchShape == SwatchShape.Line)
                {
                    g.FillRectangle(brush, 30, Height / 2 - 1, 12, 2);
                }
                else if (SwatchShape == SwatchShape.RoundedSquare)
                {
                    using (GraphicsPath sw = Ui.RoundedRect(new RectangleF(30, (Height - 10) / 2f, 10, 10), 3)) g.FillPath(brush, sw);
                }
                else
                {
                    g.FillEllipse(brush, 30, (Height - 10) / 2f, 10, 10);
                }
            }

            Color nameColor = isChecked ? ThemeColors.Slate800 : ThemeColors.Slate700;
            Color countColor = isChecked ? ThemeColors.Slate500 : ThemeColors.Slate400;
            int countWidth = string.IsNullOrEmpty(CountText) ? 0 : TextRenderer.MeasureText(CountText, ThemeFonts.Regular(11), Size.Empty, RightFlags).Width;
            int nameLeft = 48;
            TextRenderer.DrawText(g, Text, Font, new Rectangle(nameLeft, 0, Math.Max(0, Width - nameLeft - countWidth - 14), Height), nameColor, LeftFlags);
            if (countWidth > 0)
            {
                TextRenderer.DrawText(g, CountText, ThemeFonts.Regular(11), new Rectangle(Width - countWidth - 6, 0, countWidth, Height), countColor, RightFlags);
            }
        }
    }
}
