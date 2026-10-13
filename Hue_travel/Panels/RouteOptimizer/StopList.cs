using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Hue_travel.MapEngine;
using Hue_travel.Models;
using Hue_travel.Theme;

namespace Hue_travel.Panels.RouteOptimizer
{
    /// <summary>
    /// The itinerary: the places to visit in the order they will be visited, each with a numbered badge, up / down buttons to
    /// change the order and a remove button. Drawn at the screen's resolution; the control grows with its rows.
    /// </summary>
    internal sealed class StopList : Control
    {
        private const int RowHeight = 58;
        private const int ButtonSize = 28;

        private enum Part { None, Up, Down, Remove, Row }

        private readonly PressFeedback press;
        private readonly List<Poi> stops = new List<Poi>();
        private int hoverRow = -1;
        private Part hoverPart = Part.None;

        /// <summary>Raised with the row and the direction (-1 up, +1 down) when a move button is pressed.</summary>
        public event EventHandler<KeyValuePair<int, int>> MoveRequested;

        public event EventHandler<int> RemoveRequested;

        public event EventHandler<int> RowClicked;

        public StopList()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Color.White;
            press = new PressFeedback(this);
        }

        public IReadOnlyList<Poi> Stops => stops;

        /// <summary>The height needed for the rows.</summary>
        public int WantedHeight => Math.Max(1, stops.Count) * Ui.S(RowHeight);

        public void SetStops(IEnumerable<Poi> value)
        {
            stops.Clear();
            stops.AddRange(value);
            hoverRow = -1;
            Invalidate();
        }

        private int RowPx => Ui.S(RowHeight);

        private Rectangle ButtonRect(int row, Part part)
        {
            int size = Ui.S(ButtonSize);
            int right = Width - Ui.S(10);
            int slot = part == Part.Remove ? 0 : part == Part.Down ? 1 : 2;
            return new Rectangle(right - size * (slot + 1) - Ui.S(2) * slot, row * RowPx + (RowPx - size) / 2, size, size);
        }

        private Part PartAt(Point p, out int row)
        {
            row = p.Y >= 0 && RowPx > 0 ? p.Y / RowPx : -1;
            if (row < 0 || row >= stops.Count) { row = -1; return Part.None; }
            if (ButtonRect(row, Part.Remove).Contains(p)) return Part.Remove;
            if (row < stops.Count - 1 && ButtonRect(row, Part.Down).Contains(p)) return Part.Down;
            if (row > 0 && ButtonRect(row, Part.Up).Contains(p)) return Part.Up;
            return Part.Row;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int row;
            Part part = PartAt(e.Location, out row);
            if (row != hoverRow || part != hoverPart)
            {
                hoverRow = row;
                hoverPart = part;
                Cursor = part == Part.None ? Cursors.Default : Cursors.Hand;
                Invalidate();
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hoverRow = -1;
            hoverPart = Part.None;
            press.Up();
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            int row;
            Part part = PartAt(e.Location, out row);
            if (e.Button == MouseButtons.Left && part != Part.None) press.Down(row * 10 + (int)part, e.Location);
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
            if (e.Button != MouseButtons.Left) return;
            int row;
            switch (PartAt(e.Location, out row))
            {
                case Part.Up: MoveRequested?.Invoke(this, new KeyValuePair<int, int>(row, -1)); break;
                case Part.Down: MoveRequested?.Invoke(this, new KeyValuePair<int, int>(row, 1)); break;
                case Part.Remove: RemoveRequested?.Invoke(this, row); break;
                case Part.Row: RowClicked?.Invoke(this, row); break;
            }
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
            if (stops.Count == 0)
            {
                TextRenderer.DrawText(g, "Chưa có điểm đến nào. Bấm \"Thêm điểm đến\" bên dưới để chọn từ danh sách địa điểm.",
                    ThemeFonts.Regular(13), new Rectangle(Ui.S(14), 0, Width - Ui.S(28), Height), ThemeColors.Slate500,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
                return;
            }

            const TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;
            for (int i = 0; i < stops.Count; i++)
            {
                Poi poi = stops[i];
                int top = i * RowPx;
                var rowRect = new Rectangle(0, top, Width, RowPx);
                if (hoverRow == i && hoverPart == Part.Row)
                {
                    using (var brush = new SolidBrush(ThemeColors.Slate50)) g.FillRectangle(brush, rowRect);
                }

                // numbered badge in the colour of the leg that leads to the place
                int badge = Ui.S(30);
                var badgeRect = new Rectangle(Ui.S(14), top + (RowPx - badge) / 2, badge, badge);
                using (var brush = new SolidBrush(RoutePalette.ColorOf(i))) g.FillEllipse(brush, badgeRect);
                TextRenderer.DrawText(g, (i + 1).ToString(), ThemeFonts.Bold(13), badgeRect, Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);

                // buttons
                int textRight = Width - Ui.S(10) - Ui.S(ButtonSize) * 3 - Ui.S(4) - Ui.S(8);
                PaintButton(g, i, Part.Remove, "close", true);
                PaintButton(g, i, Part.Down, "expand_more", i < stops.Count - 1);
                PaintButton(g, i, Part.Up, "expand_less", i > 0);

                int textLeft = badgeRect.Right + Ui.S(12);
                int textWidth = Math.Max(Ui.S(40), textRight - textLeft);
                TextRenderer.DrawText(g, poi.Name ?? "", ThemeFonts.SemiBold(14), new Rectangle(textLeft, top + Ui.S(10), textWidth, Ui.S(20)), ThemeColors.Slate800, flags);
                TextRenderer.DrawText(g, PoiMarkerIcons.Describe(poi), ThemeFonts.Regular(12), new Rectangle(textLeft, top + Ui.S(32), textWidth, Ui.S(18)), ThemeColors.Slate500, flags);

                if (i < stops.Count - 1)
                {
                    using (var pen = new Pen(ThemeColors.Slate100)) g.DrawLine(pen, textLeft, top + RowPx - 1, Width - Ui.S(14), top + RowPx - 1);
                }
            }
        }

        private void PaintButton(Graphics g, int row, Part part, string icon, bool enabled)
        {
            Rectangle r = ButtonRect(row, part);
            bool hot = enabled && hoverRow == row && hoverPart == part;
            int id = row * 10 + (int)part;
            if (enabled)
            {
                Color fill = press.Fill(id, hot ? ThemeColors.Slate100 : Color.Transparent, ThemeColors.Slate200);
                if (fill.A > 0) using (var brush = new SolidBrush(fill)) g.FillEllipse(brush, r);
            }
            int px = (int)(r.Width * 0.66);
            Color color = !enabled ? ThemeColors.Slate200 : part == Part.Remove ? (hot ? Color.FromArgb(220, 38, 38) : ThemeColors.Slate400) : (hot ? ThemeColors.Primary : ThemeColors.Slate500);
            g.DrawImage(IconFont.Get(icon, px, color), r.X + (r.Width - px) / 2, r.Y + (r.Height - px) / 2 + (enabled ? press.SinkFor(id) : 0), px, px);
        }
    }
}
