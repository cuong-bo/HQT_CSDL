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
                captured.ShowOnMapClicked += (s, e) => PoiSelected?.Invoke(this, poi);
            }
            RefreshResults();
        }

        private void header_CloseClicked(object sender, EventArgs e) => RaiseCloseRequested();

        private void txtKeyword_TextChanged(object sender, EventArgs e) => RefreshResults();

        private void lblClear_Click(object sender, EventArgs e) => txtKeyword.Clear();

        /// <summary>Filters the result cards by the keyword (diacritic-insensitive) and re-stacks the visible ones.</summary>
        private void RefreshResults()
        {
            IReadOnlyList<Poi> matches = PoiSearchFilter.Apply(cards.Select(c => c.Value), txtKeyword.Text, null);
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
