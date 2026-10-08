using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Hue_travel.Theme;

namespace Hue_travel.Auth
{
    /// <summary>
    /// The "Đăng Nhập | Đăng Ký Du Khách" tab strip: a slate-200 baseline with a 2px primary
    /// underline and bold primary label under the active tab. <see cref="ActiveTab"/> 0 = login, 1 = register.
    /// </summary>
    public class AuthTabStrip : Control
    {
        private const TextFormatFlags Flags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine;

        private int activeTab;

        public event EventHandler<int> TabSelected;

        public AuthTabStrip()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Size = new Size(388, 30);
        }

        [Category("Appearance")]
        public int ActiveTab { get { return activeTab; } set { activeTab = value; Invalidate(); } }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            int tab = e.X < Width / 2 ? 0 : 1;
            if (tab != activeTab) TabSelected?.Invoke(this, tab);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            using (var baseline = new SolidBrush(ThemeColors.Slate200)) g.FillRectangle(baseline, 0, Height - Ui.S(1), Width, Ui.S(1));

            int half = Width / 2;
            string[] labels = { "Đăng Nhập", "Đăng Ký Du Khách" };
            for (int i = 0; i < 2; i++)
            {
                bool active = i == activeTab;
                var bounds = new Rectangle(i * half, 0, half, Height - Ui.S(2));
                TextRenderer.DrawText(g, labels[i], active ? ThemeFonts.Bold(12) : ThemeFonts.Medium(12), bounds,
                    active ? ThemeColors.Primary : ThemeColors.Slate500, Flags);
                if (active)
                {
                    using (var underline = new SolidBrush(ThemeColors.Primary)) g.FillRectangle(underline, bounds.X, Height - Ui.S(2), half, Ui.S(2));
                }
            }
        }
    }
}
