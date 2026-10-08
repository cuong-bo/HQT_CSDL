using System;
using System.Collections.Generic;
using System.Linq;
using Hue_travel.Models;
using Hue_travel.SampleData;
using Hue_travel.Theme;

namespace Hue_travel.Panels.SearchPoi
{
    public partial class SearchPoiPanel : FloatingPanelForm
    {
        private const int CardTop = 112;
        private const int CardPitch = 84;

        public event EventHandler<Poi> PoiSelected;

        /// <summary>Raised by a result card's "Bản đồ" button: the host should zoom the map to the POI.</summary>
        public event EventHandler<Poi> ShowOnMapRequested;

        private PoiCategory? category;

        private readonly List<KeyValuePair<PoiResultCard, Poi>> cards = new List<KeyValuePair<PoiResultCard, Poi>>();

        public SearchPoiPanel()
        {
            InitializeComponent();

            foreach (PoiResultCard card in new[] { cardMuRoi, cardBaTuyet, cardCam })
            {
                PoiResultCard captured = card;
                Poi poi = SamplePois.All.FirstOrDefault(p => p.Name == captured.Text) ?? new Poi
                {
                    Id = captured.Text,
                    Name = captured.Text,
                    Category = PoiCategory.Food,
                    Address = captured.Address,
                    Rating = 0,
                };
                cards.Add(new KeyValuePair<PoiResultCard, Poi>(captured, poi));
                captured.Click += (s, e) => PoiSelected?.Invoke(this, poi);
                captured.ShowOnMapClicked += (s, e) =>
                {
                    ShowOnMapRequested?.Invoke(this, poi);
                    PoiSelected?.Invoke(this, poi);
                };
            }
            chipAll.Click += (s, e) => SetCategory(null);
            chipHeritage.Click += (s, e) => SetCategory(PoiCategory.Heritage);
            chipFood.Click += (s, e) => SetCategory(PoiCategory.Food);
            chipStay.Click += (s, e) => SetCategory(PoiCategory.Stay);
            ApplyChipStyles();
            RefreshResults();
        }

        /// <summary>Filters the results by category (null = all) and highlights the matching chip.</summary>
        public void SetCategory(PoiCategory? newCategory)
        {
            category = newCategory;
            ApplyChipStyles();
            RefreshResults();
        }

        // Active chip: sky-100 / primary / semibold; others slate-100 / medium. Widths follow the text, so re-flow left to right.
        private void ApplyChipStyles()
        {
            var chips = new[]
            {
                new KeyValuePair<IconButton, PoiCategory?>(chipAll, null),
                new KeyValuePair<IconButton, PoiCategory?>(chipHeritage, PoiCategory.Heritage),
                new KeyValuePair<IconButton, PoiCategory?>(chipFood, PoiCategory.Food),
                new KeyValuePair<IconButton, PoiCategory?>(chipStay, PoiCategory.Stay),
            };
            int x = 12;
            foreach (KeyValuePair<IconButton, PoiCategory?> pair in chips)
            {
                IconButton chip = pair.Key;
                bool active = pair.Value == category;
                chip.ForeColor = active ? ThemeColors.Primary : ThemeColors.Slate600;
                chip.FillColor = active ? ThemeColors.Sky100 : ThemeColors.Slate100;
                chip.HoverFillColor = active ? ThemeColors.Sky100 : ThemeColors.Slate200;
                chip.BorderColor = active ? ThemeColors.Sky200 : ThemeColors.Slate200;
                chip.Font = active ? ThemeFonts.SemiBold(11) : ThemeFonts.Medium(11);
                chip.Left = x;
                x += chip.Width + 6;
            }
        }

        private void header_CloseClicked(object sender, EventArgs e) => RaiseCloseRequested();

        private void txtKeyword_TextChanged(object sender, EventArgs e) => RefreshResults();

        private void lblClear_Click(object sender, EventArgs e)
        {
            txtKeyword.Clear();
            SetCategory(null);
        }

        /// <summary>Filters the result cards by the keyword (diacritic-insensitive) and re-stacks the visible ones.</summary>
        private void RefreshResults()
        {
            IReadOnlyList<Poi> matches = PoiSearchFilter.Apply(cards.Select(c => c.Value), txtKeyword.Text, category);
            int row = 0;
            foreach (KeyValuePair<PoiResultCard, Poi> pair in cards)
            {
                bool isMatch = matches.Contains(pair.Value);
                pair.Key.Visible = isMatch;
                if (isMatch) pair.Key.Top = CardTop + CardPitch * row++;
            }
            lblResultCount.Text = $"Kết quả tìm kiếm: **{matches.Count} địa điểm**";
        }
    }
}
