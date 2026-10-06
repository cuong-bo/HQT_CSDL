using System;
using System.Windows.Forms;

namespace Hue_travel.Panels.BaseMapSwitcher
{
    public partial class BaseMapSwitcherPopup : Form
    {
        public event EventHandler<string> BaseMapSelected;

        public BaseMapSwitcherPopup()
        {
            InitializeComponent();
        }

        private void btnStandard_Click(object sender, EventArgs e) => BaseMapSelected?.Invoke(this, "standard");

        private void btnSatellite_Click(object sender, EventArgs e) => BaseMapSelected?.Invoke(this, "satellite");

        private void btnNight_Click(object sender, EventArgs e) => BaseMapSelected?.Invoke(this, "night");
    }
}
