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

        private void RefreshSelectionSummary()
        {
            var selected = SamplePois.All
                .Where((poi, index) => index < clbDestinations.CheckedIndices.Count &&
                                        clbDestinations.CheckedIndices.Contains(index))
                .ToList();

            var summary = RouteSelection.BuildOrderedSummary(selected);
            lblSelectedCount.Text = $"Đã chọn {summary.Count} điểm";
        }
    }
}
