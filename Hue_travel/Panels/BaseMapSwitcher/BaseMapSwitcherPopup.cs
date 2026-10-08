using System;
using Hue_travel.Theme;

namespace Hue_travel.Panels.BaseMapSwitcher
{
    public partial class BaseMapSwitcherPopup : FloatingPanelForm
    {
        public event EventHandler<string> BaseMapSelected;

        public BaseMapSwitcherPopup()
        {
            InitializeComponent();
        }

        private void btnSatellite_Click(object sender, EventArgs e) => Select("satellite");

        private void btnRoadmap_Click(object sender, EventArgs e) => Select("roadmap");

        private void btnHybrid_Click(object sender, EventArgs e) => Select("hybrid");

        private void btnTerrain_Click(object sender, EventArgs e) => Select("terrain");

        private void sliderOpacity_ValueChanged(object sender, EventArgs e)
        {
            lblOpacityValue.Text = sliderOpacity.Value + "%";
        }

        /// <summary>Marks a tile as the current base map without raising <see cref="BaseMapSelected"/>.</summary>
        public void MarkSelected(string mapId)
        {
            tileSatellite.Selected = mapId == "satellite";
            tileRoadmap.Selected = mapId == "roadmap";
            tileHybrid.Selected = mapId == "hybrid";
            tileTerrain.Selected = mapId == "terrain";
        }

        private void Select(string mapId)
        {
            MarkSelected(mapId);
            BaseMapSelected?.Invoke(this, mapId);
        }
    }
}
