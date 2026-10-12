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
        public MainForm()
        {
            // With a server address the app is "online": accounts, the server's places, road routes and weather.
            // Without one it works offline on the data that ships with it.
            if (ServerSettings.IsOnline)
            {
                api = new ApiClient(ServerSettings.BaseUrl, new TokenStore(), null, ResponseVerifier.TryCreate(ServerSettings.PublicKey));
                auth = new AuthService(api, new TokenStore());
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
                searchPoiPanel, routeOptimizerPanel, travelGuidePanel, poiIdentifyCard, authModal, registerPanel,
            };
            if (accountPanel != null) all.Add(accountPanel);
            panels = all.ToArray();
            foreach (FloatingPanelForm panel in panels) panel.MessageRaised += (s, e) => toast.ShowMessage(e.Text, e.Kind);
        }

        private readonly FloatingPanelForm[] panels;
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
            titleBar.AuthClicked += (s, e) => OnAccountButton();

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
                if (poi.X != 0 || poi.Y != 0 || poi.HasLocation) ShowOnMap(poi);
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
                lastView = view;
                statusBar.SetScale(MapScale.Format(view.ScaleDenominator));
                ShowArea();
            };

            // The server's list of places replaces the bundled one when it arrives: redraw the markers and the search results.
            PoiCatalog.Changed += (s, e) =>
            {
                mapHost.LoadPois(PoiCatalog.All);
                searchPoiPanel.ReloadPois();
            };
            routeOptimizerPanel.Api = api;
            routeOptimizerPanel.RouteInvalidated += (s, e) => mapHost.ClearRoute();
            routeOptimizerPanel.RouteComputed += (s, route) => mapHost.ShowRoute(route.Legs.Select(l => l.Path).ToList());
            poiIdentifyCard.AddToRouteRequested += (s, poi) =>
            {
                TogglePanel(routeOptimizerPanel, true);
                if (routeOptimizerPanel.TrySelectStop(poi)) toast.ShowMessage("Đã thêm vào lộ trình: " + poi.Name, ToastKind.Success);
                else toast.ShowMessage("Địa điểm này chưa có vị trí trên bản đồ nên chưa thêm được vào lộ trình", ToastKind.Info);
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
            poiIdentifyCard.FavoriteIds = favoriteIds;
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
            accountPanel.AttachTo(this, new System.Drawing.Point(510, 110), System.Windows.Forms.AnchorStyles.Top);
            accountPanel.SetRouteSource(() => routeOptimizerPanel.CurrentStopIds);
            accountPanel.CloseRequested += (s, e) => accountPanel.Visible = false;
            accountPanel.ShowOnMapRequested += (s, poi) => ShowOnMap(poi);
            accountPanel.FavoritesChanged += (s, e) => Safe(LoadFavoritesAsync());
            accountPanel.LoggedOut += (s, e) =>
            {
                accountPanel.Visible = false;
                ShowAuthCard(authModal);
            };

            authModal.Service = auth;
            registerPanel.Service = auth;
            authModal.Authenticated += (s, e) => OnSignedIn(authModal);
            registerPanel.Registered += (s, e) => OnSignedIn(registerPanel);
            auth.Changed += (s, e) => UpdateAccountUi();

            poiIdentifyCard.FavoriteToggled += (s, poi) => Safe(ToggleFavoriteAsync(poi));
            poiIdentifyCard.ClaimRequested += (s, poi) => Safe(ClaimAsync(poi));
            poiIdentifyCard.ReportRequested += (s, poi) => Safe(ReportAsync(poi));
        }

        private void OnAccountButton()
        {
            if (api == null)
            {
                toast.ShowMessage("Chưa có địa chỉ máy chủ nên chưa đăng nhập được. Ứng dụng đang chạy ngoại tuyến.", ToastKind.Info);
                return;
            }
            if (auth.CurrentUser == null) ToggleAuth();
            else if (accountPanel.Visible) accountPanel.Visible = false;
            else Safe(accountPanel.OpenAsync());
        }

        private void OnSignedIn(FloatingPanelForm card)
        {
            card.Visible = false;
            toast.ShowMessage("Xin chào " + auth.CurrentUser.DisplayName, ToastKind.Success);
            Safe(accountPanel.OpenAsync());
            Safe(LoadFavoritesAsync());
        }

        private void UpdateAccountUi()
        {
            UserInfo user = auth.CurrentUser;
            titleBar.SetUser(user == null ? null : user.DisplayName);
            poiIdentifyCard.SignedIn = user != null;
            if (user == null)
            {
                favoriteIds.Clear();
                accountPanel.Visible = false;   // signed out, or the session ended
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
                if (await auth.RestoreAsync()) await LoadFavoritesAsync();

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
