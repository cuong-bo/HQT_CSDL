using System.Windows.Forms;
using Hue_travel.Models;

namespace Hue_travel.Shell
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            WireEvents();
        }

        private void WireEvents()
        {
            titleBar.MinimizeClicked += (s, e) => WindowState = FormWindowState.Minimized;
            titleBar.MaximizeClicked += (s, e) =>
                WindowState = WindowState == FormWindowState.Maximized
                    ? FormWindowState.Normal
                    : FormWindowState.Maximized;
            titleBar.CloseClicked += (s, e) => Close();
            titleBar.AuthClicked += (s, e) => TogglePanel(authModal);

            ribbonBar.LayerControlToggled += (s, e) => TogglePanel(layerControlPanel);
            ribbonBar.RouteOptimizerToggled += (s, e) => TogglePanel(routeOptimizerPanel);
            ribbonBar.SearchPoiToggled += (s, e) => TogglePanel(searchPoiPanel);
            ribbonBar.TravelGuideToggled += (s, e) => TogglePanel(travelGuidePanel);
            ribbonBar.ZoomInClicked += (s, e) => mapHost.ZoomIn();
            ribbonBar.ZoomOutClicked += (s, e) => mapHost.ZoomOut();
            ribbonBar.BaseMapSwitcherToggled += (s, e) => TogglePanel(baseMapSwitcherPopup);

            categoryRail.CategorySelected += (s, category) => TogglePanel(searchPoiPanel, forceVisible: true);

            layerControlPanel.CloseRequested += (s, e) => layerControlPanel.Visible = false;
            travelGuidePanel.CloseRequested += (s, e) => travelGuidePanel.Visible = false;
            authModal.CloseRequested += (s, e) => authModal.Visible = false;

            searchPoiPanel.PoiSelected += (s, poi) => ShowPoiCard(poi);
            baseMapSwitcherPopup.BaseMapSelected += (s, mapId) => baseMapSwitcherPopup.Visible = false;
        }

        private void TogglePanel(Control panel, bool forceVisible = false)
        {
            panel.Visible = forceVisible || !panel.Visible;
            if (panel.Visible) panel.BringToFront();
        }

        private void ShowPoiCard(Poi poi)
        {
            poiIdentifyCard.ShowPoi(poi);
        }
    }
}
