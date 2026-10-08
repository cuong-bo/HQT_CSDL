using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Hue_travel.Theme;

namespace Hue_travel.Panels.BaseMapSwitcher
{
    /// <summary>One base map choice: a coloured preview strip with an icon and a caption; the selected tile has a primary border.</summary>
    public class BaseMapTile : Control
    {
        private bool isSelected;
        private bool isHovered;

        public BaseMapTile()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Size = new Size(72, 58);
            IconName = "map";
            IconColor = ThemeColors.Primary;
            PreviewColor = ThemeColors.Sky200;
        }

        [Category("Appearance")] public string IconName { get; set; }
        [Category("Appearance")] public Color IconColor { get; set; }
        [Category("Appearance")] public Color PreviewColor { get; set; }

        [Category("Appearance")]
        public bool Selected { get { return isSelected; } set { isSelected = value; Invalidate(); } }

        [Browsable(true), EditorBrowsable(EditorBrowsableState.Always)]
        public override string Text { get { return base.Text; } set { base.Text = value; Invalidate(); } }

        protected override void OnMouseEnter(EventArgs e) { isHovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { isHovered = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (GraphicsPath card = Ui.RoundedRect(new RectangleF(1f, 1f, Width - 2f, Height - 2f), Ui.S(6)))
            using (var fill = new SolidBrush(isSelected ? ThemeColors.Sky50 : Color.White))
            using (var pen = new Pen(isSelected ? ThemeColors.Primary : (isHovered ? ThemeColors.Slate300 : ThemeColors.Slate200), isSelected ? 2f : 1f))
            {
                g.FillPath(fill, card);
                g.DrawPath(pen, card);
            }

            var preview = new RectangleF(Ui.S(7), Ui.S(7), Width - Ui.S(14), Ui.S(28));
            using (GraphicsPath path = Ui.RoundedRect(preview, Ui.S(4)))
            using (var brush = new SolidBrush(PreviewColor))
            {
                g.FillPath(brush, path);
            }
            int glyph = Ui.S(16);
            g.DrawImage(IconFont.Get(IconName, glyph, IconColor), (Width - glyph) / 2, (int)(preview.Y + (preview.Height - glyph) / 2), glyph, glyph);

            TextRenderer.DrawText(g, Text, isSelected ? ThemeFonts.Bold(10) : ThemeFonts.Medium(10), new Rectangle(0, Ui.S(38), Width, Ui.S(16)),
                isSelected ? ThemeColors.Primary : ThemeColors.Slate700,
                TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }
    }
}
