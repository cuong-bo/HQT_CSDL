using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using Hue_travel.MapEngine;
using Hue_travel.Models;
using Hue_travel.Theme;

namespace Hue_travel.Panels.Shared
{
    /// <summary>One line of a <see cref="PoiRowList"/>.</summary>
    internal sealed class PoiRow
    {
        public PoiRow(Poi poi) { Poi = poi; }

        public Poi Poi { get; }

        /// <summary>The second line; the place's own type text when null.</summary>
        public string Subtitle { get; set; }

        /// <summary>Text on the right (a distance); only shown when the list has no switches.</summary>
        public string Right { get; set; }

        /// <summary>The switch state when the list shows switches.</summary>
        public bool On { get; set; } = true;

        /// <summary>True when the place is already in the itinerary (the add button shows a tick).</summary>
        public bool Added { get; set; }
    }

    /// <summary>
    /// A scrolling list of places, drawn at the screen's resolution: the group's round badge, the name, a second line and
    /// either a switch (layer list) or a distance (nearby list). Rows darken, sink and ripple when pressed; a slim scroll bar
    /// appears when the rows do not fit.
    /// </summary>
    internal sealed class PoiRowList : Control
    {
        private const int RowHeight = 60;
        private const int BadgeSize = 38;

        private readonly PressFeedback press;
        private List<PoiRow> rows = new List<PoiRow>();
        private int scroll;
        private int hoverIndex = -1;
        private int selectedIndex = -1;
        private bool dragging;
        private int dragStartY, dragStartScroll;
        private bool stealFocus = true;

        public event EventHandler<PoiRow> RowClicked;
        public event EventHandler<PoiRow> SwitchToggled;

        public PoiRowList()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            BackColor = Color.White;
            Cursor = Cursors.Hand;
            press = new PressFeedback(this);
        }

        /// <summary>True when each row has a switch on the right (layer list).</summary>
        public bool ShowSwitch { get; set; }

        /// <summary>True when each row has a round add button on the right (picking places for the itinerary).</summary>
        public bool ShowAddButton { get; set; }

        /// <summary>Raised when the add button of a row is pressed.</summary>
        public event EventHandler<PoiRow> AddClicked;

        private Rectangle AddRect(int rowTop)
        {
            int size = Ui.S(30);
            int right = Width - Ui.S(14) - (NeedsScrollBar ? ScrollBarWidth : 0);
            return new Rectangle(right - size, rowTop + (RowPx - size) / 2, size, size);
        }

        /// <summary>False for a list that must not take the keyboard focus (the search drop-down under the search box).</summary>
        public bool StealFocus
        {
            get { return stealFocus; }
            set { stealFocus = value; SetStyle(ControlStyles.Selectable, value); }
        }

        /// <summary>Shown in the middle when there are no rows.</summary>
        public string EmptyText { get; set; } = "";

        public int RowCount => rows.Count;

        public PoiRow RowAt(int index) => index >= 0 && index < rows.Count ? rows[index] : null;

        public int SelectedIndex
        {
            get { return selectedIndex; }
            set { selectedIndex = value; EnsureVisible(value); Invalidate(); }
        }

        /// <summary>Height the rows need, for a list that grows with its rows.</summary>
        public int ContentHeight => rows.Count * Ui.S(RowHeight);

        public void SetRows(IEnumerable<PoiRow> newRows)
        {
            rows = new List<PoiRow>(newRows);
            scroll = 0;
            hoverIndex = -1;
            selectedIndex = -1;
            Invalidate();
        }

        public void Repaint() => Invalidate();

        private int RowPx => Ui.S(RowHeight);

        private int MaxScroll => Math.Max(0, ContentHeight - Height);

        private bool NeedsScrollBar => MaxScroll > 0;

        private int ScrollBarWidth => Ui.S(10);

        private void EnsureVisible(int index)
        {
            if (index < 0) return;
            int top = index * RowPx, bottom = top + RowPx;
            if (top < scroll) scroll = top;
            else if (bottom > scroll + Height) scroll = bottom - Height;
            scroll = Math.Max(0, Math.Min(MaxScroll, scroll));
        }

        private int IndexAt(Point p)
        {
            if (p.Y < 0 || p.Y >= Height) return -1;
            int index = (p.Y + scroll) / RowPx;
            return index < rows.Count ? index : -1;
        }

        private Rectangle SwitchRect(int rowTop)
        {
            int w = Ui.S(ToggleSwitch.DesignWidth), h = Ui.S(ToggleSwitch.DesignHeight);
            int right = Width - Ui.S(14) - (NeedsScrollBar ? ScrollBarWidth : 0);
            return new Rectangle(right - w, rowTop + (RowPx - h) / 2, w, h);
        }

        private Rectangle ThumbRect()
        {
            int track = Height - Ui.S(8);
            int thumb = Math.Max(Ui.S(28), (int)((long)track * Height / Math.Max(1, ContentHeight)));
            int y = Ui.S(4) + (MaxScroll == 0 ? 0 : (int)((long)(track - thumb) * scroll / MaxScroll));
            return new Rectangle(Width - Ui.S(8), y, Ui.S(5), thumb);
        }

        // ------------------------------------------------------------------ input

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            if (stealFocus && CanFocus) Focus();   // so the wheel scrolls this list
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (dragging)
            {
                int track = Height - Ui.S(8) - ThumbRect().Height;
                if (track > 0) scroll = Math.Max(0, Math.Min(MaxScroll, dragStartScroll + (int)((long)(e.Y - dragStartY) * MaxScroll / track)));
                Invalidate();
                return;
            }
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
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            if (NeedsScrollBar && e.X >= Width - ScrollBarWidth - Ui.S(2))
            {
                dragging = true;
                dragStartY = e.Y;
                dragStartScroll = scroll;
                return;
            }
            int index = IndexAt(e.Location);
            if (index >= 0) press.Down(index, e.Location);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            dragging = false;
            press.Up();
            base.OnMouseUp(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (!NeedsScrollBar) return;
            scroll = Math.Max(0, Math.Min(MaxScroll, scroll - Math.Sign(e.Delta) * RowPx));
            Invalidate();
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button != MouseButtons.Left) return;
            int index = IndexAt(e.Location);
            if (index < 0) return;
            PoiRow row = rows[index];
            Rectangle hit = SwitchRect(index * RowPx - scroll);
            hit.Inflate(Ui.S(8), Ui.S(8));
            if (ShowSwitch && hit.Contains(e.Location))
            {
                row.On = !row.On;
                Invalidate();
                SwitchToggled?.Invoke(this, row);
                return;
            }
            if (ShowAddButton)
            {
                Rectangle add = AddRect(index * RowPx - scroll);
                add.Inflate(Ui.S(8), Ui.S(8));
                if (add.Contains(e.Location))
                {
                    AddClicked?.Invoke(this, row);
                    Invalidate();
                    return;
                }
            }
            selectedIndex = index;
            Invalidate();
            RowClicked?.Invoke(this, row);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            scroll = Math.Max(0, Math.Min(MaxScroll, scroll));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) press.Dispose();
            base.Dispose(disposing);
        }

        // ------------------------------------------------------------------ painting

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);

            if (rows.Count == 0)
            {
                TextRenderer.DrawText(g, EmptyText, ThemeFonts.Regular(13), new Rectangle(Ui.S(16), 0, Width - Ui.S(32), Height), ThemeColors.Slate500,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
                return;
            }

            int first = Math.Max(0, scroll / RowPx);
            int last = Math.Min(rows.Count - 1, (scroll + Height) / RowPx);
            int rightLimit = Width - (NeedsScrollBar ? ScrollBarWidth + Ui.S(4) : 0);
            const TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;

            for (int i = first; i <= last; i++)
            {
                PoiRow row = rows[i];
                int top = i * RowPx - scroll;
                var rowRect = new Rectangle(0, top, rightLimit, RowPx);
                bool dim = ShowSwitch && !row.On;

                Color rest = i == selectedIndex ? ThemeColors.Sky50 : (i == hoverIndex ? ThemeColors.Slate50 : Color.Transparent);
                Color fill = press.Fill(i, rest, ThemeColors.Slate200);
                if (fill.A > 0)
                {
                    using (var brush = new SolidBrush(fill)) g.FillRectangle(brush, rowRect);
                }
                using (var clip = new GraphicsPath())
                {
                    clip.AddRectangle(rowRect);
                    press.PaintRipple(g, i, clip, ThemeColors.Sky700, 55);
                }
                if (i == selectedIndex)
                {
                    using (var bar = new SolidBrush(ThemeColors.Primary)) g.FillRectangle(bar, 0, top + Ui.S(10), Ui.S(3), RowPx - Ui.S(20));
                }

                int sink = press.SinkFor(i);
                int badge = Ui.S(BadgeSize);
                var badgeRect = new Rectangle(Ui.S(14), top + (RowPx - badge) / 2 + sink, badge, badge);
                DrawBadge(g, row.Poi.Category, badgeRect, dim);

                int textLeft = badgeRect.Right + Ui.S(12);
                int rightEdge = rightLimit - Ui.S(14);
                if (ShowSwitch)
                {
                    Rectangle sw = SwitchRect(top);
                    sw.Y += sink;
                    ToggleSwitch.Draw(g, sw, row.On, false, true);
                    rightEdge = sw.X - Ui.S(10);
                }
                else if (ShowAddButton)
                {
                    Rectangle add = AddRect(top);
                    add.Y += sink;
                    DrawAddButton(g, add, row.Added);
                    rightEdge = add.X - Ui.S(10);
                }
                else if (!string.IsNullOrEmpty(row.Right))
                {
                    Font rightFont = ThemeFonts.SemiBold(12);
                    Size size = TextRenderer.MeasureText(row.Right, rightFont, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                    TextRenderer.DrawText(g, row.Right, rightFont, new Rectangle(rightEdge - size.Width, top + (RowPx - size.Height) / 2 + sink, size.Width, size.Height),
                        ThemeColors.Primary, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                    rightEdge -= size.Width + Ui.S(10);
                }

                int textWidth = Math.Max(Ui.S(40), rightEdge - textLeft);
                string subtitle = row.Subtitle ?? row.Poi.TypeLabel ?? row.Poi.Address ?? "";
                TextRenderer.DrawText(g, row.Poi.Name ?? "", ThemeFonts.SemiBold(14), new Rectangle(textLeft, top + Ui.S(11) + sink, textWidth, Ui.S(20)),
                    dim ? ThemeColors.Slate400 : ThemeColors.Slate800, flags);
                TextRenderer.DrawText(g, subtitle, ThemeFonts.Regular(12), new Rectangle(textLeft, top + Ui.S(33) + sink, textWidth, Ui.S(18)),
                    dim ? ThemeColors.Slate300 : ThemeColors.Slate500, flags);

                if (i < rows.Count - 1)
                {
                    using (var pen = new Pen(ThemeColors.Slate100)) g.DrawLine(pen, textLeft, top + RowPx - 1, rightLimit - Ui.S(14), top + RowPx - 1);
                }
            }

            if (NeedsScrollBar)
            {
                using (GraphicsPath thumb = Ui.RoundedRect(ThumbRect(), Ui.S(3)))
                using (var brush = new SolidBrush(dragging ? ThemeColors.Slate400 : ThemeColors.Slate300))
                {
                    g.FillPath(brush, thumb);
                }
            }
        }

        // A round "+" (outlined) or a tick (solid) for a place that is already in the itinerary.
        private static void DrawAddButton(Graphics g, Rectangle r, bool added)
        {
            if (added)
            {
                using (var brush = new SolidBrush(ThemeColors.Primary)) g.FillEllipse(brush, r);
            }
            else
            {
                using (var brush = new SolidBrush(ThemeColors.Sky50)) g.FillEllipse(brush, r);
                using (var pen = new Pen(ThemeColors.Primary, Math.Max(1.5f, Ui.S(1.5f)))) g.DrawEllipse(pen, r.X + 0.75f, r.Y + 0.75f, r.Width - 1.5f, r.Height - 1.5f);
            }
            int px = (int)(r.Width * 0.58);
            Bitmap glyph = IconFont.Get(added ? "check" : "add", px, added ? Color.White : ThemeColors.Primary);
            g.DrawImage(glyph, r.X + (r.Width - px) / 2, r.Y + (r.Height - px) / 2, px, px);
        }

        // The round badge of the group; faded when the row is switched off.
        private static void DrawBadge(Graphics g, PoiCategory category, Rectangle target, bool dim)
        {
            Bitmap badge = PoiMarkerIcons.Badge(category, target.Width);
            if (!dim)
            {
                g.DrawImage(badge, target);
                return;
            }
            var matrix = new ColorMatrix(new[]
            {
                new[] { 0.35f, 0.35f, 0.35f, 0f, 0f },
                new[] { 0.35f, 0.35f, 0.35f, 0f, 0f },
                new[] { 0.35f, 0.35f, 0.35f, 0f, 0f },
                new[] { 0f, 0f, 0f, 0.55f, 0f },
                new[] { 0.35f, 0.35f, 0.35f, 0f, 1f },
            });
            using (var attributes = new ImageAttributes())
            {
                attributes.SetColorMatrix(matrix);
                g.DrawImage(badge, target, 0, 0, badge.Width, badge.Height, GraphicsUnit.Pixel, attributes);
            }
        }
    }
}
