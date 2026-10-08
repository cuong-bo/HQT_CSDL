using System;
using System.Linq;
using System.Windows.Forms;
using Hue_travel.MapEngine;
using Hue_travel.Models;
using Hue_travel.SampleData;
using Hue_travel.Services;
using Hue_travel.Services.Area;
using Hue_travel.Services.Weather;
using Hue_travel.Theme;

namespace Hue_travel.Shell
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            WireEvents();
            panels = new FloatingPanelForm[]
            {
                layerControlPanel, searchPoiPanel, routeOptimizerPanel, travelGuidePanel, poiIdentifyCard,
                baseMapSwitcherPopup, authModal, registerPanel, forgotPasswordPanel,
            };
            foreach (FloatingPanelForm panel in panels) panel.MessageRaised += (s, e) => toast.ShowMessage(e.Text, e.Kind);
        }

        private readonly FloatingPanelForm[] panels;
        private readonly WeatherUpdater weatherUpdater = new WeatherUpdater(new OpenMeteoWeatherService());
        private readonly IAreaLocator areaLocator = new DemoAreaLocator();
        private bool showDegrees;
        private MapCursorEventArgs lastCursor;

        // The default layout opens once the window is on screen (the panels are owned by it).
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            layerControlPanel.Visible = true;
            searchPoiPanel.Visible = true;
            routeOptimizerPanel.Visible = true;
            travelGuidePanel.Visible = true;
            poiIdentifyCard.Visible = true;
            weatherUpdater.Start();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            weatherUpdater.Dispose();
            base.OnFormClosed(e);
        }

        private void WireEvents()
        {
            titleBar.MinimizeClicked += (s, e) => WindowState = FormWindowState.Minimized;
            titleBar.MaximizeClicked += (s, e) =>
            {
                if (WindowState == FormWindowState.Maximized)
                {
                    WindowState = FormWindowState.Normal;
                    return;
                }
                // A borderless window would otherwise cover the taskbar when maximised.
                MaximizedBounds = Screen.FromControl(this).WorkingArea;
                WindowState = FormWindowState.Maximized;
            };
            titleBar.CloseClicked += (s, e) => Close();
            titleBar.AuthClicked += (s, e) => ToggleAuth();

            ribbonBar.LayerControlToggled += (s, e) => TogglePanel(layerControlPanel);
            ribbonBar.RouteOptimizerToggled += (s, e) => TogglePanel(routeOptimizerPanel);
            ribbonBar.SearchPoiToggled += (s, e) => TogglePanel(searchPoiPanel);
            ribbonBar.TravelGuideToggled += (s, e) => TogglePanel(travelGuidePanel);
            ribbonBar.ZoomInClicked += (s, e) => mapHost.ZoomIn();
            ribbonBar.ZoomOutClicked += (s, e) => mapHost.ZoomOut();
            ribbonBar.ZoomFullClicked += (s, e) => mapHost.ZoomToFull();
            ribbonBar.MeasureToggled += (s, e) => mapHost.ToggleMeasure(MeasureKind.Distance);
            ribbonBar.MeasureAreaToggled += (s, e) => mapHost.ToggleMeasure(MeasureKind.Area);
            ribbonBar.BaseMapSwitcherToggled += (s, e) => TogglePanel(baseMapSwitcherPopup);

            categoryRail.CategorySelected += (s, category) =>
            {
                TogglePanel(searchPoiPanel, forceVisible: true);
                searchPoiPanel.SetCategory(category);
                layerControlPanel.SetOnlyGroup(GroupFor(category));
            };

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

            // Status bar: live scale and area from the map view, weather from Open-Meteo (no fixed placeholder text).
            statusBar.SetWeather(weatherUpdater.Text);
            weatherUpdater.TextChanged += (s, text) => statusBar.SetWeather(text);
            statusBar.CoordinatesClicked += (s, e) =>
            {
                showDegrees = !showDegrees;
                ShowCursorCoordinates();
            };
            mapHost.CursorMoved += (s, cursor) =>
            {
                lastCursor = cursor;
                ShowCursorCoordinates();
            };
            mapHost.ViewChanged += (s, view) =>
            {
                statusBar.SetScale(MapScale.Format(view.ScaleDenominator));
                statusBar.SetArea(areaLocator.Locate(view.CenterX, view.CenterY));
            };

            baseMapSwitcherPopup.BaseMapSelected += (s, mapId) =>
            {
                mapHost.SetBaseMap(mapId);
                baseMapSwitcherPopup.Visible = false;
            };
            mapHost.BaseMapRequested += (s, mapId) => baseMapSwitcherPopup.MarkSelected(mapId);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (titleBar != null) titleBar.SetMaximized(WindowState == FormWindowState.Maximized);
        }

        // Map layer group shown for a rail category (null = every group).
        private static string GroupFor(PoiCategory? category)
        {
            switch (category)
            {
                case PoiCategory.Heritage: return "heritage";
                case PoiCategory.Food: return "food";
                case PoiCategory.Stay: return "stay";
                case PoiCategory.River: return "river";
                case PoiCategory.Transport: return "piers";
                default: return null;
            }
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

        private void ShowCursorCoordinates()
        {
            if (lastCursor == null) return;
            statusBar.SetCoordinates(showDegrees
                ? CoordinateFormatter.FormatDegrees(lastCursor.Longitude, lastCursor.Latitude)
                : CoordinateFormatter.FormatUtm(lastCursor.Easting, lastCursor.Northing));
        }

        private void ShowPoiCard(Poi poi)
        {
            poiIdentifyCard.ShowPoi(poi);
        }
    }
}
