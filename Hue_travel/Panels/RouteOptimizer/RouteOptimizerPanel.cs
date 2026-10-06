using System;
using System.Linq;
using System.Windows.Forms;
using Hue_travel.SampleData;

namespace Hue_travel.Panels.RouteOptimizer
{
    public partial class RouteOptimizerPanel : UserControl
    {
        public RouteOptimizerPanel()
        {
            InitializeComponent();

            foreach (var poi in SamplePois.All)
            {
                cboStartPoint.Items.Add(poi.Name);
                clbDestinations.Items.Add(poi.Name);
            }
            if (cboStartPoint.Items.Count > 0) cboStartPoint.SelectedIndex = 0;

            RefreshSelectionSummary();
        }

        private void btnClose_Click(object sender, EventArgs e) => Hide();

        private void clbDestinations_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            BeginInvoke((Action)RefreshSelectionSummary);
        }

        private void RefreshSelectionSummary()
        {
            var selected = SamplePois.All
                .Where((poi, index) => clbDestinations.GetItemChecked(index))
                .ToList();

            var summary = RouteSelection.BuildOrderedSummary(selected);
            lblSelectedCount.Text = $"Đã chọn {summary.Count} điểm";
        }
    }
}
