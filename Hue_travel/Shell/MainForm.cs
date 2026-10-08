using System.Linq;
using System.Windows.Forms;
using Hue_travel.Models;
using Hue_travel.SampleData;

namespace Hue_travel.Shell
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            WireEvents();
            RestoreFloatingOrder();
        }

        // Floating panels are added before the docked bars; lift them above the map in a stable order.
        private void RestoreFloatingOrder()
        {
            baseMapSwitcherPopup.BringToFront();
            layerControlPanel.BringToFront();
            searchPoiPanel.BringToFront();
            routeOptimizerPanel.BringToFront();
            travelGuidePanel.BringToFront();
            poiIdentifyCard.BringToFront();
        }

        private void WireEvents()
        {
            titleBar.MinimizeClicked += (s, e) => WindowState = FormWindowState.Minimized;
            titleBar.MaximizeClicked += (s, e) =>
                WindowState = WindowState == FormWindowState.Maximized
                    ? FormWindowState.Normal
                    : FormWindowState.Maximized;
            titleBar.CloseClicked += (s, e) => Close();
            titleBar.AuthClicked += (s, e) => ToggleAuth();

            ribbonBar.LayerControlToggled += (s, e) => TogglePanel(layerControlPanel);
            ribbonBar.RouteOptimizerToggled += (s, e) => TogglePanel(routeOptimizerPanel);
            ribbonBar.SearchPoiToggled += (s, e) => TogglePanel(searchPoiPanel);
            ribbonBar.TravelGuideToggled += (s, e) => TogglePanel(travelGuidePanel);
            ribbonBar.ZoomInClicked += (s, e) => mapHost.ZoomIn();
            ribbonBar.ZoomOutClicked += (s, e) => mapHost.ZoomOut();
            ribbonBar.ZoomFullClicked += (s, e) => mapHost.ZoomToFull();
            ribbonBar.MeasureToggled += (s, e) => mapHost.SetMeasureMode(!mapHost.IsMeasuring);
            ribbonBar.BaseMapSwitcherToggled += (s, e) => TogglePanel(baseMapSwitcherPopup);

            categoryRail.CategorySelected += (s, category) => TogglePanel(searchPoiPanel, forceVisible: true);

            layerControlPanel.CloseRequested += (s, e) => layerControlPanel.Visible = false;
            routeOptimizerPanel.CloseRequested += (s, e) => routeOptimizerPanel.Visible = false;
            searchPoiPanel.CloseRequested += (s, e) => searchPoiPanel.Visible = false;
            travelGuidePanel.CloseRequested += (s, e) => travelGuidePanel.Visible = false;
            baseMapSwitcherPopup.CloseRequested += (s, e) => baseMapSwitcherPopup.Visible = false;
            authModal.CloseRequested += (s, e) => authModal.Visible = false;
            registerPanel.CloseRequested += (s, e) => registerPanel.Visible = false;
            forgotPasswordPanel.CloseRequested += (s, e) => forgotPasswordPanel.Visible = false;
            authModal.RegisterRequested += (s, e) => ShowAuthCard(registerPanel);
            authModal.ForgotPasswordRequested += (s, e) => ShowAuthCard(forgotPasswordPanel);
            registerPanel.LoginRequested += (s, e) => ShowAuthCard(authModal);
            forgotPasswordPanel.BackToLoginRequested += (s, e) => ShowAuthCard(authModal);

            searchPoiPanel.PoiSelected += (s, poi) => ShowPoiCard(poi);
            searchPoiPanel.ShowOnMapRequested += (s, poi) => mapHost.ZoomToPin(poi.Id);
            layerControlPanel.LayerToggled += (s, e) => mapHost.SetGroupVisible(e.Key, e.Value);
            mapHost.PinClicked += (s, key) =>
            {
                Poi poi = SamplePois.All.FirstOrDefault(p => p.Id == key);
                if (poi != null) ShowPoiCard(poi);
            };

            baseMapSwitcherPopup.BaseMapSelected += (s, mapId) =>
            {
                mapHost.SetBaseMap(mapId);
                baseMapSwitcherPopup.Visible = false;
            };
            mapHost.BaseMapRequested += (s, mapId) => baseMapSwitcherPopup.MarkSelected(mapId);
        }

        private void TogglePanel(Control panel, bool forceVisible = false)
        {
            panel.Visible = forceVisible || !panel.Visible;
            if (panel.Visible) panel.BringToFront();
        }

        // The auth button toggles whichever auth card is open (login by default).
        private void ToggleAuth()
        {
            bool anyOpen = authModal.Visible || registerPanel.Visible || forgotPasswordPanel.Visible;
            if (anyOpen)
            {
                authModal.Visible = registerPanel.Visible = forgotPasswordPanel.Visible = false;
                return;
            }
            ShowAuthCard(authModal);
        }

        private void ShowAuthCard(Control card)
        {
            authModal.Visible = card == authModal;
            registerPanel.Visible = card == registerPanel;
            forgotPasswordPanel.Visible = card == forgotPasswordPanel;
            card.BringToFront();
        }

        private void ShowPoiCard(Poi poi)
        {
            poiIdentifyCard.ShowPoi(poi);
        }
    }
}
