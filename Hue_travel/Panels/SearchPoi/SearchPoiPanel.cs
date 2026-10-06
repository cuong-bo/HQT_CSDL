using System;
using System.Windows.Forms;
using Hue_travel.Models;
using Hue_travel.SampleData;

namespace Hue_travel.Panels.SearchPoi
{
    public partial class SearchPoiPanel : UserControl
    {
        public event EventHandler<Poi> PoiSelected;

        private System.Collections.Generic.IReadOnlyList<Poi> currentResults;

        public SearchPoiPanel()
        {
            InitializeComponent();
            RefreshResults();
        }

        private void RefreshResults()
        {
            currentResults = PoiSearchFilter.Apply(SamplePois.All, txtKeyword.Text, null);

            lstResults.Items.Clear();
            foreach (var poi in currentResults)
            {
                lstResults.Items.Add(poi.Name);
            }

            lblResultCount.Text = $"Kết quả tìm kiếm: {currentResults.Count} địa điểm";
        }

        private void btnClose_Click(object sender, EventArgs e) => Hide();

        private void txtKeyword_TextChanged(object sender, EventArgs e) => RefreshResults();

        private void lstResults_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstResults.SelectedIndex < 0 || currentResults == null) return;
            PoiSelected?.Invoke(this, currentResults[lstResults.SelectedIndex]);
        }
    }
}
