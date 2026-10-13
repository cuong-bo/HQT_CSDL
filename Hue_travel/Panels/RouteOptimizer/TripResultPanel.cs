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
    /// "Kết quả tìm đường": a compact bar centred under the map that opens when the route form has computed a trip. A strip of
    /// numbers on top, the legs side by side, the car controls underneath. It can be folded away to a button on the map (see
    /// <see cref="Folded"/>) and opened again; "Dừng lại" clears everything the search put on the map and in the form.
    /// </summary>
    public sealed class TripResultPanel : FloatingPanelForm
    {
        // ------------------------------------------------------------------ the legs of a computed trip, side by side

        private sealed class LegStrip : Control
        {
            private const int CardWidth = 196;
            private const int CardGap = 8;
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

            private Rectangle CardRect(int i) => new Rectangle(i * Step - scroll, 0, Ui.S(CardWidth), Height - (MaxScroll > 0 ? Ui.S(8) : 0));

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
                    using (GraphicsPath path = Ui.RoundedRect(new Rectangle(card.X, card.Y, card.Width - 1, card.Height - 1), Ui.S(12)))
                    {
                        using (var brush = new SolidBrush(fill)) g.FillPath(brush, path);
                        using (var pen = new Pen(i == active ? color : ThemeColors.Slate200, i == active ? 2f : 1f)) g.DrawPath(pen, path);
                    }

                    int pad = Ui.S(12), badge = Ui.S(26);
                    var badgeRect = new Rectangle(card.X + pad, card.Y + pad, badge, badge);
                    using (var brush = new SolidBrush(color)) g.FillEllipse(brush, badgeRect);
                    TextRenderer.DrawText(g, (i + 1).ToString(), ThemeFonts.Bold(12), badgeRect, Color.White,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                    int textLeft = badgeRect.Right + Ui.S(10), textWidth = card.Right - pad - textLeft;
                    TextRenderer.DrawText(g, ShortName(leg.ToName), ThemeFonts.SemiBold(13), new Rectangle(textLeft, badgeRect.Y, textWidth, badge), ThemeColors.Slate800, flags | TextFormatFlags.VerticalCenter);
                    TextRenderer.DrawText(g, FormatKm(leg.Meters / 1000) + "  ·  " + FormatMinutes((int)Math.Round(leg.Seconds / 60)), ThemeFonts.SemiBold(12),
                        new Rectangle(card.X + pad, badgeRect.Bottom + Ui.S(8), card.Width - pad * 2, Ui.S(18)), ThemeColors.Sky800, flags);
                }

                if (MaxScroll > 0)
                {
                    int track = Width - Ui.S(8), thumb = Math.Max(Ui.S(30), (int)((long)track * Width / (MaxScroll + Width)));
                    int x = Ui.S(4) + (int)((long)(track - thumb) * scroll / MaxScroll);
                    using (GraphicsPath bar = Ui.RoundedRect(new Rectangle(x, Height - Ui.S(4), thumb, Ui.S(3)), Ui.S(1)))
                    using (var brush = new SolidBrush(ThemeColors.Slate300)) g.FillPath(brush, bar);
                }
            }
        }

        // ------------------------------------------------------------------ the title row: the totals as two pills and a line of note

        private sealed class TitleRow : Control
        {
            public string Distance = "", Duration = "", Note = "";
            public bool Estimate;

            public TitleRow()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                BackColor = Color.White;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(BackColor);
                int x = 0;
                Bitmap icon = IconFont.Get("route", Ui.S(20), ThemeColors.Primary);
                g.DrawImage(icon, x, (Height - icon.Height) / 2);
                x += icon.Width + Ui.S(8);
                Size title = TextRenderer.MeasureText("Kết quả tìm đường", ThemeFonts.Bold(15), Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                TextRenderer.DrawText(g, "Kết quả tìm đường", ThemeFonts.Bold(15), new Rectangle(x, 0, title.Width, Height), ThemeColors.Slate800,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                x += title.Width + Ui.S(16);
                x += Pill(g, x, "straighten", Distance);
                x += Ui.S(8);
                x += Pill(g, x, "schedule", Duration);
                x += Ui.S(14);
                TextRenderer.DrawText(g, Note, ThemeFonts.Regular(12), new Rectangle(x, 0, Math.Max(0, Width - x), Height), Estimate ? ThemeColors.Amber700 : ThemeColors.Slate500,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
            }

            // A rounded pill with an icon and a figure; returns its width.
            private int Pill(Graphics g, int x, string iconName, string text)
            {
                Font font = ThemeFonts.Bold(13);
                Size size = TextRenderer.MeasureText(text, font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                int iconPx = Ui.S(16), pad = Ui.S(12);
                int width = pad + iconPx + Ui.S(6) + size.Width + pad;
                var rect = new Rectangle(x, (Height - Ui.S(32)) / 2, width, Ui.S(32));
                using (GraphicsPath path = Ui.RoundedRect(rect, rect.Height / 2f))
                using (var fill = new SolidBrush(ThemeColors.Sky50)) g.FillPath(fill, path);
                g.DrawImage(IconFont.Get(iconName, iconPx, ThemeColors.Sky700), rect.X + pad, rect.Y + (rect.Height - iconPx) / 2, iconPx, iconPx);
                TextRenderer.DrawText(g, text, font, new Rectangle(rect.X + pad + iconPx + Ui.S(6), rect.Y, size.Width + 2, rect.Height), ThemeColors.Sky800,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                return width;
            }
        }

        private readonly TitleRow titleRow = new TitleRow();
        private readonly IconButton btnFold = new IconButton();
        private readonly IconButton btnClose = new IconButton();
        private readonly LegStrip legStrip = new LegStrip();
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

        /// <summary>Design size (96-DPI pixels) of the panel: a low bar to sit under the map.</summary>
        public const int DesignWidth = 760;
        public const int DesignHeight = 204;
        /// <summary>Raised when the panel is folded away: the host shows a button on the map that calls <see cref="Unfold"/>.</summary>
        public event EventHandler Folded;

        public TripResultPanel()
        {
            Size = new Size(DesignWidth, DesignHeight);
            BackColor = Color.White;

            Round(btnFold, "expand_more");
            Round(btnClose, "close");
            Style(btnPlay, "Tạm dừng", "pause");
            Style(btnReplay, "Chạy lại", "refresh");
            Style(btnFit, "Toàn tuyến", "route");
            Style(btnSave, "Lưu", "save_alt");
            Style(btnPrint, "In", "print");
            btnStop.AutoWidth = false;
            btnStop.Text = "Dừng lại";
            btnStop.IconName = "close";
            btnStop.IconSize = 16;
            btnStop.Gap = 6;
            btnStop.Font = ThemeFonts.SemiBold(13);
            btnStop.Radius = 10;
            btnStop.ForeColor = Color.White;
            btnStop.IconColor = Color.White;
            btnStop.FillColor = Color.FromArgb(220, 38, 38);
            btnStop.HoverFillColor = Color.FromArgb(185, 28, 28);
            new ToolTip().SetToolTip(btnFold, "Thu nhỏ thành biểu tượng");
            new ToolTip().SetToolTip(btnClose, "Dừng lại và xoá kết quả");

            foreach (Control c in new Control[] { titleRow, btnFold, btnClose, legStrip, btnPlay, btnReplay, btnFit, btnSave, btnPrint, btnStop }) Controls.Add(c);

            btnFold.Click += (s, e) => Fold();
            btnClose.Click += (s, e) => StopRequested?.Invoke(this, EventArgs.Empty);
            legStrip.LegClicked += (s, i) => { LegSelected?.Invoke(this, i); legStrip.SetActive(i); };
            btnPlay.Click += (s, e) => { if (carPlaying) PauseRequested?.Invoke(this, EventArgs.Empty); else PlayRequested?.Invoke(this, EventArgs.Empty); };
            btnReplay.Click += (s, e) => ReplayRequested?.Invoke(this, EventArgs.Empty);
            btnFit.Click += (s, e) => FitRequested?.Invoke(this, EventArgs.Empty);
            btnSave.Click += (s, e) => SaveItinerary();
            btnPrint.Click += (s, e) => PrintItinerary();
            btnStop.Click += (s, e) => StopRequested?.Invoke(this, EventArgs.Empty);
        }

        // A small round icon-only button in the title row.
        private static void Round(IconButton b, string icon)
        {
            b.AutoWidth = false;
            b.IconName = icon;
            b.IconSize = 16;
            b.IconColor = ThemeColors.Slate600;
            b.FillColor = ThemeColors.Slate100;
            b.HoverFillColor = ThemeColors.Slate200;
            b.Radius = 14;
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

        // ------------------------------------------------------------------ folding into the round button

        /// <summary>Hides the panel; the host shows its button on the map.</summary>
        public void Fold()
        {
            Visible = false;
            Folded?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Shows the panel again.</summary>
        public void Unfold() => Visible = true;

        /// <summary>Opens the panel with a new trip.</summary>
        public void Open(TripPlan trip)
        {
            ShowTrip(trip);
            Unfold();
        }

        /// <summary>Closes the panel (after "Dừng lại").</summary>
        public void CloseAll() => Visible = false;

        /// <summary>Fills the panel with a computed trip.</summary>
        public void ShowTrip(TripPlan trip)
        {
            plan = trip;
            titleRow.Distance = FormatKm(trip.TotalMeters / 1000);
            titleRow.Duration = FormatMinutes((int)Math.Round(trip.TotalSeconds / 60));
            titleRow.Estimate = trip.IsEstimate;
            int saved = (int)Math.Round(trip.SavedSeconds / 60);
            titleRow.Note = trip.IsEstimate
                ? "Ước tính theo đường chim bay (chưa có dữ liệu đường bộ)"
                : ShortName(trip.StartName) + "  →  " + trip.Stops.Count + " điểm đến" + (saved > 0 ? "  ·  tiết kiệm " + saved + " phút" : "");
            titleRow.Invalidate();
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

        // Top: title and totals, the two small buttons at the right. Middle: the legs. Bottom: the car controls, "Dừng lại" at the right.
        private void LayoutAll()
        {
            if (ClientSize.Width <= 0 || ClientSize.Height <= 0) return;
            int pad = Ui.S(18), gap = Ui.S(8);
            int width = ClientSize.Width, height = ClientSize.Height;
            int small = Ui.S(28);

            btnClose.SetBounds(width - pad - small, pad - Ui.S(2), small, small);
            btnFold.SetBounds(width - pad - small * 2 - gap, pad - Ui.S(2), small, small);
            titleRow.SetBounds(pad, pad - Ui.S(2), width - pad * 2 - small * 2 - gap * 2, small + Ui.S(4));

            int rowH = Ui.S(38);
            int bottom = height - pad;
            int controlsY = bottom - rowH;
            int stripY = pad + small + Ui.S(10);
            legStrip.SetBounds(pad, stripY, width - pad * 2, controlsY - Ui.S(10) - stripY);

            int x = pad;
            int w1 = Ui.S(112), w2 = Ui.S(100), w3 = Ui.S(116), w4 = Ui.S(72), w5 = Ui.S(62);
            btnPlay.SetBounds(x, controlsY, w1, rowH); x += w1 + gap;
            btnReplay.SetBounds(x, controlsY, w2, rowH); x += w2 + gap;
            btnFit.SetBounds(x, controlsY, w3, rowH); x += w3 + gap;
            btnSave.SetBounds(x, controlsY, w4, rowH); x += w4 + gap;
            btnPrint.SetBounds(x, controlsY, w5, rowH);
            int stopW = Ui.S(120);
            btnStop.SetBounds(width - pad - stopW, controlsY, stopW, rowH);
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
            text.AppendLine("Tổng quãng đường: " + titleRow.Distance);
            text.AppendLine("Thời gian dự kiến: " + titleRow.Duration);
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
