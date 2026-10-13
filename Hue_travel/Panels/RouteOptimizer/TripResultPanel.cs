using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Hue_travel.MapEngine;
using Hue_travel.Models;
using Hue_travel.Services.Routing;
using Hue_travel.Theme;

namespace Hue_travel.Panels.RouteOptimizer
{
    /// <summary>
    /// "Kết quả tìm đường": a wide panel centred at the bottom of the map that opens when the route form has computed a trip. It
    /// shows the distance and travel time, the legs side by side and the car controls; "Dừng lại" clears everything the search
    /// put on the map and in the form.
    /// </summary>
    public sealed class TripResultPanel : FloatingPanelForm
    {
        // ------------------------------------------------------------------ the legs of a computed trip, side by side

        private sealed class LegStrip : Control
        {
            private const int CardWidth = 214;
            private const int CardGap = 10;
            private readonly PressFeedback press;
            private IReadOnlyList<TripLeg> legs = new TripLeg[0];
            private int active = -1;
            private int hover = -1;
            private int scroll;

            public event EventHandler<int> LegClicked;

            public LegStrip()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                BackColor = Color.White;
                Cursor = Cursors.Hand;
                press = new PressFeedback(this);
            }

            private int Step => Ui.S(CardWidth + CardGap);

            private int MaxScroll => Math.Max(0, legs.Count * Step - Ui.S(CardGap) - Width);

            public void SetLegs(IReadOnlyList<TripLeg> value)
            {
                legs = value ?? new TripLeg[0];
                active = -1;
                scroll = 0;
                Invalidate();
            }

            public void SetActive(int index)
            {
                if (active == index) return;
                active = index;
                if (index >= 0)
                {
                    int left = index * Step, right = left + Ui.S(CardWidth);
                    if (left < scroll) scroll = left;
                    else if (right > scroll + Width) scroll = right - Width;
                    scroll = Math.Max(0, Math.Min(MaxScroll, scroll));
                }
                Invalidate();
            }

            private Rectangle CardRect(int i) => new Rectangle(i * Step - scroll, 0, Ui.S(CardWidth), Height - Ui.S(10));

            private int CardAt(Point p)
            {
                for (int i = 0; i < legs.Count; i++) if (CardRect(i).Contains(p)) return i;
                return -1;
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                int card = CardAt(e.Location);
                if (card != hover) { hover = card; Invalidate(); }
                base.OnMouseMove(e);
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                hover = -1;
                press.Up();
                Invalidate();
                base.OnMouseLeave(e);
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                int card = CardAt(e.Location);
                if (e.Button == MouseButtons.Left && card >= 0) press.Down(card, e.Location);
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
                int card = CardAt(e.Location);
                if (e.Button == MouseButtons.Left && card >= 0) LegClicked?.Invoke(this, card);
            }

            protected override void OnMouseWheel(MouseEventArgs e)
            {
                base.OnMouseWheel(e);
                scroll = Math.Max(0, Math.Min(MaxScroll, scroll - Math.Sign(e.Delta) * Step));
                Invalidate();
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

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(BackColor);
                const TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;
                for (int i = 0; i < legs.Count; i++)
                {
                    TripLeg leg = legs[i];
                    Rectangle card = CardRect(i);
                    if (card.Right < 0 || card.Left > Width) continue;
                    Color color = RoutePalette.ColorOf(i);
                    Color rest = i == active ? ThemeColors.Sky50 : (i == hover ? ThemeColors.Slate50 : Color.White);
                    Color fill = press.Fill(i, rest, ThemeColors.Slate200);
                    using (GraphicsPath path = Ui.RoundedRect(card, Ui.S(12)))
                    {
                        using (var brush = new SolidBrush(fill)) g.FillPath(brush, path);
                        using (var pen = new Pen(i == active ? color : ThemeColors.Slate200, i == active ? 2f : 1f)) g.DrawPath(pen, path);
                    }

                    int pad = Ui.S(14), badge = Ui.S(30);
                    var badgeRect = new Rectangle(card.X + pad, card.Y + pad, badge, badge);
                    using (var brush = new SolidBrush(color)) g.FillEllipse(brush, badgeRect);
                    TextRenderer.DrawText(g, (i + 1).ToString(), ThemeFonts.Bold(13), badgeRect, Color.White,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                    TextRenderer.DrawText(g, "Chặng " + (i + 1), ThemeFonts.SemiBold(13), new Rectangle(badgeRect.Right + Ui.S(10), badgeRect.Y, card.Width - badge - pad * 2 - Ui.S(10), badge), ThemeColors.Slate700,
                        flags | TextFormatFlags.VerticalCenter);

                    int textWidth = card.Width - pad * 2;
                    int y = badgeRect.Bottom + Ui.S(10);
                    TextRenderer.DrawText(g, "Từ  " + ShortName(leg.FromName), ThemeFonts.Regular(12), new Rectangle(card.X + pad, y, textWidth, Ui.S(18)), ThemeColors.Slate500, flags);
                    TextRenderer.DrawText(g, "Đến  " + ShortName(leg.ToName), ThemeFonts.SemiBold(12), new Rectangle(card.X + pad, y + Ui.S(20), textWidth, Ui.S(18)), ThemeColors.Slate800, flags);
                    TextRenderer.DrawText(g, FormatKm(leg.Meters / 1000) + "  ·  " + FormatMinutes((int)Math.Round(leg.Seconds / 60)), ThemeFonts.Bold(13),
                        new Rectangle(card.X + pad, y + Ui.S(46), textWidth, Ui.S(20)), ThemeColors.Sky800, flags);
                }

                if (MaxScroll > 0)
                {
                    int track = Width - Ui.S(8), thumb = Math.Max(Ui.S(30), (int)((long)track * Width / (MaxScroll + Width)));
                    int x = Ui.S(4) + (int)((long)(track - thumb) * scroll / MaxScroll);
                    using (GraphicsPath bar = Ui.RoundedRect(new Rectangle(x, Height - Ui.S(5), thumb, Ui.S(4)), Ui.S(2)))
                    using (var brush = new SolidBrush(ThemeColors.Slate300)) g.FillPath(brush, bar);
                }
            }
        }

        // ------------------------------------------------------------------ the totals of a computed trip

        private sealed class SummaryCard : Control
        {
            public string Distance = "", Duration = "", Extra = "", Note = "";
            public bool Estimate;

            public SummaryCard()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                BackColor = Color.White;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(BackColor);
                var card = new Rectangle(0, 0, Width - 1, Height - 1);
                using (GraphicsPath path = Ui.RoundedRect(card, Ui.S(12)))
                {
                    using (var fill = new SolidBrush(ThemeColors.Sky50)) g.FillPath(fill, path);
                    using (var pen = new Pen(ThemeColors.Sky200)) g.DrawPath(pen, path);
                }
                int half = Width / 2;
                DrawFigure(g, "Quãng đường", Distance, new Rectangle(Ui.S(16), Ui.S(12), half - Ui.S(16), Ui.S(46)));
                DrawFigure(g, "Thời gian di chuyển", Duration, new Rectangle(half, Ui.S(12), half - Ui.S(10), Ui.S(46)));
                TextRenderer.DrawText(g, Extra, ThemeFonts.SemiBold(12), new Rectangle(Ui.S(16), Ui.S(66), Width - Ui.S(32), Ui.S(20)), ThemeColors.Slate700,
                    TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
                TextRenderer.DrawText(g, Note, ThemeFonts.Regular(12), new Rectangle(Ui.S(16), Ui.S(90), Width - Ui.S(32), Height - Ui.S(96)),
                    Estimate ? ThemeColors.Amber700 : ThemeColors.Slate600, TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            }

            private static void DrawFigure(Graphics g, string caption, string value, Rectangle area)
            {
                TextRenderer.DrawText(g, caption, ThemeFonts.Regular(11), new Rectangle(area.X, area.Y, area.Width, Ui.S(16)), ThemeColors.Slate500,
                    TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
                TextRenderer.DrawText(g, value, ThemeFonts.Bold(18), new Rectangle(area.X, area.Y + Ui.S(18), area.Width, Ui.S(28)), ThemeColors.Sky800,
                    TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
            }
        }

        private readonly PanelHeader header = new PanelHeader { IconName = "route", Text = "Kết quả tìm đường", Prominent = true };
        private readonly Panel body = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        private readonly SummaryCard summary = new SummaryCard();
        private readonly LegStrip legStrip = new LegStrip();
        private readonly Label lblLegsHint = new Label { AutoSize = false, Text = "Bấm một chặng để xem trên bản đồ; lăn chuột để cuộn các chặng.", Font = ThemeFonts.Regular(12), ForeColor = ThemeColors.Slate500, TextAlign = ContentAlignment.MiddleLeft };
        private readonly IconButton btnPlay = new IconButton();
        private readonly IconButton btnReplay = new IconButton();
        private readonly IconButton btnFit = new IconButton();
        private readonly IconButton btnSave = new IconButton();
        private readonly IconButton btnPrint = new IconButton();
        private readonly IconButton btnStop = new IconButton();
        private TripPlan plan;
        private bool carPlaying;

        /// <summary>Raised when "Dừng lại" is pressed: the host clears the map and the route form.</summary>
        public event EventHandler StopRequested;

        public event EventHandler<int> LegSelected;
        public event EventHandler PlayRequested;
        public event EventHandler PauseRequested;
        public event EventHandler ReplayRequested;
        public event EventHandler FitRequested;

        /// <summary>The design size of the panel (96-DPI pixels): wide and low, to sit under the map.</summary>
        public const int DesignWidth = 1000;
        public const int DesignHeight = 262;

        public TripResultPanel()
        {
            Size = new Size(DesignWidth, DesignHeight);
            header.CloseClicked += (s, e) => StopRequested?.Invoke(this, EventArgs.Empty);   // closing the result ends the search

            Style(btnPlay, "Tạm dừng", "pause");
            Style(btnReplay, "Chạy lại", "refresh");
            Style(btnFit, "Toàn tuyến", "route");
            Style(btnSave, "Lưu", "save_alt");
            Style(btnPrint, "In", "print");
            btnStop.AutoWidth = false;
            btnStop.Text = "Dừng lại và xoá kết quả";
            btnStop.IconName = "close";
            btnStop.IconSize = 18;
            btnStop.Gap = 8;
            btnStop.Font = ThemeFonts.SemiBold(14);
            btnStop.Radius = 12;
            btnStop.ForeColor = Color.White;
            btnStop.IconColor = Color.White;
            btnStop.FillColor = Color.FromArgb(220, 38, 38);
            btnStop.HoverFillColor = Color.FromArgb(185, 28, 28);
            btnStop.Elevated = true;

            foreach (Control c in new Control[] { summary, lblLegsHint, legStrip, btnPlay, btnReplay, btnFit, btnSave, btnPrint, btnStop }) body.Controls.Add(c);
            Controls.Add(body);
            Controls.Add(header);

            legStrip.LegClicked += (s, i) => { LegSelected?.Invoke(this, i); legStrip.SetActive(i); };
            btnPlay.Click += (s, e) => { if (carPlaying) PauseRequested?.Invoke(this, EventArgs.Empty); else PlayRequested?.Invoke(this, EventArgs.Empty); };
            btnReplay.Click += (s, e) => ReplayRequested?.Invoke(this, EventArgs.Empty);
            btnFit.Click += (s, e) => FitRequested?.Invoke(this, EventArgs.Empty);
            btnSave.Click += (s, e) => SaveItinerary();
            btnPrint.Click += (s, e) => PrintItinerary();
            btnStop.Click += (s, e) => StopRequested?.Invoke(this, EventArgs.Empty);
        }

        private static void Style(IconButton b, string text, string icon)
        {
            b.AutoWidth = false;
            b.Text = text;
            b.IconName = icon;
            b.IconSize = 16;
            b.Gap = 6;
            b.HorizontalPadding = 8;
            b.Font = ThemeFonts.Medium(13);
            b.Radius = 10;
            b.ForeColor = ThemeColors.Slate700;
            b.IconColor = ThemeColors.Primary;
            b.FillColor = Color.White;
            b.HoverFillColor = ThemeColors.Slate100;
            b.BorderColor = ThemeColors.Slate300;
        }

        /// <summary>Fills the panel with a computed trip.</summary>
        public void ShowTrip(TripPlan trip)
        {
            plan = trip;
            summary.Distance = FormatKm(trip.TotalMeters / 1000);
            summary.Duration = FormatMinutes((int)Math.Round(trip.TotalSeconds / 60));
            summary.Estimate = trip.IsEstimate;
            summary.Extra = ShortName(trip.StartName) + "  →  " + trip.Stops.Count + " điểm đến";
            int saved = (int)Math.Round(trip.SavedSeconds / 60);
            summary.Note = trip.IsEstimate
                ? "Ước tính theo đường chim bay (chưa có dữ liệu đường bộ)."
                : saved > 0 ? "Tiết kiệm " + saved + " phút so với thứ tự đã chọn." : "Tính theo đường bộ, đúng thứ tự bạn đã sắp xếp.";
            summary.Invalidate();
            legStrip.SetLegs(trip.Legs);
            SetCarPlaying(true);
            LayoutAll();
        }

        public void SetCarPlaying(bool playing)
        {
            carPlaying = playing;
            btnPlay.Text = playing ? "Tạm dừng" : "Chạy xe";
            btnPlay.IconName = playing ? "pause" : "play_arrow";
        }

        public void SetActiveLeg(int leg) => legStrip.SetActive(leg);

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutAll();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible) LayoutAll();
        }

        // Left: the totals. Middle: the legs side by side. Right: the car controls and "Dừng lại".
        private void LayoutAll()
        {
            if (body == null || body.ClientSize.Width <= 0 || body.ClientSize.Height <= 0) return;
            int pad = Ui.S(16), gap = Ui.S(10);
            int width = body.ClientSize.Width, height = body.ClientSize.Height;
            int summaryWidth = Ui.S(250), controlsWidth = Ui.S(264);

            summary.SetBounds(pad, pad, summaryWidth, height - pad * 2);

            int cx = width - pad - controlsWidth;
            int half = (controlsWidth - gap) / 2, small = (half - gap) / 2, row = Ui.S(40);
            int y = pad;
            btnPlay.SetBounds(cx, y, half, row);
            btnReplay.SetBounds(cx + half + gap, y, half, row); y += row + gap;
            btnFit.SetBounds(cx, y, half, row);
            btnSave.SetBounds(cx + half + gap, y, small, row);
            btnPrint.SetBounds(cx + half + gap + small + gap, y, half - small - gap, row); y += row + gap;
            btnStop.SetBounds(cx, y, controlsWidth, Ui.S(46));

            int mx = pad + summaryWidth + pad;
            int mw = Math.Max(Ui.S(200), cx - pad - mx);
            lblLegsHint.SetBounds(mx, pad - Ui.S(4), mw, Ui.S(22));
            legStrip.SetBounds(mx, pad + Ui.S(22), mw, height - pad * 2 - Ui.S(22));
        }

        // ------------------------------------------------------------------ saving and printing

        private string BuildItinerary()
        {
            var text = new StringBuilder();
            text.AppendLine("LỘ TRÌNH THAM QUAN - CỐ ĐÔ GIS");
            text.AppendLine(new string('=', 40));
            if (plan == null) return text.ToString();
            text.AppendLine("Điểm bắt đầu: " + plan.StartName);
            text.AppendLine();
            text.AppendLine("Các điểm tham quan:");
            for (int i = 0; i < plan.Stops.Count; i++)
            {
                text.AppendLine((i + 1) + ". " + plan.Stops[i].Name);
                text.AppendLine("   " + PoiMarkerIcons.Describe(plan.Stops[i]));
            }
            text.AppendLine();
            text.AppendLine("Tổng quãng đường: " + summary.Distance);
            text.AppendLine("Thời gian dự kiến: " + summary.Duration);
            text.AppendLine();
            text.AppendLine("Chi tiết các chặng di chuyển:");
            for (int i = 0; i < plan.Legs.Count; i++)
            {
                TripLeg leg = plan.Legs[i];
                text.AppendLine("Chặng " + (i + 1) + ": " + ShortName(leg.FromName) + " → " + ShortName(leg.ToName) + " - " + FormatKm(leg.Meters / 1000) + " (khoảng " + (int)Math.Round(leg.Seconds / 60) + " phút)");
            }
            text.AppendLine();
            text.AppendLine(plan.IsEstimate ? "(Quãng đường ước tính theo đường chim bay nhân hệ số 1,3; thời gian tính ở tốc độ trung bình 25 km/h.)" : "(Quãng đường và thời gian tính theo đường bộ thật.)");
            return text.ToString();
        }

        private void SaveItinerary()
        {
            using (var dialog = new SaveFileDialog { Filter = "Tệp văn bản (*.txt)|*.txt", FileName = "LoTrinhThamQuan.txt", Title = "Lưu lịch trình vào máy" })
            {
                if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
                try
                {
                    File.WriteAllText(dialog.FileName, BuildItinerary(), new UTF8Encoding(true));
                    Notify("Đã lưu lịch trình: " + Path.GetFileName(dialog.FileName), ToastKind.Success);
                }
                catch (IOException ex)
                {
                    Notify("Không lưu được tệp: " + ex.Message, ToastKind.Error);
                }
                catch (UnauthorizedAccessException)
                {
                    Notify("Không có quyền ghi vào vị trí đã chọn", ToastKind.Error);
                }
            }
        }

        private void PrintItinerary()
        {
            string itineraryText = BuildItinerary();
            using (var document = new PrintDocument())
            using (var preview = new PrintPreviewDialog { Document = document, Width = 900, Height = 700 })
            using (var font = new Font("Segoe UI", 11f))
            {
                document.PrintPage += (s, e) => e.Graphics.DrawString(itineraryText, font, Brushes.Black, e.MarginBounds);
                preview.ShowDialog(FindForm());
            }
        }

        // "Ga Huế (Đường Bùi Thị Xuân)" -> "Ga Huế"
        private static string ShortName(string name) => string.IsNullOrEmpty(name) ? "" : Regex.Replace(name, @"\s*\(.*\)\s*$", "");

        private static string FormatKm(double km) => km.ToString("0.0", CultureInfo.InvariantCulture) + " km";

        private static string FormatMinutes(int minutes) =>
            minutes >= 60 ? (minutes / 60) + " giờ " + (minutes % 60) + " phút" : minutes + " phút";
    }
}
