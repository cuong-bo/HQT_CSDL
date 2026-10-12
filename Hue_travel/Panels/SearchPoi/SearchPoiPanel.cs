using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Hue_travel.Models;
using Hue_travel.Services.Data;
using Hue_travel.Theme;

namespace Hue_travel.Panels.SearchPoi
{
    public partial class SearchPoiPanel : FloatingPanelForm
    {
        // Design-pixel layout of the part that is arranged in code (scaled to the screen with Ui.S).
        private const int ChipsTop = 54;
        private const int ChipGap = 6;
        private const int SideMargin = 12;
        private const int CardTop = 138;
        private const int CardPitch = 84;

        public event EventHandler<Poi> PoiSelected;

        /// <summary>Raised by a result card's "Bản đồ" button: the host should zoom the map to the POI.</summary>
        public event EventHandler<Poi> ShowOnMapRequested;

        // Empty = every category; otherwise only the ticked groups.
        private readonly HashSet<PoiCategory> categories = new HashSet<PoiCategory>();

        private readonly List<KeyValuePair<IconButton, PoiCategory?>> chips;

        private readonly List<KeyValuePair<PoiResultCard, Poi>> cards = new List<KeyValuePair<PoiResultCard, Poi>>();

        public SearchPoiPanel()
        {
            InitializeComponent();

            chips = new List<KeyValuePair<IconButton, PoiCategory?>>
            {
                new KeyValuePair<IconButton, PoiCategory?>(chipAll, null),
                new KeyValuePair<IconButton, PoiCategory?>(chipHeritage, PoiCategory.Heritage),
                new KeyValuePair<IconButton, PoiCategory?>(chipScenery, PoiCategory.Scenery),
                new KeyValuePair<IconButton, PoiCategory?>(chipFood, PoiCategory.Food),
                new KeyValuePair<IconButton, PoiCategory?>(chipStay, PoiCategory.Stay),
                new KeyValuePair<IconButton, PoiCategory?>(chipService, PoiCategory.Service),
                new KeyValuePair<IconButton, PoiCategory?>(chipTransport, PoiCategory.Transport),
            };

            foreach (Poi poi in PoiCatalog.All) AddCard(poi);
            foreach (KeyValuePair<IconButton, PoiCategory?> pair in chips)
            {
                PoiCategory? category = pair.Value;
                pair.Key.Click += (s, e) => ToggleCategory(category);
            }
            ApplyChipStyles();
            RefreshResults();
        }

        /// <summary>Rebuilds the result cards from the current places (the server's list replaced the bundled one).</summary>
        public void ReloadPois()
        {
            foreach (KeyValuePair<PoiResultCard, Poi> pair in cards)
            {
                panelBody.Controls.Remove(pair.Key);
                pair.Key.Dispose();
            }
            cards.Clear();
            foreach (Poi poi in PoiCatalog.All) AddCard(poi);
            RefreshResults();
        }

        private void AddCard(Poi poi)
        {
            var card = new PoiResultCard
            {
                Text = poi.Name,
                Address = poi.Address,
                Rating = poi.Rating > 0 ? "★ " + poi.Rating.ToString("0.0", CultureInfo.InvariantCulture) : string.Empty,
                Distance = DistanceLine(poi),
                Size = new Size(340, 76),   // leaves room for the scroll bar
            };
            card.Click += (s, e) => PoiSelected?.Invoke(this, poi);
            card.ShowOnMapClicked += (s, e) =>
            {
                ShowOnMapRequested?.Invoke(this, poi);
                PoiSelected?.Invoke(this, poi);
            };
            panelBody.Controls.Add(card);
            cards.Add(new KeyValuePair<PoiResultCard, Poi>(card, poi));
        }

        // The real data has no distance from the user, so its cards show the type; the samples keep "Cách bạn 1.2 km".
        private static string DistanceLine(Poi poi)
        {
            if (!string.IsNullOrEmpty(poi.TypeLabel)) return poi.TypeLabel;
            string distance = poi.DistanceKm > 0
                ? "Cách bạn " + poi.DistanceKm.ToString("0.0", CultureInfo.InvariantCulture) + " km"
                : "Điểm xuất phát";
            string extra = poi.Category == PoiCategory.Food ? poi.TicketPrice : string.Empty;
            return string.IsNullOrEmpty(extra) ? distance : distance + " • " + extra;
        }

        /// <summary>"Tất cả" clears the choice; any other chip is switched on or off, so several groups can be combined.</summary>
        private void ToggleCategory(PoiCategory? category)
        {
            if (category == null) categories.Clear();
            else if (!categories.Remove(category.Value)) categories.Add(category.Value);
            ApplyChipStyles();
            RefreshResults();
        }

        // Active chip: sky-100 / primary / semibold; others slate-100 / medium.
        private void ApplyChipStyles()
        {
            foreach (KeyValuePair<IconButton, PoiCategory?> pair in chips)
            {
                IconButton chip = pair.Key;
                bool active = pair.Value == null ? categories.Count == 0 : categories.Contains(pair.Value.Value);
                chip.ForeColor = active ? ThemeColors.Primary : ThemeColors.Slate600;
                chip.FillColor = active ? ThemeColors.Sky100 : ThemeColors.Slate100;
                chip.HoverFillColor = active ? ThemeColors.Sky100 : ThemeColors.Slate200;
                chip.BorderColor = active ? ThemeColors.Sky200 : ThemeColors.Slate200;
                chip.Font = active ? ThemeFonts.SemiBold(11) : ThemeFonts.Medium(11);
            }
            ArrangeChips();
        }

        // Chip widths follow their text, so flow them left to right and wrap onto a second row.
        private void ArrangeChips()
        {
            if (chips == null) return;
            int left = Ui.S(SideMargin);
            int right = panelBody.ClientSize.Width - Ui.S(SideMargin);
            int x = left, y = Ui.S(ChipsTop);
            foreach (KeyValuePair<IconButton, PoiCategory?> pair in chips)
            {
                IconButton chip = pair.Key;
                if (x > left && x + chip.Width > right)
                {
                    x = left;
                    y += chip.Height + Ui.S(ChipGap);
                }
                chip.Location = new Point(x, y);
                x += chip.Width + Ui.S(ChipGap);
            }
        }

        // Scaling a form grows its children after the constructor ran, so arrange again once that is done.
        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            ArrangeChips();
            RefreshResults();
        }

        private void header_CloseClicked(object sender, EventArgs e) => RaiseCloseRequested();

        private void txtKeyword_TextChanged(object sender, EventArgs e) => RefreshResults();

        private void lblClear_Click(object sender, EventArgs e)
        {
            txtKeyword.Clear();
            categories.Clear();
            ApplyChipStyles();
            RefreshResults();
        }

        /// <summary>Filters the result cards by keyword (diacritic-insensitive) and groups, and re-stacks the visible ones.</summary>
        private void RefreshResults()
        {
            if (cards.Count == 0) return;
            IReadOnlyList<Poi> matches = PoiSearchFilter.Apply(cards.Select(c => c.Value), txtKeyword.Text, categories);
            int row = 0;
            foreach (KeyValuePair<PoiResultCard, Poi> pair in cards)
            {
                bool isMatch = matches.Contains(pair.Value);
                pair.Key.Visible = isMatch;
                if (!isMatch) continue;
                pair.Key.Tinted = row == 0;   // the first result is drawn on slate-50, as in the mockup
                pair.Key.Top = Ui.S(CardTop) + Ui.S(CardPitch) * row;
                pair.Key.Left = Ui.S(SideMargin);
                row++;
            }
            lblResultCount.Text = $"Kết quả tìm kiếm: **{matches.Count} địa điểm**";
        }
    }
}
