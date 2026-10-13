using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Hue_travel.Models;
using Hue_travel.Services.Data;
using Hue_travel.Theme;

namespace Hue_travel.Panels.TravelGuide
{
    /// <summary>The tour the visitor chose: the places in visiting order; the first one is where the trip starts.</summary>
    public sealed class TourChosenEventArgs : EventArgs
    {
        public TourChosenEventArgs(string title, IReadOnlyList<Poi> places)
        {
            Title = title;
            Places = places;
        }

        public string Title { get; }
        public IReadOnlyList<Poi> Places { get; }
    }

    /// <summary>
    /// The travel guide: suggested tours (a tour opens its stops and "Dùng tour này" sends it to the route form, which finds
    /// the road route), tips for visiting Huế and useful phone numbers. Built in code like the route form.
    /// </summary>
    public sealed class TravelGuidePanel : FloatingPanelForm
    {
        private readonly PanelHeader header = new PanelHeader { IconName = "menu_book", Text = "Cẩm nang du lịch Huế", Prominent = true };
        private readonly NoAutoScrollPanel body = new NoAutoScrollPanel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White };

        private readonly Label lblToursTitle = Caption("Tour gợi ý");
        private readonly Label lblToursHint = Hint("Chọn một tour để xem các điểm dừng. Bấm \"Dùng tour này\" để máy chủ tìm đường và xe chạy thử trên bản đồ.");
        private readonly TourList tourList = new TourList();
        private readonly Label lblTipsTitle = Caption("Lưu ý khi tham quan");
        private readonly InfoRows tipRows = new InfoRows();
        private readonly Label lblContactsTitle = Caption("Số điện thoại hữu ích");
        private readonly InfoRows contactRows = new InfoRows();
        private readonly List<Control> dividers = new List<Control>();

        /// <summary>Raised when the visitor presses "Dùng tour này": the host opens the route form with the tour in it.</summary>
        public event EventHandler<TourChosenEventArgs> TourChosen;

        public TravelGuidePanel()
        {
            Size = new Size(384, 640);
            header.CloseClicked += (s, e) => RaiseCloseRequested();

            tipRows.SetNotes(GuideContent.Tips);
            contactRows.SetNotes(GuideContent.Contacts);
            foreach (Control c in new Control[] { lblToursTitle, lblToursHint, tourList, lblTipsTitle, tipRows, lblContactsTitle, contactRows }) body.Controls.Add(c);
            for (int i = 0; i < 2; i++)
            {
                var line = new Panel { BackColor = ThemeColors.Slate100, Height = 1 };
                dividers.Add(line);
                body.Controls.Add(line);
            }
            Controls.Add(body);
            Controls.Add(header);

            tourList.HeightChanged += (s, e) => LayoutAll();
            tourList.UseRequested += (s, entry) => TourChosen?.Invoke(this, new TourChosenEventArgs(entry.Tour.Title, entry.Places));
            contactRows.CopyClicked += (s, note) => Copy(note);
            ReloadTours();
            PoiCatalog.Changed += (s, e) => { if (!IsDisposed) { ReloadTours(); LayoutAll(); } };
        }

        // The tours whose places can be found in the current data (a tour needs at least two to be a trip).
        private void ReloadTours()
        {
            Dictionary<string, Poi> byId = PoiCatalog.All.Where(p => p.X != 0 || p.Y != 0).GroupBy(p => p.Id).ToDictionary(g => g.Key, g => g.First());
            var entries = new List<TourEntry>();
            foreach (GuideTour tour in GuideContent.Tours)
            {
                List<Poi> places = tour.PlaceIds.Where(byId.ContainsKey).Select(id => byId[id]).ToList();
                if (places.Count >= 2) entries.Add(new TourEntry { Tour = tour, Places = places });
            }
            tourList.SetTours(entries);
        }

        private void Copy(GuideNote note)
        {
            try
            {
                Clipboard.SetText(note.CopyText);
                Notify("Đã sao chép " + note.CopyText, ToastKind.Success);
            }
            catch (ExternalException)
            {
                Notify("Không sao chép được " + note.CopyText, ToastKind.Error);
            }
        }

        private static Label Caption(string text) =>
            new Label { AutoSize = false, Text = text, Font = ThemeFonts.Bold(15), ForeColor = ThemeColors.Slate800, TextAlign = ContentAlignment.MiddleLeft };

        private static Label Hint(string text) =>
            new Label { AutoSize = false, Text = text, Font = ThemeFonts.Regular(12), ForeColor = ThemeColors.Slate500, TextAlign = ContentAlignment.TopLeft };

        // ------------------------------------------------------------------ layout

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

        // Stacks the sections from the top (device pixels) and sets how far the body scrolls.
        private void LayoutAll()
        {
            if (body == null || dividers.Count < 2 || body.ClientSize.Width <= 0) return;   // the first resizes come before the controls exist
            int offset = body.AutoScrollPosition.Y;
            int pad = Ui.S(16), gap = Ui.S(10);
            int full = Math.Max(Ui.S(240), ClientSize.Width - SystemInformation.VerticalScrollBarWidth);
            int inner = full - pad * 2;
            int y = Ui.S(16);

            lblToursTitle.SetBounds(pad, y + offset, inner, Ui.S(26)); y += Ui.S(26) + Ui.S(4);
            lblToursHint.SetBounds(pad, y + offset, inner, Ui.S(52)); y += Ui.S(52) + gap;
            // the list draws its own side margins, so it spans the full width
            tourList.SetBounds(0, y + offset, full, tourList.WantedHeight); y += tourList.WantedHeight + gap;
            y += Divider(0, pad, inner, y, offset);

            lblTipsTitle.SetBounds(pad, y + offset, inner, Ui.S(26)); y += Ui.S(26) + Ui.S(4);
            int tipsHeight = tipRows.HeightFor(full);
            tipRows.SetBounds(0, y + offset, full, tipsHeight); y += tipsHeight + gap;
            y += Divider(1, pad, inner, y, offset);

            lblContactsTitle.SetBounds(pad, y + offset, inner, Ui.S(26)); y += Ui.S(26) + Ui.S(4);
            int contactsHeight = contactRows.HeightFor(full);
            contactRows.SetBounds(0, y + offset, full, contactsHeight); y += contactsHeight + gap;
            body.AutoScrollMinSize = new Size(0, y + pad);
        }

        // A thin line across the body; returns the space it takes.
        private int Divider(int index, int pad, int inner, int y, int offset)
        {
            Control line = dividers[index];
            line.SetBounds(pad, y + offset, inner, 1);
            line.Visible = true;
            return Ui.S(14);
        }
    }
}
