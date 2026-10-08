using System;
using System.Collections.Generic;
using Hue_travel.Theme;

namespace Hue_travel.Panels.LayerControl
{
    public partial class LayerControlPanel : FloatingPanelForm
    {
        /// <summary>Raised when a layer row is ticked or unticked: (map layer group, new visibility).</summary>
        public event EventHandler<KeyValuePair<string, bool>> LayerToggled;

        public LayerControlPanel()
        {
            InitializeComponent();
            rowHeritage.CheckedChanged += (s, e) => RaiseLayerToggled("heritage", rowHeritage.Checked);
            rowFood.CheckedChanged += (s, e) => RaiseLayerToggled("food", rowFood.Checked);
            rowStay.CheckedChanged += (s, e) => RaiseLayerToggled("stay", rowStay.Checked);
            rowRoads.CheckedChanged += (s, e) => RaiseLayerToggled("roads", rowRoads.Checked);
            rowRiver.CheckedChanged += (s, e) => RaiseLayerToggled("river", rowRiver.Checked);
            rowPiers.CheckedChanged += (s, e) => RaiseLayerToggled("piers", rowPiers.Checked);
        }

        /// <summary>
        /// Shows only the given category group (null = every group). The road layer is a base layer
        /// and is left as the user set it.
        /// </summary>
        public void SetOnlyGroup(string group)
        {
            rowHeritage.Checked = group == null || group == "heritage";
            rowFood.Checked = group == null || group == "food";
            rowStay.Checked = group == null || group == "stay";
            rowRiver.Checked = group == null || group == "river";
            rowPiers.Checked = group == null || group == "piers";
        }

        private void RaiseLayerToggled(string group, bool visible) =>
            LayerToggled?.Invoke(this, new KeyValuePair<string, bool>(group, visible));

        private void header_CloseClicked(object sender, EventArgs e) => RaiseCloseRequested();
    }
}
