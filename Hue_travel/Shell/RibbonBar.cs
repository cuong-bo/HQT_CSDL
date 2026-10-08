using System;
using System.Windows.Forms;

namespace Hue_travel.Shell
{
    public partial class RibbonBar : UserControl
    {
        public event EventHandler LayerControlToggled;
        public event EventHandler RouteOptimizerToggled;
        public event EventHandler SearchPoiToggled;
        public event EventHandler TravelGuideToggled;
        public event EventHandler ZoomInClicked;
        public event EventHandler ZoomOutClicked;
        public event EventHandler BaseMapSwitcherToggled;
        public event EventHandler ZoomFullClicked;
        public event EventHandler MeasureToggled;

        public RibbonBar()
        {
            InitializeComponent();
        }

        private void btnZoomFull_Click(object sender, EventArgs e) => ZoomFullClicked?.Invoke(this, EventArgs.Empty);

        private void btnMeasure_Click(object sender, EventArgs e) => MeasureToggled?.Invoke(this, EventArgs.Empty);

        private void btnLayerControl_Click(object sender, EventArgs e) => LayerControlToggled?.Invoke(this, EventArgs.Empty);

        private void btnRouteOptimizer_Click(object sender, EventArgs e) => RouteOptimizerToggled?.Invoke(this, EventArgs.Empty);

        private void btnSearchPoi_Click(object sender, EventArgs e) => SearchPoiToggled?.Invoke(this, EventArgs.Empty);

        private void btnTravelGuide_Click(object sender, EventArgs e) => TravelGuideToggled?.Invoke(this, EventArgs.Empty);

        private void btnZoomIn_Click(object sender, EventArgs e) => ZoomInClicked?.Invoke(this, EventArgs.Empty);

        private void btnZoomOut_Click(object sender, EventArgs e) => ZoomOutClicked?.Invoke(this, EventArgs.Empty);

        private void btnBaseMap_Click(object sender, EventArgs e) => BaseMapSwitcherToggled?.Invoke(this, EventArgs.Empty);
    }
}
