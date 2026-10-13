using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Hue_travel.MapEngine;
using Hue_travel.Models;
using Hue_travel.Panels.Shared;
using Hue_travel.Panels.SearchPoi;
using Hue_travel.Services.Data;
using Hue_travel.Services.Online;
using Hue_travel.Services.Routing;
using Hue_travel.Theme;

namespace Hue_travel.Panels.RouteOptimizer
{
    /// <summary>
    /// "Lộ trình": the visitor picks a start (from the suggested places or by clicking the map), builds the itinerary from the full
    /// list of places and puts it in order; only when "Tìm đường" is pressed the route is computed. The form then closes and the
    /// result panel (<see cref="TripResultPanel"/>) takes over.
    /// </summary>
    public sealed class RouteOptimizerPanel : FloatingPanelForm
    {
        private const int MaxStops = 12;
        private const int PickerRows = 5;
        private const int SuggestRows = 6;
        private const int PickerMaxPlaces = 300;

        private sealed class StartSpot
        {
            public string Name;
            public MapXY Point;
            public double Longitude, Latitude;
            public Poi Place;   // set when the start is one of the listed places
        }


        // ------------------------------------------------------------------ state

        private readonly PanelHeader header = new PanelHeader { IconName = "route", Text = "Lộ trình tham quan", Prominent = true };
        private readonly NoAutoScrollPanel body = new NoAutoScrollPanel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White };

        private readonly Label lblStartTitle = Caption("Điểm bắt đầu");
        private readonly Label startLabel = new Label { AutoSize = false, Font = ThemeFonts.Medium(13), ForeColor = ThemeColors.Slate500, BackColor = ThemeColors.Slate50, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(12, 0, 8, 0), Text = "Chưa chọn điểm bắt đầu" };
        private readonly IconButton btnClearStart = new IconButton();
        private readonly Label lblStartHint = Hint("Bấm \"Gợi ý điểm bắt đầu\" để chọn từ danh sách, hoặc \"Chọn trên bản đồ\" rồi bấm vào vị trí bạn muốn xuất phát.");
        private readonly IconButton btnSuggest = new IconButton();
        private readonly IconButton btnPickOnMap = new IconButton();
        private readonly InputField suggestSearch = new InputField { IconName = "search" };
        private readonly Label lblSuggestCount = Hint("");
        private readonly PoiRowList suggestList = new PoiRowList { StealFocus = false, EmptyText = "Không có địa điểm nào khớp." };

        private readonly Label lblStopsTitle = Caption("Điểm đến");
        private readonly Label lblStopsCount = Hint("");
        private readonly StopList stopList = new StopList();
        private readonly IconButton btnAddStops = new IconButton();
        private readonly InputField pickerSearch = new InputField { IconName = "search" };
        private readonly Label lblPickerCount = Hint("");
        private readonly PoiRowList pickerList = new PoiRowList { ShowAddButton = true, StealFocus = false, EmptyText = "Không có địa điểm nào khớp." };
        private readonly IconButton btnAutoOrder = new IconButton();
        private readonly Label lblOrderHint = Hint("Xe đi đúng thứ tự trong danh sách. Dùng mũi tên để đổi chỗ, hoặc bấm nút trên để máy xếp giúp theo đường gần nhất.");
        private readonly IconButton btnFind = new IconButton();

        private readonly List<Poi> itinerary = new List<Poi>();
        private readonly List<Control> dividers = new List<Control>();
        private StartSpot start;
        private bool suggestOpen;
        private bool pickerOpen;
        private bool pickingOnMap;
        private bool busy;
        private ApiRouteService routeService;
        private int planVersion;
        private CancellationTokenSource planCts;

        /// <summary>The route panel asks the server for road routes when this is set (otherwise it estimates).</summary>
        public ApiClient Api
        {
            set { routeService = value == null ? null : new ApiRouteService(value); }
        }

        /// <summary>Raised with the computed trip: the host closes this form and shows the result.</summary>
        public event EventHandler<TripPlan> RouteComputed;

        /// <summary>Raised when "Chọn trên bản đồ" is switched on (true) or off (false).</summary>
        public event EventHandler<bool> PickStartRequested;

        public RouteOptimizerPanel()
        {
            Size = new Size(384, 640);
            header.CloseClicked += (s, e) => RaiseCloseRequested();

            StyleSecondary(btnSuggest, "Gợi ý điểm bắt đầu", "explore");
            StyleSecondary(btnPickOnMap, "Chọn trên bản đồ", "place");
            StyleSecondary(btnClearStart, "Xoá", "close");
            StyleSecondary(btnAddStops, "Thêm điểm đến từ danh sách địa điểm", "add");
            StyleSecondary(btnAutoOrder, "Xếp giúp thứ tự đi gần nhất", "route");
            StylePrimary(btnFind, "Tìm đường", "directions");

            foreach (Control c in new Control[]
            {
                lblStartTitle, startLabel, btnClearStart, lblStartHint, btnSuggest, btnPickOnMap, suggestSearch, lblSuggestCount, suggestList,
                lblStopsTitle, lblStopsCount, stopList, btnAddStops, pickerSearch, lblPickerCount, pickerList,
                btnAutoOrder, lblOrderHint, btnFind,
            }) body.Controls.Add(c);
            for (int i = 0; i < 2; i++)
            {
                var line = new Panel { BackColor = ThemeColors.Slate100, Height = 1 };
                dividers.Add(line);
                body.Controls.Add(line);
            }
            Controls.Add(body);
            Controls.Add(header);

            WireEvents();
            ReloadSuggestions();
            ReloadPicker();
            RefreshStops();
            PoiCatalog.Changed += (s, e) => { if (!IsDisposed) { ReloadSuggestions(); ReloadPicker(); } };
        }

        // ------------------------------------------------------------------ public use

        /// <summary>Place ids of the itinerary in visiting order, for saving it to the account.</summary>
        public IReadOnlyList<string> CurrentStopIds => itinerary.Select(p => p.Id).ToList();

        /// <summary>Adds a place to the itinerary (the card's "Thêm vào lộ trình"); false when it has no position on the map.</summary>
        public bool TrySelectStop(Poi poi)
        {
            if (poi == null || (poi.X == 0 && poi.Y == 0)) return false;
            AddStop(poi);
            return true;
        }

        /// <summary>Sets the start to a point picked on the map.</summary>
        public void SetStartFromMap(MapXY point, double longitude, double latitude)
        {
            SetPicking(false, false);
            string label = "Vị trí đã chọn trên bản đồ (" + latitude.ToString("0.0000", CultureInfo.InvariantCulture) + ", " + longitude.ToString("0.0000", CultureInfo.InvariantCulture) + ")";
            SetStart(new StartSpot { Name = label, Point = point, Longitude = longitude, Latitude = latitude });
        }

        /// <summary>Clears the start, the itinerary and anything being computed: the form is empty again (after "Dừng lại").</summary>
        public void ResetAll()
        {
            planVersion++;
            if (planCts != null) planCts.Cancel();
            SetBusy(false);
            SetPicking(false, true);
            start = null;
            itinerary.Clear();
            suggestOpen = pickerOpen = false;
            suggestSearch.Text = "";
            pickerSearch.Text = "";
            ShowStart();
            ReloadSuggestions();
            ReloadPicker();
            RefreshStops();
            LayoutAll();
        }

        // ------------------------------------------------------------------ building the controls

        private static Label Caption(string text) =>
            new Label { AutoSize = false, Text = text, Font = ThemeFonts.Bold(15), ForeColor = ThemeColors.Slate800, TextAlign = ContentAlignment.MiddleLeft };

        private static Label Hint(string text) =>
            new Label { AutoSize = false, Text = text, Font = ThemeFonts.Regular(12), ForeColor = ThemeColors.Slate500, TextAlign = ContentAlignment.TopLeft };

        private static void StyleSecondary(IconButton b, string text, string icon)
        {
            b.AutoWidth = false;
            b.Text = text;
            b.IconName = icon;
            b.IconSize = 16;
            b.Gap = 6;
            b.HorizontalPadding = 8;
            b.Font = ThemeFonts.Medium(13);
            b.Radius = 10;
            b.ForeColor = ThemeColors.Slate700;
            b.IconColor = ThemeColors.Primary;
            b.FillColor = Color.White;
            b.HoverFillColor = ThemeColors.Slate100;
            b.BorderColor = ThemeColors.Slate300;
        }

        private static void StylePrimary(IconButton b, string text, string icon)
        {
            b.AutoWidth = false;
            b.Text = text;
            b.IconName = icon;
            b.IconSize = 18;
            b.Gap = 8;
            b.Font = ThemeFonts.SemiBold(14);
            b.Radius = 12;
            b.ForeColor = Color.White;
            b.IconColor = Color.White;
            b.FillColor = ThemeColors.Primary;
            b.HoverFillColor = ThemeColors.PrimaryContainer;
            b.Elevated = true;
        }

        private void WireEvents()
        {
            btnSuggest.Click += (s, e) => { suggestOpen = !suggestOpen; if (suggestOpen) ReloadSuggestions(); LayoutAll(); };
            btnPickOnMap.Click += (s, e) => SetPicking(!pickingOnMap, true);
            btnClearStart.Click += (s, e) => { SetPicking(false, true); SetStart(null); };
            suggestSearch.TextChanged += (s, e) => ReloadSuggestions();
            suggestList.RowClicked += (s, row) => { suggestOpen = false; SetStart(FromPlace(row.Poi)); };

            stopList.MoveRequested += (s, move) => MoveStop(move.Key, move.Value);
            stopList.RemoveRequested += (s, index) => RemoveStop(index);
            btnAddStops.Click += (s, e) => { pickerOpen = !pickerOpen; if (pickerOpen) ReloadPicker(); LayoutAll(); };
            pickerSearch.TextChanged += (s, e) => ReloadPicker();
            pickerList.AddClicked += (s, row) => TogglePlace(row);
            pickerList.RowClicked += (s, row) => TogglePlace(row);
            btnAutoOrder.Click += (s, e) => AutoOrder();

            btnFind.Click += async (s, e) => await FindRouteAsync();
        }

        // ------------------------------------------------------------------ the start

        private StartSpot FromPlace(Poi poi) =>
            new StartSpot { Name = poi.Name, Point = new MapXY(poi.X, poi.Y), Longitude = poi.Longitude, Latitude = poi.Latitude, Place = poi };

        private void SetStart(StartSpot spot)
        {
            start = spot;
            ShowStart();
            LayoutAll();
        }

        private void ShowStart()
        {
            startLabel.Text = start == null ? "Chưa chọn điểm bắt đầu" : start.Name;
            startLabel.ForeColor = start == null ? ThemeColors.Slate500 : ThemeColors.Slate800;
        }

        private void SetPicking(bool on, bool raise)
        {
            if (pickingOnMap == on) return;
            pickingOnMap = on;
            btnPickOnMap.Text = on ? "Bấm vào bản đồ… (Huỷ)" : "Chọn trên bản đồ";
            btnPickOnMap.FillColor = on ? ThemeColors.Sky50 : Color.White;
            btnPickOnMap.BorderColor = on ? ThemeColors.Primary : ThemeColors.Slate300;
            if (raise) PickStartRequested?.Invoke(this, on);
        }

        // The full list of places to start from, hubs (stations, airport, bus) first, filtered by the search box.
        private void ReloadSuggestions()
        {
            IEnumerable<Poi> source = PoiCatalog.All.Where(p => p.X != 0 || p.Y != 0);
            IReadOnlyList<Poi> found = PoiSearchFilter.Apply(source, suggestSearch.Text, null);
            var rows = found.OrderBy(p => p.Category == PoiCategory.Transport ? 0 : p.Category == PoiCategory.Stay ? 1 : 2)
                .ThenBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase).Take(PickerMaxPlaces)
                .Select(p => new PoiRow(p) { Subtitle = PoiMarkerIcons.Describe(p) }).ToList();
            suggestList.SetRows(rows);
            lblSuggestCount.Text = found.Count + " địa điểm (ga, bến xe, khách sạn ở đầu)";
        }

        // ------------------------------------------------------------------ the itinerary

        private void AddStop(Poi poi)
        {
            if (itinerary.Any(p => p.Id == poi.Id)) return;
            if (itinerary.Count >= MaxStops)
            {
                Notify("Một lộ trình có tối đa " + MaxStops + " điểm đến", ToastKind.Info);
                return;
            }
            itinerary.Add(poi);
            AfterStopsChanged();
        }

        private void RemoveStop(int index)
        {
            if (index < 0 || index >= itinerary.Count) return;
            itinerary.RemoveAt(index);
            AfterStopsChanged();
        }

        private void MoveStop(int index, int direction)
        {
            int target = index + direction;
            if (index < 0 || target < 0 || index >= itinerary.Count || target >= itinerary.Count) return;
            Poi poi = itinerary[index];
            itinerary.RemoveAt(index);
            itinerary.Insert(target, poi);
            AfterStopsChanged();
        }

        private void TogglePlace(PoiRow row)
        {
            if (itinerary.Any(p => p.Id == row.Poi.Id)) itinerary.RemoveAll(p => p.Id == row.Poi.Id);
            else
            {
                if (itinerary.Count >= MaxStops)
                {
                    Notify("Một lộ trình có tối đa " + MaxStops + " điểm đến", ToastKind.Info);
                    return;
                }
                itinerary.Add(row.Poi);
            }
            AfterStopsChanged();
        }

        private void AfterStopsChanged()
        {
            RefreshStops();
            LayoutAll();
        }

        private void RefreshStops()
        {
            stopList.SetStops(itinerary);
            lblStopsCount.Text = itinerary.Count + " / " + MaxStops + " điểm";
            for (int i = 0; i < pickerList.RowCount; i++)
            {
                PoiRow row = pickerList.RowAt(i);
                row.Added = itinerary.Any(p => p.Id == row.Poi.Id);
            }
            pickerList.Repaint();
        }

        // Every place with a position, filtered by what is typed in the search box of the list.
        private void ReloadPicker()
        {
            IEnumerable<Poi> source = PoiCatalog.All.Where(p => p.X != 0 || p.Y != 0);
            IReadOnlyList<Poi> found = PoiSearchFilter.Apply(source, pickerSearch.Text, null);
            var rows = found.OrderBy(p => p.Category).ThenBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase).Take(PickerMaxPlaces)
                .Select(p => new PoiRow(p) { Subtitle = PoiMarkerIcons.Describe(p), Added = itinerary.Any(i => i.Id == p.Id) }).ToList();
            pickerList.SetRows(rows);
            lblPickerCount.Text = found.Count + " địa điểm" + (found.Count > PickerMaxPlaces ? " (hiện " + PickerMaxPlaces + " đầu tiên)" : "");
        }


        // ------------------------------------------------------------------ ordering

        // Puts the stops in the order of a short trip from the start (straight lines as a guide); the visitor can still move them.
        private void AutoOrder()
        {
            if (itinerary.Count < 2) return;
            var points = itinerary.Select(p => new GeoPoint(p.Longitude, p.Latitude)).ToList();
            GeoPoint? from = start == null ? (GeoPoint?)null : new GeoPoint(start.Longitude, start.Latitude);
            RoutePlan best = RoutePlanner.Plan(from, points);
            var ordered = best.Order.Select(i => itinerary[i]).ToList();
            itinerary.Clear();
            itinerary.AddRange(ordered);
            AfterStopsChanged();
            Notify("Đã xếp lại các điểm đến theo đường đi gần nhất", ToastKind.Success);
        }

        // ------------------------------------------------------------------ computing the route

        private async Task FindRouteAsync()
        {
            if (busy) return;
            if (start == null)
            {
                Notify("Hãy chọn điểm bắt đầu: bấm \"Gợi ý điểm bắt đầu\" hoặc \"Chọn trên bản đồ\"", ToastKind.Info);
                return;
            }
            if (itinerary.Count == 0)
            {
                Notify("Hãy thêm ít nhất một điểm đến vào lộ trình", ToastKind.Info);
                return;
            }

            List<Poi> places = itinerary.ToList();
            StartSpot from = start;
            planVersion++;
            if (planCts != null) planCts.Cancel();
            int version = planVersion;
            planCts = new CancellationTokenSource();
            CancellationToken token = planCts.Token;
            SetBusy(true);
            TripPlan result = null;
            string problem = null;
            try
            {
                if (routeService != null)
                {
                    var ids = places.Select(p => p.Id).ToList();
                    bool byPlace = from.Place != null && !ids.Contains(from.Place.Id);
                    ApiResult<OnlineRoute> answer = byPlace
                        ? await routeService.PlanAsync(from.Place.Id, ids, false, token)
                        : await routeService.PlanFromPointAsync(from.Point.X, from.Point.Y, ids, false, token);
                    if (version != planVersion) return;   // the form was reset while the server was working
                    if (answer.Ok) result = TripPlanner.FromOnline(answer.Value, from.Name, from.Point, places, null);
                    else problem = answer.Error;
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            finally
            {
                SetBusy(false);
            }

            if (version != planVersion) return;
            if (result == null)
            {
                result = TripPlanner.Estimate(from.Name, from.Longitude, from.Latitude, from.Point, places, false);
                if (problem != null) Notify(problem + " Đang hiển thị số liệu ước tính.", ToastKind.Error);
            }
            RouteComputed?.Invoke(this, result);
        }

        private void SetBusy(bool value)
        {
            busy = value;
            btnFind.Enabled = !value;
            btnFind.Text = value ? "Đang tìm đường…" : "Tìm đường";
        }

        // ------------------------------------------------------------------ layout

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutAll();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible) LayoutAll();
        }

        // Stacks the sections from the top (device pixels) and sets how far the body scrolls.
        private void LayoutAll()
        {
            if (body == null || dividers.Count < 2 || body.ClientSize.Width <= 0) return;   // the first resizes come before the controls exist
            int offset = body.AutoScrollPosition.Y;
            int pad = Ui.S(16), gap = Ui.S(10);
            int full = Math.Max(Ui.S(240), ClientSize.Width - SystemInformation.VerticalScrollBarWidth);
            int inner = full - pad * 2;
            int y = Ui.S(16);

            Action<Control, int> put = (c, h) => { c.SetBounds(pad, y + offset, inner, h); y += h; };

            // start
            put(lblStartTitle, Ui.S(26)); y += Ui.S(4);
            int clearWidth = Ui.S(76);
            bool hasStart = start != null;
            startLabel.SetBounds(pad, y + offset, hasStart ? inner - clearWidth - gap : inner, Ui.S(44));
            btnClearStart.Visible = hasStart;
            btnClearStart.SetBounds(pad + inner - clearWidth, y + offset, clearWidth, Ui.S(44));
            y += Ui.S(44) + gap;
            put(lblStartHint, Ui.S(52)); y += gap;
            int half = (inner - gap) / 2;
            btnSuggest.SetBounds(pad, y + offset, half, Ui.S(44));
            btnPickOnMap.SetBounds(pad + half + gap, y + offset, half, Ui.S(44));
            y += Ui.S(44) + gap;
            suggestSearch.Visible = lblSuggestCount.Visible = suggestList.Visible = suggestOpen;
            if (suggestOpen)
            {
                put(suggestSearch, Ui.S(40)); y += Ui.S(6);
                put(lblSuggestCount, Ui.S(20)); y += Ui.S(2);
                put(suggestList, SuggestRows * Ui.S(60)); y += gap;
            }
            y += Divider(0, pad, inner, y, offset);

            // itinerary
            lblStopsCount.SetBounds(pad, y + offset + Ui.S(4), inner, Ui.S(20));
            lblStopsCount.TextAlign = ContentAlignment.TopRight;
            put(lblStopsTitle, Ui.S(26)); y += Ui.S(4);
            put(stopList, stopList.WantedHeight); y += gap;
            btnAddStops.Text = pickerOpen ? "Đóng danh sách địa điểm" : "Thêm điểm đến từ danh sách địa điểm";
            btnAddStops.IconName = pickerOpen ? "expand_less" : "add";
            put(btnAddStops, Ui.S(44)); y += gap;
            pickerSearch.Visible = lblPickerCount.Visible = pickerList.Visible = pickerOpen;
            if (pickerOpen)
            {
                put(pickerSearch, Ui.S(40)); y += Ui.S(6);
                put(lblPickerCount, Ui.S(20)); y += Ui.S(2);
                put(pickerList, PickerRows * Ui.S(60)); y += gap;
            }
            btnAutoOrder.Enabled = itinerary.Count >= 2;
            put(btnAutoOrder, Ui.S(44)); y += Ui.S(6);
            put(lblOrderHint, Ui.S(64)); y += gap;
            y += Divider(1, pad, inner, y, offset);

            // the button that computes
            btnFind.SetBounds(pad, y + offset, inner, Ui.S(50));
            y += Ui.S(50) + gap;
            body.AutoScrollMinSize = new Size(0, y + pad);
        }

        // A thin line across the body; returns the space it takes.
        private int Divider(int index, int pad, int inner, int y, int offset)
        {
            Control line = dividers[index];
            line.SetBounds(pad, y + offset, inner, 1);
            line.Visible = true;
            return Ui.S(14);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && planCts != null) planCts.Cancel();
            base.Dispose(disposing);
        }
    }
}
