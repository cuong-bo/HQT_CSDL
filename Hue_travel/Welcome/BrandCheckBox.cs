using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Hue_travel.Theme;

namespace Hue_travel.Welcome
{
    /// <summary>A purple check box with its caption ("Ghi nhớ đăng nhập"); clicking the box or the caption toggles it.</summary>
    public sealed class BrandCheckBox : Control
    {
        private bool isChecked;
        private bool isHovered;

        public event EventHandler CheckedChanged;

        public BrandCheckBox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.Selectable, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Font = ThemeFonts.Medium(13);
            ForeColor = ThemeColors.Slate700;
            TabStop = true;
        }

        public bool Checked
        {
            get { return isChecked; }
            set { if (isChecked == value) return; isChecked = value; Invalidate(); CheckedChanged?.Invoke(this, EventArgs.Empty); }
        }

        protected override void OnMouseEnter(EventArgs e) { isHovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { isHovered = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnClick(EventArgs e) { Checked = !Checked; base.OnClick(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space) { Checked = !Checked; e.Handled = true; }
            base.OnKeyDown(e);
        }

        protected override bool IsInputKey(Keys keyData) => keyData == Keys.Space || base.IsInputKey(keyData);

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int box = Ui.S(20);
            var rect = new RectangleF(0.5f, (Height - box) / 2f + 0.5f, box - 1f, box - 1f);
            using (GraphicsPath path = Ui.RoundedRect(rect, Ui.S(5)))
            {
                using (var fill = new SolidBrush(isChecked ? ThemeColors.BrandPurple : Color.White)) g.FillPath(fill, path);
                Color border = isChecked || isHovered || Focused ? ThemeColors.BrandPurple : ThemeColors.Slate300;
                using (var pen = new Pen(border, Focused ? 1.8f : 1f)) g.DrawPath(pen, path);
            }
            if (isChecked)
            {
                int mark = Ui.S(14);
                g.DrawImage(IconFont.Get("check", mark, Color.White), (box - mark) / 2, (Height - mark) / 2, mark, mark);
            }
            TextRenderer.DrawText(g, Text, Font, new Rectangle(box + Ui.S(10), 0, Width - box - Ui.S(10), Height), ForeColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
        }
    }
}
