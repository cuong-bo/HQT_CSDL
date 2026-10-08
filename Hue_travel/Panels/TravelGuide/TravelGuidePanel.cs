using System;
using Hue_travel.Theme;

namespace Hue_travel.Panels.TravelGuide
{
    public partial class TravelGuidePanel : FloatingPanelForm
    {
        public TravelGuidePanel()
        {
            InitializeComponent();
        }

        private void header_CloseClicked(object sender, EventArgs e) => RaiseCloseRequested();
    }
}
