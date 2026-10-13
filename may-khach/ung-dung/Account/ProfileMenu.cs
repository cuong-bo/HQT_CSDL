using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Hue_travel.Theme;

namespace Hue_travel.Account
{
    /// <summary>
    /// The list of things the "Hồ sơ" tab offers: an icon tile, a title and a short line each, a chevron on the right.
    /// A row darkens, sinks and ripples when pressed. The danger row (sign out) is drawn in red without a chevron.
    /// </summary>
    internal sealed class ProfileMenu : Control
    {
        internal sealed class Item
        {
            public Item(string icon, string title, string subtitle, bool isDanger = false)
            {
                Icon = icon;
                Title = title;
                Subtitle = subtitle;
                IsDanger = isDanger;
            }

            public string Icon { get; }
            public string Title { get; }
            public string Subtitle { get; }
            public bool IsDanger { get; }
        }

        private const int RowHeight = 64;
        private const int TopMargin = 6;
        private const int TileSize = 40;

        private readonly PressFeedback press;
        private List<Item> items = new List<Item>();
        private int hoverIndex = -1;

        public event EventHandler<int> ItemClicked;

        public ProfileMenu()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = ThemeColors.SurfaceContainerLowest;
            Cursor = Cursors.Hand;
            press = new PressFeedback(this);
        }

        public void SetItems(IEnumerable<Item> newItems)
        {
            items = new List<Item>(newItems);
            hoverIndex = -1;
            Invalidate();
        }

        private int IndexAt(Point p)
        {
            int top = Ui.S(TopMargin);
            if (p.Y < top) return -1;
            int index = (p.Y - top) / Ui.S(RowHeight);
            return index < items.Count ? index : -1;
        }

        private Rectangle RowRect(int index) => new Rectangle(0, Ui.S(TopMargin) + index * Ui.S(RowHeight), Width, Ui.S(RowHeight));

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int index = IndexAt(e.Location);
            if (index != hoverIndex) { hoverIndex = index; Invalidate(); }
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
            if (disposing) press.Dispose();
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);
            for (int i = 0; i < items.Count; i++)
            {
                Item item = items[i];
                Rectangle row = RowRect(i);
                Color rest = i == hoverIndex ? (item.IsDanger ? Color.FromArgb(254, 242, 242) : ThemeColors.Slate50) : Color.Transparent;
                Color fill = press.Fill(i, rest, item.IsDanger ? Color.FromArgb(254, 226, 226) : ThemeColors.Slate200);
                if (fill.A > 0)
                {
                    using (var brush = new SolidBrush(fill)) g.FillRectangle(brush, row);
                }
                using (var clip = new GraphicsPath())
                {
                    clip.AddRectangle(row);
                    press.PaintRipple(g, i, clip, item.IsDanger ? ThemeColors.Red500 : ThemeColors.Sky700, 55);
                }

                int sink = press.SinkFor(i);
                int tile = Ui.S(TileSize);
                var tileRect = new Rectangle(Ui.S(16), row.Y + (row.Height - tile) / 2 + sink, tile, tile);
                Color tileColor = item.IsDanger ? Color.FromArgb(254, 226, 226) : ThemeColors.Sky100;
                Color iconColor = item.IsDanger ? ThemeColors.Red500 : ThemeColors.Primary;
                using (GraphicsPath path = Ui.RoundedRect(tileRect, Ui.S(10)))
                using (var brush = new SolidBrush(tileColor))
                {
                    g.FillPath(brush, path);
                }
                int iconPx = Ui.S(22);
                g.DrawImage(IconFont.Get(item.Icon, iconPx, iconColor), tileRect.X + (tile - iconPx) / 2, tileRect.Y + (tile - iconPx) / 2, iconPx, iconPx);

                int textLeft = tileRect.Right + Ui.S(14);
                int textWidth = Width - textLeft - Ui.S(item.IsDanger ? 16 : 44);
                const TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;
                TextRenderer.DrawText(g, item.Title, ThemeFonts.SemiBold(14), new Rectangle(textLeft, row.Y + Ui.S(12) + sink, textWidth, Ui.S(20)),
                    item.IsDanger ? ThemeColors.Red500 : ThemeColors.Slate800, flags);
                TextRenderer.DrawText(g, item.Subtitle, ThemeFonts.Regular(12), new Rectangle(textLeft, row.Y + Ui.S(34) + sink, textWidth, Ui.S(18)),
                    ThemeColors.Slate500, flags);

                if (!item.IsDanger)
                {
                    int chevron = Ui.S(18);
                    g.DrawImage(IconFont.Get("chevron_right", chevron, ThemeColors.Slate400), Width - Ui.S(16) - chevron, row.Y + (row.Height - chevron) / 2 + sink, chevron, chevron);
                }
                if (i < items.Count - 1)
                {
                    using (var pen = new Pen(ThemeColors.Slate100)) g.DrawLine(pen, textLeft, row.Bottom - 1, Width - Ui.S(16), row.Bottom - 1);
                }
            }
        }
    }
}
