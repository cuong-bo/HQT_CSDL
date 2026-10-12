using System;
using System.Linq;
using System.Windows.Forms;
using Hue_travel.MapEngine;
using Hue_travel.Models;
using Hue_travel.SampleData;
using Hue_travel.Services;
using Hue_travel.Services.Area;
using Hue_travel.Services.Data;
using Hue_travel.Services.Routing;
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
                searchPoiPanel, routeOptimizerPanel, travelGuidePanel, poiIdentifyCard,
                authModal, registerPanel,
            };
            foreach (FloatingPanelForm panel in panels) panel.MessageRaised += (s, e) => toast.ShowMessage(e.Text, e.Kind);
        }

        private readonly FloatingPanelForm[] panels;
        private readonly WeatherUpdater weatherUpdater = new WeatherUpdater(new OpenMeteoWeatherService());
        private readonly IAreaLocator areaLocator = new DemoAreaLocator();
        private bool showDegrees = true; // degrees are the familiar format; click the read-out for UTM metres
        private MapCursorEventArgs lastCursor;

        // Only the main window opens at start-up; the panels appear when the user asks for them (ribbon, rail, search).
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            mapHost.LoadPois(PoiCatalog.All);
            if (PoiCatalog.LastError != null)
                toast.ShowMessage("Không đọc được dữ liệu địa điểm, đang dùng dữ liệu mẫu: " + PoiCatalog.LastError, ToastKind.Error);
            mapHost.ZoomToFull();
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

            ribbonBar.RouteOptimizerToggled += (s, e) => TogglePanel(routeOptimizerPanel);
            ribbonBar.SearchPoiToggled += (s, e) => TogglePanel(searchPoiPanel);
            ribbonBar.TravelGuideToggled += (s, e) => TogglePanel(travelGuidePanel);
            ribbonBar.ZoomInClicked += (s, e) => mapHost.ZoomIn();
            ribbonBar.ZoomOutClicked += (s, e) => mapHost.ZoomOut();
            ribbonBar.ZoomFullClicked += (s, e) => mapHost.ZoomToFull();
            ribbonBar.MeasureToggled += (s, e) => mapHost.ToggleMeasure(MeasureKind.Distance);
            ribbonBar.MeasureAreaToggled += (s, e) => mapHost.ToggleMeasure(MeasureKind.Area);


            // The left rail shows only the picked kind of object on the map.
            categoryRail.CategorySelected += (s, category) => mapHost.ShowOnlyCategory(category);

            routeOptimizerPanel.CloseRequested += (s, e) => routeOptimizerPanel.Visible = false;
            searchPoiPanel.CloseRequested += (s, e) => searchPoiPanel.Visible = false;
            travelGuidePanel.CloseRequested += (s, e) => travelGuidePanel.Visible = false;
            authModal.CloseRequested += (s, e) => authModal.Visible = false;
            registerPanel.CloseRequested += (s, e) => registerPanel.Visible = false;
            authModal.RegisterRequested += (s, e) => ShowAuthCard(registerPanel);
            registerPanel.LoginRequested += (s, e) => ShowAuthCard(authModal);

            searchPoiPanel.PoiSelected += (s, poi) => ShowPoiCard(poi);
            mapHost.PoiClicked += (s, poi) => ShowPoiCard(poi);
            searchPoiPanel.ShowOnMapRequested += (s, poi) =>
            {
                GeoPoint place;
                if (poi.X != 0 || poi.Y != 0) mapHost.ZoomToProjected(poi.X, poi.Y);
                else if (poi.HasLocation) mapHost.ZoomTo(poi.Longitude, poi.Latitude);
                else if (DemoPlaces.TryGetPoi(poi.Id, out place)) mapHost.ZoomTo(place.Longitude, place.Latitude);
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
                statusBar.SetArea(areaLocator.Locate(view.CenterLongitude, view.CenterLatitude));
            };

        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (titleBar != null) titleBar.SetMaximized(WindowState == FormWindowState.Maximized);
        }

        private void TogglePanel(Control panel, bool forceVisible = false)
        {
            panel.Visible = forceVisible || !panel.Visible;
            if (panel.Visible) panel.BringToFront();
        }

        // The auth button toggles whichever auth card is open (login by default).
        private void ToggleAuth()
        {
            bool anyOpen = authModal.Visible || registerPanel.Visible;
            if (anyOpen)
            {
                authModal.Visible = registerPanel.Visible = false;
                return;
            }
            ShowAuthCard(authModal);
        }

        private void ShowAuthCard(Control card)
        {
            authModal.Visible = card == authModal;
            registerPanel.Visible = card == registerPanel;
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
