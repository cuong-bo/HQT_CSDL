using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Hue_travel.Theme;

namespace Hue_travel.Shell
{
    /// <summary>
    /// The left navigation rail of a web GIS: one tab per tool (explore, route, guide), an icon in a pill with a short
    /// caption below. The open tab is tinted and marked with a bar; pressing a tab darkens it, sinks it and ripples.
    /// </summary>
    public sealed class NavRail : Control
    {
        public const int Explore = 0;
        public const int Route = 1;
        public const int Guide = 2;
        public const int Profile = 3;

        private static readonly string[] Icons = { "search", "alt_route", "menu_book", "account_circle" };
        private static readonly string[] Labels = { "Khám phá", "Lộ trình", "Hướng dẫn", "Hồ sơ" };
        private static readonly string[] Tips = { "Tìm địa điểm theo tên và nhóm", "Lên lộ trình tham quan ngắn nhất", "Gợi ý các tour tham quan", "Hồ sơ, yêu thích, lộ trình đã lưu và đăng xuất" };

        private const int DesignWidth = 80;
        private const int ItemHeight = 76;
        private const int TopMargin = 10;
        private const int PillWidth = 56;
        private const int PillHeight = 32;
        private const int IconSize = 22;

        private readonly PressFeedback press;
        private readonly ToolTip tooltip = new ToolTip();
        private int activeIndex = -1;
        private int hoverIndex = -1;
        private int tipIndex = -1;

        public event EventHandler<int> ItemClicked;

        public NavRail()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = ThemeColors.SurfaceContainerLowest;
            Cursor = Cursors.Hand;
            Width = DesignWidth;
            press = new PressFeedback(this);
        }

        /// <summary>The tab whose panel is open, or -1 when none is.</summary>
        public int ActiveIndex
        {
            get { return activeIndex; }
            set { if (activeIndex == value) return; activeIndex = value; Invalidate(); }
        }

        private int IndexAt(Point p)
        {
            int top = Ui.S(TopMargin);
            if (p.Y < top) return -1;
            int index = (p.Y - top) / Ui.S(ItemHeight);
            return index < Icons.Length ? index : -1;
        }

        private Rectangle ItemRect(int index) => new Rectangle(0, Ui.S(TopMargin) + index * Ui.S(ItemHeight), Width - 1, Ui.S(ItemHeight));

        private Rectangle PillRect(int index)
        {
            Rectangle item = ItemRect(index);
            return new Rectangle(item.X + (item.Width - Ui.S(PillWidth)) / 2, item.Y + Ui.S(8), Ui.S(PillWidth), Ui.S(PillHeight));
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int index = IndexAt(e.Location);
            if (index != hoverIndex)
            {
                hoverIndex = index;
                if (index != tipIndex)
                {
                    tipIndex = index;
                    tooltip.SetToolTip(this, index >= 0 ? Tips[index] : "");
                }
                Invalidate();
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hoverIndex = -1;
            press.Up();
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            int index = IndexAt(e.Location);
            if (e.Button == MouseButtons.Left && index >= 0) press.Down(index, e.Location);
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            press.Up();
            base.OnMouseUp(e);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            int index = IndexAt(e.Location);
            if (e.Button == MouseButtons.Left && index >= 0) ItemClicked?.Invoke(this, index);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                press.Dispose();
                tooltip.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);
            using (var pen = new Pen(ThemeColors.Slate200)) g.DrawLine(pen, Width - 1, 0, Width - 1, Height);

            for (int i = 0; i < Icons.Length; i++)
            {
                bool active = i == activeIndex;
                Rectangle item = ItemRect(i);
                Rectangle pill = PillRect(i);

                Color rest = active ? ThemeColors.Sky100 : (i == hoverIndex ? ThemeColors.Slate100 : Color.Transparent);
                Color pressedTint = active ? ThemeColors.Sky200 : ThemeColors.Slate200;
                Color fill = press.Fill(i, rest, pressedTint);
                if (fill.A > 0)
                {
                    using (GraphicsPath path = Ui.RoundedRect(pill, pill.Height / 2f))
                    using (var brush = new SolidBrush(fill))
                    {
                        g.FillPath(brush, path);
                    }
                }
                using (GraphicsPath clip = Ui.RoundedRect(pill, pill.Height / 2f))
                {
                    press.PaintRipple(g, i, clip, ThemeColors.Sky700, 70);
                }

                if (active)
                {
                    using (GraphicsPath bar = Ui.RoundedRect(new RectangleF(0, pill.Y + Ui.S(4), Ui.S(3), pill.Height - Ui.S(8)), Ui.S(2)))
                    using (var brush = new SolidBrush(ThemeColors.Primary))
                    {
                        g.FillPath(brush, bar);
                    }
                }

                int sink = press.SinkFor(i);
                Color iconColor = active ? ThemeColors.Primary : ThemeColors.Slate600;
                int iconPx = Ui.S(IconSize);
                g.DrawImage(IconFont.Get(Icons[i], iconPx, iconColor), pill.X + (pill.Width - iconPx) / 2, pill.Y + (pill.Height - iconPx) / 2 + sink, iconPx, iconPx);

                var labelBounds = new Rectangle(0, pill.Bottom + Ui.S(4) + sink, item.Width, Ui.S(18));
                TextRenderer.DrawText(g, Labels[i], active ? ThemeFonts.Bold(11) : ThemeFonts.Medium(11), labelBounds,
                    active ? ThemeColors.Primary : ThemeColors.Slate700,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
            }
        }
    }
}
