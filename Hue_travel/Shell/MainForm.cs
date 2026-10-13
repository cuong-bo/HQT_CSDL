using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Hue_travel.Account;
using Hue_travel.MapEngine;
using Hue_travel.Models;
using Hue_travel.SampleData;
using Hue_travel.Services;
using Hue_travel.Services.Area;
using Hue_travel.Services.Data;
using Hue_travel.Services.Online;
using Hue_travel.Services.Routing;
using Hue_travel.Services.Weather;
using Hue_travel.Theme;

namespace Hue_travel.Shell
{
    public partial class MainForm : Form
    {
        /// <summary>
        /// The main window, opened once somebody has signed in (or chose to continue offline) on the sign-in screen.
        /// With a server the app is "online": accounts, the server's places, road routes and weather. Without one it works
        /// offline on the data that ships with it.
        /// </summary>
        public MainForm(AppSession session)
        {
            this.session = session;
            api = session.Api;
            auth = session.Auth;
            if (api != null)
            {
                var areaFromServer = new ApiAreaLocator(api, areaLocator);
                areaFromServer.Updated += (s, e) => ShowArea();
                areaLocator = areaFromServer;
            }
            weatherUpdater = new WeatherUpdater(api != null ? (IWeatherService)new FallbackWeatherService(new ApiWeatherService(api), new OpenMeteoWeatherService()) : new OpenMeteoWeatherService());

            InitializeComponent();
            if (api != null) SetUpAccount();
            WireEvents();
            var all = new List<FloatingPanelForm>
            {
                explorePanel, routeOptimizerPanel, travelGuidePanel, poiIdentifyCard, layerPanel, tripResultPanel,
            };
            if (accountPanel != null) all.Add(accountPanel);
            panels = all.ToArray();
            foreach (FloatingPanelForm panel in panels) panel.MessageRaised += (s, e) => toast.ShowMessage(e.Text, e.Kind);
        }

        private readonly FloatingPanelForm[] panels;
        private const int LayerSlot = 4;   // the layer list: in the side slot but not a rail tab
        private readonly System.Windows.Forms.Timer exploreTimer = new System.Windows.Forms.Timer { Interval = 250 };
        private FloatingPanelForm[] sidePanels;   // in NavRail tab order: explore, route, guide, profile (null while offline)
        private readonly AppSession session;
        private readonly WeatherUpdater weatherUpdater;
        private readonly ApiClient api;
        private readonly AuthService auth;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private readonly HashSet<string> favoriteIds = new HashSet<string>();
        private readonly IAreaLocator areaLocator = new DemoAreaLocator();
        private AccountPanel accountPanel;
        private MapViewChangedEventArgs lastView;
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
            if (api != null) StartOnline();
            if (auth != null && auth.MustChangePassword) ShowSidePanel(NavRail.Profile);   // a temporary password must be changed first
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            lifetime.Cancel();
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

            // Left rail: one tab per tool; the tools' panels share the side slot, so opening one closes the others.
            sidePanels = new FloatingPanelForm[] { explorePanel, routeOptimizerPanel, travelGuidePanel, accountPanel, layerPanel };
            navRail.ItemClicked += (s, index) =>
            {
                if (sidePanels[index] == null)
                {
                    toast.ShowMessage("Chưa có địa chỉ máy chủ nên chưa có hồ sơ cá nhân. Ứng dụng đang chạy ngoại tuyến.", ToastKind.Info);
                    return;
                }
                ShowSidePanel(sidePanels[index].Visible ? -1 : index);
            };

            // The chips under the header pick a group: the map shows only that group and its places are listed with switches.
            filterBar.CategorySelected += (s, category) =>
            {
                mapHost.ShowOnlyCategory(category);
                layerPanel.SetCategory(category);
                ShowSidePanel(LayerSlot);
            };
            layerPanel.HiddenChanged += (s, e) => mapHost.SetHiddenPois(layerPanel.HiddenIds);
            layerPanel.PoiSelected += (s, poi) => { ShowOnMap(poi); ShowPoiCard(poi); };

            // The search box lists its results right under itself (no new window).
            titleBar.SearchTextChanged += (s, text) => UpdateSearchResults(text);
            titleBar.SearchFocusGained += (s, e) => UpdateSearchResults(titleBar.SearchText);
            titleBar.SearchFocusLost += (s, e) => searchDropdown.Dismiss();
            titleBar.SearchMoved += (s, delta) => searchDropdown.MoveSelection(delta);
            titleBar.SearchSubmitted += (s, text) =>
            {
                if (!searchDropdown.ChooseSelected()) UpdateSearchResults(text);
            };
            searchDropdown.PoiChosen += (s, poi) =>
            {
                searchDropdown.Dismiss();
                ShowOnMap(poi);
                ShowPoiCard(poi);
            };

            // The Explore panel follows the map: it lists what is in the window, refreshed shortly after the map stops moving.
            exploreTimer.Tick += (s, e) => { exploreTimer.Stop(); RefreshExplore(); };
            explorePanel.PoiSelected += (s, poi) => ShowPoiCard(poi);
            poiIdentifyCard.VisibleChanged += (s, e) => { if (!poiIdentifyCard.Visible) mapHost.ClearHighlight(); };

            for (int i = 0; i < sidePanels.Length; i++)
            {
                int index = i;
                if (sidePanels[i] == null) continue;
                sidePanels[i].CloseRequested += (s, e) =>
                {
                    sidePanels[index].Visible = false;
                    if (navRail.ActiveIndex == index) navRail.ActiveIndex = -1;
                };
            }

            mapHost.PoiClicked += (s, poi) => ShowPoiCard(poi);

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
                lastView = view;
                statusBar.SetScale(MapScale.Format(view.ScaleDenominator));
                ShowArea();
                if (explorePanel.Visible) { exploreTimer.Stop(); exploreTimer.Start(); }
            };

            // The server's list of places replaces the bundled one when it arrives: redraw the markers and the search results.
            PoiCatalog.Changed += (s, e) =>
            {
                mapHost.LoadPois(PoiCatalog.All);
            };
            routeOptimizerPanel.Api = api;
            routeOptimizerPanel.PickStartRequested += (s, on) => { if (on) mapHost.BeginPickPoint(); else mapHost.CancelPickPoint(); };
            // "Tìm đường" closes the route form and opens the result; "Dừng lại" clears the map and the form and brings the empty form back.
            routeOptimizerPanel.RouteComputed += (s, trip) =>
            {
                ShowSidePanel(-1);                  // the route form closes ...
                tripResultPanel.Open(trip);         // ... and the result opens, a low bar centred under the map
                mapHost.ShowTrip(trip, true);
            };
            tripResultPanel.StopRequested += (s, e) =>
            {
                mapHost.ClearRoute();
                tripResultPanel.CloseAll();
                routeOptimizerPanel.ResetAll();
                ShowSidePanel(NavRail.Route);
            };
            tripResultPanel.LegSelected += (s, leg) => mapHost.ZoomToTripLeg(leg);
            tripResultPanel.PlayRequested += (s, e) => mapHost.PlayTrip();
            tripResultPanel.PauseRequested += (s, e) => mapHost.PauseTrip();
            tripResultPanel.ReplayRequested += (s, e) => mapHost.ReplayTrip();
            tripResultPanel.FitRequested += (s, e) => mapHost.ZoomToTrip();
            mapHost.TripPlayStateChanged += (s, e) => tripResultPanel.SetCarPlaying(mapHost.TripPlaying);
            mapHost.TripLegChanged += (s, leg) => tripResultPanel.SetActiveLeg(leg);
            mapHost.PointPicked += (s, point) =>
            {
                double longitude, latitude;
                if (mapHost.TryToDegrees(point, out longitude, out latitude)) routeOptimizerPanel.SetStartFromMap(point, longitude, latitude);
            };
            poiIdentifyCard.AddToRouteRequested += (s, poi) =>
            {
                ShowSidePanel(NavRail.Route);
                if (routeOptimizerPanel.TrySelectStop(poi)) toast.ShowMessage("Đã thêm vào lộ trình: " + poi.Name, ToastKind.Success);
                else toast.ShowMessage("Địa điểm này chưa có vị trí trên bản đồ nên chưa thêm được vào lộ trình", ToastKind.Info);
            };

        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (titleBar != null) titleBar.SetMaximized(WindowState == FormWindowState.Maximized);
        }

        // Shows the panel of one rail tab (NavRail.Explore / Route / Guide / Profile) and closes the others; -1 closes all.
        private void ShowSidePanel(int index)
        {
            for (int i = 0; i < sidePanels.Length; i++)
            {
                if (i != index && sidePanels[i] != null) sidePanels[i].Visible = false;
            }
            // No BringToFront here: for a window of its own it activates the panel and takes the keyboard from whoever is
            // typing (the header search box). The panels are owned by this window, so they stay above it anyway.
            if (index >= 0 && sidePanels[index] != null)
            {
                sidePanels[index].Visible = true;
                if (index == NavRail.Profile) Safe(accountPanel.OpenAsync());
                if (index == NavRail.Explore) RefreshExplore();
            }
            navRail.ActiveIndex = index <= NavRail.Profile ? index : -1;   // the layer list belongs to the filter bar, not to a rail tab
        }

        // Back to the sign-in screen (signed out, password changed, or the session ended); Program.Main shows it again.
        private void ReturnToSignIn(string notice)
        {
            if (session.ReturnToSignIn) return;
            session.ReturnToSignIn = true;
            session.Notice = notice;
            Close();
        }

        // Fills the search drop-down with the places whose name matches (diacritic-insensitive), names that start with it first.
        private void UpdateSearchResults(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) { searchDropdown.Dismiss(); return; }
            string wanted = Hue_travel.Panels.SearchPoi.PoiSearchFilter.Fold(text);
            List<Poi> matches = Hue_travel.Panels.SearchPoi.PoiSearchFilter.Apply(PoiCatalog.All.Where(p => p.X != 0 || p.Y != 0), text.Trim(), null)
                .OrderBy(p => Hue_travel.Panels.SearchPoi.PoiSearchFilter.Fold(p.Name).StartsWith(wanted, StringComparison.Ordinal) ? 0 : 1)
                .ThenBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
            System.Drawing.Rectangle box = titleBar.SearchBoxBounds;
            box.Offset(titleBar.Location);
            searchDropdown.ShowResults(matches, box);
        }

        // Tells the Explore panel which part of the map is in view and what area its centre is in.
        private void RefreshExplore()
        {
            double xMin, yMin, xMax, yMax;
            if (!mapHost.TryGetViewBounds(out xMin, out yMin, out xMax, out yMax)) return;
            explorePanel.UpdateView(xMin, yMin, xMax, yMax);
            if (lastView != null) explorePanel.SetArea(areaLocator.Locate(lastView.CenterLongitude, lastView.CenterLatitude));
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
            poiIdentifyCard.FavoriteIds = favoriteIds;
            mapHost.HighlightPoi(poi);   // the place the card is about is ringed on the map until the card is closed
            poiIdentifyCard.ShowPoi(poi);
        }

        private void ShowArea()
        {
            if (lastView == null) return;
            statusBar.SetArea(areaLocator.Locate(lastView.CenterLongitude, lastView.CenterLatitude));
        }

        // ---------------------------------------------------------------- online mode
        private void SetUpAccount()
        {
            accountPanel = new AccountPanel(api, auth);
            accountPanel.AttachToSide(this, SideLeft, SideTop, SideBottom);
            accountPanel.SetRouteSource(() => routeOptimizerPanel.CurrentStopIds);
            accountPanel.ShowOnMapRequested += (s, poi) => ShowOnMap(poi);
            accountPanel.FavoritesChanged += (s, e) => Safe(LoadFavoritesAsync());
            accountPanel.LoggedOut += (s, e) => ReturnToSignIn(null);
            accountPanel.PasswordChanged += (s, e) => ReturnToSignIn("Đã đổi mật khẩu. Hãy đăng nhập lại bằng mật khẩu mới.");
            auth.Changed += (s, e) => UpdateAccountUi();

            poiIdentifyCard.FavoriteToggled += (s, poi) => Safe(ToggleFavoriteAsync(poi));
            poiIdentifyCard.ClaimRequested += (s, poi) => Safe(ClaimAsync(poi));
            poiIdentifyCard.ReportRequested += (s, poi) => Safe(ReportAsync(poi));
            UpdateAccountUi();
        }

        private void UpdateAccountUi()
        {
            UserInfo user = auth.CurrentUser;
            poiIdentifyCard.SignedIn = user != null;
            if (user == null)
            {
                favoriteIds.Clear();
                // Signing out or changing the password is handled by the panel's own events; anything else is a session that ended.
                if (!accountPanel.IsLeaving) ReturnToSignIn("Phiên đăng nhập đã hết hạn. Hãy đăng nhập lại.");
            }
            poiIdentifyCard.RefreshAccountButtons();
        }

        // Runs a task nobody awaits (a button handler, an event) and turns its failure into a message instead of a crash.
        private async void Safe(Task task)
        {
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                // the window was closed
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Safe: " + ex);
                toast.ShowMessage("Có lỗi: " + ex.Message, ToastKind.Error);
            }
        }

        private async Task LoadFavoritesAsync()
        {
            ApiResponse r = await api.GetAsync("/v1/favorites", lifetime.Token);
            if (!r.IsSuccess || auth.CurrentUser == null) return;   // an answer for someone who has signed out is dropped
            favoriteIds.Clear();
            foreach (var d in (JsonHelper.AsList(r.Json) ?? new List<object>()).OfType<IDictionary<string, object>>()) favoriteIds.Add(JsonHelper.Str(d, "id"));
            poiIdentifyCard.RefreshAccountButtons();
        }

        private async Task ToggleFavoriteAsync(Poi poi)
        {
            bool isFavorite = favoriteIds.Contains(poi.Id);
            string path = "/v1/favorites/" + Uri.EscapeDataString(poi.Id);
            ApiResponse r = isFavorite ? await api.DeleteAsync(path) : await api.PutAsync(path, null);
            if (!r.IsSuccess) { toast.ShowMessage(r.Error, ToastKind.Error); return; }
            if (isFavorite) favoriteIds.Remove(poi.Id); else favoriteIds.Add(poi.Id);
            poiIdentifyCard.RefreshAccountButtons();
            toast.ShowMessage(isFavorite ? "Đã bỏ khỏi yêu thích" : "Đã thêm vào yêu thích", ToastKind.Success);
        }

        private async Task ClaimAsync(Poi poi)
        {
            ApiResponse r = await api.PostAsync("/v1/owner/claims", new Dictionary<string, object> { { "poiId", poi.Id } });
            toast.ShowMessage(r.IsSuccess ? "Đã gửi yêu cầu. Quản trị viên sẽ xác minh với bạn rồi duyệt." : r.Error, r.IsSuccess ? ToastKind.Success : ToastKind.Error);
        }

        private async Task ReportAsync(Poi poi)
        {
            string[] kinds = { "wrong_info", "closed", "wrong_location", "other" };
            string[] names = { "Sai thông tin", "Đã đóng cửa", "Sai vị trí", "Khác" };
            int index = Dialogs.Pick(this, "Báo sai: " + poi.Name, new[] { "Loại báo cáo" }, names.Select(n => new[] { n }).ToList());
            if (index < 0) return;
            string[] v = Dialogs.Ask(this, "Chi tiết (không bắt buộc)", new DialogField("Mô tả:", "", 3));
            if (v == null) return;
            ApiResponse r = await api.PostAsync("/v1/pois/" + Uri.EscapeDataString(poi.Id) + "/reports", new Dictionary<string, object> { { "kind", kinds[index] }, { "content", v[0] } });
            toast.ShowMessage(r.IsSuccess ? "Đã gửi báo cáo. Cảm ơn bạn" : r.Error, r.IsSuccess ? ToastKind.Success : ToastKind.Error);
        }

        private void ShowOnMap(Poi poi)
        {
            mapHost.HighlightPoi(poi);
            if (poi.X != 0 || poi.Y != 0) mapHost.ZoomToProjected(poi.X, poi.Y);
            else if (poi.HasLocation) mapHost.ZoomTo(poi.Longitude, poi.Latitude);
        }

        // Cached places first (instant, works without a network), then the session and the fresh list from the server.
        private async void StartOnline()
        {
            try
            {
                IReadOnlyList<Poi> cached = PoiCache.TryLoad(ServerSettings.BaseUrl);
                if (cached != null) PoiCatalog.Replace(cached);
                if (auth.CurrentUser != null) await LoadFavoritesAsync();   // already signed in on the sign-in screen

                ApiConfigSource config = await ApiConfigSource.FetchAsync(api, lifetime.Token);
                if (BaseMapCatalog.ApplyOverrides(config.TileUrls, config.TileAttribution)) mapHost.ReloadBaseMap();

                IReadOnlyList<Poi> fresh = await ApiPoiSource.FetchAllAsync(api, lifetime.Token);
                if (fresh.Count == 0) return;
                PoiCatalog.Replace(fresh);
                PoiCache.TrySave(fresh, ServerSettings.BaseUrl);
            }
            catch (ApiException ex)
            {
                toast.ShowMessage(ex.Message + " Đang dùng dữ liệu ngoại tuyến.", ToastKind.Info);
            }
            catch (OperationCanceledException)
            {
                // the window was closed
            }
            catch (Exception ex)
            {
                // A background start-up task must never take the app down: stay on the data we already have.
                Debug.WriteLine("StartOnline: " + ex);
                toast.ShowMessage("Không tải được dữ liệu từ máy chủ. Đang dùng dữ liệu ngoại tuyến.", ToastKind.Info);
            }
        }
    }
}
