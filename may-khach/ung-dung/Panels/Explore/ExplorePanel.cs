using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Hue_travel.MapEngine;
using Hue_travel.Models;
using Hue_travel.Panels.Shared;
using Hue_travel.Services.Data;
using Hue_travel.Theme;

namespace Hue_travel.Panels.Explore
{
    /// <summary>
    /// "Khám phá": what is around the part of the map being looked at. It lists the places inside the map window, the nearest
    /// to its centre first, with the distance, and counts them by group (a count chip filters the list). It follows the map
    /// as it is moved or zoomed, so it never repeats the search box (which finds a place by name).
    /// </summary>
    public sealed class ExplorePanel : FloatingPanelForm
    {
        private const int MaxRows = 120;

        /// <summary>A row of count chips: "Tất cả 24", "Di tích 5", ...; the chosen one is solid blue.</summary>
        private sealed class CountChips : Control
        {
            private sealed class Chip { public string Text; public PoiCategory? Category; public Rectangle Bounds; }

            private readonly PressFeedback press;
            private List<Chip> chips = new List<Chip>();
            private int selected;
            private int hover = -1;

            public event EventHandler<PoiCategory?> Changed;

            public CountChips()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                BackColor = ThemeColors.Slate50;
                Cursor = Cursors.Hand;
                press = new PressFeedback(this);
            }

            public PoiCategory? Selected => selected < chips.Count ? chips[selected].Category : null;

            public void SetCounts(int total, IDictionary<PoiCategory, int> perCategory, PoiCategory? keep)
            {
                var next = new List<Chip> { new Chip { Text = "Tất cả " + total, Category = null } };
                foreach (PoiCategory c in Enum.GetValues(typeof(PoiCategory)))
                {
                    int n;
                    if (perCategory.TryGetValue(c, out n) && n > 0) next.Add(new Chip { Text = PoiMarkerIcons.NameOf(c) + " " + n, Category = c });
                }
                chips = next;
                selected = Math.Max(0, chips.FindIndex(c => c.Category == keep));
                LayoutChips();
                Invalidate();
            }

            private void LayoutChips()
            {
                int x = Ui.S(16), y = Ui.S(6), height = Ui.S(30), gap = Ui.S(8), right = Width - Ui.S(16);
                foreach (Chip chip in chips)
                {
                    int w = TextRenderer.MeasureText(chip.Text, ThemeFonts.SemiBold(12), Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Width + Ui.S(26);
                    if (x + w > right && x > Ui.S(16)) { x = Ui.S(16); y += height + gap; }
                    chip.Bounds = new Rectangle(x, y, w, height);
                    x += w + gap;
                }
            }

            public int WantedHeight => chips.Count == 0 ? Ui.S(44) : chips.Max(c => c.Bounds.Bottom) + Ui.S(8);

            protected override void OnResize(EventArgs e) { base.OnResize(e); LayoutChips(); }

            private int IndexAt(Point p) => chips.FindIndex(c => c.Bounds.Contains(p));

            protected override void OnMouseMove(MouseEventArgs e) { int i = IndexAt(e.Location); if (i != hover) { hover = i; Invalidate(); } base.OnMouseMove(e); }
            protected override void OnMouseLeave(EventArgs e) { hover = -1; press.Up(); Invalidate(); base.OnMouseLeave(e); }
            protected override void OnMouseUp(MouseEventArgs e) { press.Up(); base.OnMouseUp(e); }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                int i = IndexAt(e.Location);
                if (e.Button == MouseButtons.Left && i >= 0) press.Down(i, e.Location);
                base.OnMouseDown(e);
            }

            protected override void OnMouseClick(MouseEventArgs e)
            {
                base.OnMouseClick(e);
                int i = IndexAt(e.Location);
                if (e.Button != MouseButtons.Left || i < 0) return;
                selected = i;
                Invalidate();
                Changed?.Invoke(this, chips[i].Category);
            }

            protected override void Dispose(bool disposing) { if (disposing) press.Dispose(); base.Dispose(disposing); }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(BackColor);
                for (int i = 0; i < chips.Count; i++)
                {
                    Chip chip = chips[i];
                    bool on = i == selected;
                    var bounds = new RectangleF(chip.Bounds.X + 0.5f, chip.Bounds.Y + 0.5f, chip.Bounds.Width - 1f, chip.Bounds.Height - 1f);
                    Color rest = on ? ThemeColors.Primary : (i == hover ? ThemeColors.Slate100 : Color.White);
                    Color fill = press.Fill(i, rest, on ? ThemeColors.Sky800 : ThemeColors.Slate200);
                    using (GraphicsPath path = Ui.RoundedRect(bounds, bounds.Height / 2f))
                    {
                        using (var brush = new SolidBrush(fill)) g.FillPath(brush, path);
                        press.PaintRipple(g, i, path, on ? Color.White : ThemeColors.Sky700, on ? 90 : 60);
                        using (var pen = new Pen(on ? ThemeColors.Primary : ThemeColors.Slate300)) g.DrawPath(pen, path);
                    }
                    TextRenderer.DrawText(g, chip.Text, ThemeFonts.SemiBold(12), new Rectangle(chip.Bounds.X, chip.Bounds.Y + press.SinkFor(i), chip.Bounds.Width, chip.Bounds.Height),
                        on ? Color.White : ThemeColors.Slate700,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
                }
            }
        }

        private readonly PanelHeader header = new PanelHeader { IconName = "search", Text = "Khám phá", Prominent = true };
        private readonly Panel top = new Panel { Dock = DockStyle.Top, Height = 92, BackColor = ThemeColors.Slate50 };
        private readonly Label lblTitle = new Label { AutoSize = false, Font = ThemeFonts.Bold(18), ForeColor = ThemeColors.Slate800, TextAlign = ContentAlignment.MiddleLeft, Text = "Quanh khu vực đang xem", Location = new Point(16, 10), Size = new Size(350, 30) };
        private readonly Label lblArea = new Label { AutoSize = false, Font = ThemeFonts.SemiBold(13), ForeColor = ThemeColors.Primary, TextAlign = ContentAlignment.MiddleLeft, Location = new Point(16, 40), Size = new Size(350, 22) };
        private readonly Label lblHint = new Label { AutoSize = false, Font = ThemeFonts.Regular(12), ForeColor = ThemeColors.Slate500, TextAlign = ContentAlignment.MiddleLeft, Text = "Các điểm trong khung bản đồ, gần tâm nhất xếp trước", Location = new Point(16, 62), Size = new Size(350, 20) };
        private readonly CountChips chips = new CountChips { Dock = DockStyle.Top, Height = 48 };
        private readonly PoiRowList list = new PoiRowList { Dock = DockStyle.Fill };
        private double xMin, yMin, xMax, yMax;
        private bool hasView;
        private PoiCategory? filter;

        /// <summary>Raised when a place in the list is clicked.</summary>
        public event EventHandler<Poi> PoiSelected;

        public ExplorePanel()
        {
            Size = new Size(384, 640);
            header.CloseClicked += (s, e) => RaiseCloseRequested();
            top.Controls.Add(lblTitle);
            top.Controls.Add(lblArea);
            top.Controls.Add(lblHint);
            chips.Changed += (s, category) => { filter = category; Rebuild(false); };
            list.RowClicked += (s, row) => PoiSelected?.Invoke(this, row.Poi);
            list.EmptyText = "Không có địa điểm nào trong khung bản đồ. Hãy thu nhỏ hoặc kéo bản đồ sang khu vực khác.";

            Controls.Add(list);
            Controls.Add(chips);
            Controls.Add(top);
            Controls.Add(header);
            PoiCatalog.Changed += (s, e) => { if (!IsDisposed) Rebuild(true); };
        }

        /// <summary>The name of the area at the map centre (district / commune), shown under the title.</summary>
        public void SetArea(string text)
        {
            lblArea.Text = string.IsNullOrWhiteSpace(text) ? "" : text;
        }

        /// <summary>Tells the panel which part of the map is in view (map metres) and redraws the list.</summary>
        public void UpdateView(double minX, double minY, double maxX, double maxY)
        {
            xMin = minX; yMin = minY; xMax = maxX; yMax = maxY;
            hasView = true;
            Rebuild(true);
        }

        private void Rebuild(bool refreshChips)
        {
            if (!hasView) return;
            double cx = (xMin + xMax) / 2, cy = (yMin + yMax) / 2;
            var inView = PoiCatalog.All
                .Where(p => (p.X != 0 || p.Y != 0) && p.X >= xMin && p.X <= xMax && p.Y >= yMin && p.Y <= yMax)
                .Select(p => new { Poi = p, Meters = Math.Sqrt((p.X - cx) * (p.X - cx) + (p.Y - cy) * (p.Y - cy)) })
                .OrderBy(x => x.Meters).ToList();

            if (refreshChips)
            {
                chips.SetCounts(inView.Count, inView.GroupBy(x => x.Poi.Category).ToDictionary(g => g.Key, g => g.Count()), filter);
                filter = chips.Selected;
                chips.Height = chips.WantedHeight;
            }

            list.SetRows(inView.Where(x => filter == null || x.Poi.Category == filter.Value).Take(MaxRows).Select(x => new PoiRow(x.Poi)
            {
                Right = Distance(x.Meters),
                Subtitle = SubtitleOf(x.Poi),
            }));
        }

        private static string SubtitleOf(Poi poi) =>
            PoiMarkerIcons.Describe(poi);

        private static string Distance(double meters) =>
            meters < 1000 ? ((int)Math.Round(meters / 10.0) * 10) + " m" : (meters / 1000).ToString("0.0", CultureInfo.InvariantCulture) + " km";
    }
}
