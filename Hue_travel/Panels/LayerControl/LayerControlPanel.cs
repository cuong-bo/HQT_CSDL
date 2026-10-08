using System;
using Hue_travel.Theme;

namespace Hue_travel.Panels.LayerControl
{
    public partial class LayerControlPanel : FloatingPanelForm
    {
        public LayerControlPanel()
        {
            InitializeComponent();
        }

        private void header_CloseClicked(object sender, EventArgs e) => RaiseCloseRequested();

        private void sliderBrightness_ValueChanged(object sender, EventArgs e)
        {
            lblBrightnessValue.Text = sliderBrightness.Value + "%";
        }
    }
}
