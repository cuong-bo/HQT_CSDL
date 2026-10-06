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

        public RibbonBar()
        {
            InitializeComponent();
        }
    }
}
