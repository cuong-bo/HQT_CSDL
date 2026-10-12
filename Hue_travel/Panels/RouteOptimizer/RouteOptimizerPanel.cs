using System;
using System.Collections.Generic;
using System.Globalization;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Hue_travel.Models;
using Hue_travel.Services.Area;
using Hue_travel.Services.Data;
using Hue_travel.Services.Online;
using Hue_travel.Services.Routing;
using Hue_travel.Theme;

namespace Hue_travel.Panels.RouteOptimizer
{
    public partial class RouteOptimizerPanel : FloatingPanelForm
    {
        // The real place behind each row of the stop list, found by name in the current places.
        private static readonly string[] StopNames = { "Đại Nội", "Thiên Mụ", "Tự Đức", "Khải Định" };
        private static readonly string[] StopIds = { "DL021", "DL024", "DL029", "DL033" };   // looked up first; the name is the fallback

        private readonly Poi[] stopPois = new Poi[4];
        private readonly Poi[] chosenPois = new Poi[4];   // places the visitor put into a row instead of the suggested one
        private IList<Poi> startPois = new List<Poi>();
        private ApiRouteService routeService;
        private int planVersion;
        private System.Threading.CancellationTokenSource planCts;
        private bool routeShown;

        /// <summary>The route panel talks to the server for road routes when this is set (otherwise it estimates).</summary>
        public ApiClient Api
        {
            set { routeService = value == null ? null : new ApiRouteService(value); }
        }

        /// <summary>Raised with the road route computed by the server, to draw it on the map.</summary>
        public event EventHandler<OnlineRoute> RouteComputed;

        /// <summary>Raised when the drawn route no longer matches the selection (a stop or the start changed).</summary>
        public event EventHandler RouteInvalidated;

        // Any change of selection makes an answer that is still on its way (or already drawn) obsolete.
        private void InvalidateOnlineRoute()
        {
            planVersion++;
            if (planCts != null) planCts.Cancel();
            if (!routeShown) return;
            routeShown = false;
            RouteInvalidated?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Place ids of the ticked stops in visiting order, for saving the itinerary to the account.</summary>
        public IReadOnlyList<string> CurrentStopIds
        {
            get
            {
                var ids = new List<string>();
                foreach (RouteStopRow row in stops.Where(s => s.Checked).OrderBy(s => s.Order))
                {
                    Poi poi = stopPois[Array.IndexOf(stops, row)];
                    if (poi != null) ids.Add(poi.Id);
                }
                return ids;
            }
        }

        /// <summary>
        /// Ticks the row that shows this place. A place that is not among the suggested stops takes over the first row
        /// that is not ticked (the last row when all are ticked); false when the place has no position on the map.
        /// </summary>
        public bool TrySelectStop(Poi poi)
        {
            if (poi == null || !poi.HasLocation) return false;
            for (int i = 0; i < stopPois.Length; i++)
            {
                if (stopPois[i] == null || stopPois[i].Id != poi.Id) continue;
                stops[i].Checked = true;
                return true;
            }

            int slot = Array.FindIndex(stops, s => !s.Checked);
            if (slot < 0) slot = stops.Length - 1;
            chosenPois[slot] = poi;
            bool wasChecked = stops[slot].Checked;
            ResolvePlaces();
            if (wasChecked) UpdatePlan();   // the row kept its tick but now shows another place
            else stops[slot].Checked = true;
            return true;
        }

        private readonly RouteStopRow[] stops;
        private readonly RoundedPanel[] legBoxes;
        private readonly RichTextBlock[] legTexts;
        private readonly Label[] legValues;
        private RoutePlan currentPlan;

        public RouteOptimizerPanel()
        {
            InitializeComponent();

            stops = new[] { stopDaiNoi, stopThienMu, stopBunBo, stopKhaiDinh };
            ResolvePlaces();
            legBoxes = new[] { boxLeg1, boxLeg2, boxLeg3, boxLeg4 };
            legTexts = new[] { textLeg1, textLeg2, textLeg3, textLeg4 };
            legValues = new[] { lblLeg1Value, lblLeg2Value, lblLeg3Value, lblLeg4Value };
            foreach (RouteStopRow stop in stops) stop.CheckedChanged += (s, e) => UpdatePlan();
            cboStartPoint.SelectedChanged += (s, e) => UpdatePlan();
            UpdatePlan();

            btnSave.Click += (s, e) => SaveItinerary();
            btnPrint.Click += (s, e) => PrintItinerary();
            btnFindRoute.Click += async (s, e) => await FindRouteAsync();
            PoiCatalog.Changed += (s, e) => { ResolvePlaces(); UpdatePlan(); };
        }

        /// <summary>The itinerary as plain text, in the planned visiting order.</summary>
        private string BuildItinerary()
        {
            var text = new StringBuilder();
            text.AppendLine("LỘ TRÌNH THAM QUAN - CỐ ĐÔ GIS");
            text.AppendLine(new string('=', 40));
            text.AppendLine("Điểm bắt đầu: " + cboStartPoint.Text);
            text.AppendLine();
            text.AppendLine("Các điểm tham quan:");
            foreach (RouteStopRow stop in stops.Where(s => s.Checked).OrderBy(s => s.Order))
            {
                text.AppendLine(stop.Order + ". " + StopName(stop));
                text.AppendLine("   " + stop.Subtitle);
            }
            text.AppendLine();
            text.AppendLine("Tổng quãng đường: " + lblDistanceValue.Text);
            text.AppendLine("Thời gian dự kiến: " + lblDurationValue.Text);
            text.AppendLine("Tiết kiệm: " + lblSavingValue.Text);
            text.AppendLine();
            text.AppendLine("Chi tiết các chặng di chuyển:");
            for (int i = 0; i < legBoxes.Length; i++)
            {
                if (legBoxes[i].Visible) text.AppendLine(legTexts[i].Text.Replace("**", "") + " - " + legValues[i].Text);
            }
            text.AppendLine();
            text.AppendLine(routeService == null ? "(Quãng đường ước tính theo đường chim bay nhân hệ số 1,3; thời gian tính ở tốc độ trung bình 25 km/h.)" : "(Quãng đường và thời gian tính theo đường bộ thật từ máy chủ khi đã bấm tìm đường.)");
            return text.ToString();
        }

        private void SaveItinerary()
        {
            using (var dialog = new SaveFileDialog { Filter = "Tệp văn bản (*.txt)|*.txt", FileName = "LoTrinhThamQuan.txt", Title = "Lưu lịch trình vào máy" })
            {
                if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
                try
                {
                    File.WriteAllText(dialog.FileName, BuildItinerary(), new UTF8Encoding(true));
                    Notify("Đã lưu lịch trình: " + Path.GetFileName(dialog.FileName), ToastKind.Success);
                }
                catch (IOException ex)
                {
                    Notify("Không lưu được tệp: " + ex.Message, ToastKind.Error);
                }
                catch (UnauthorizedAccessException)
                {
                    Notify("Không có quyền ghi vào vị trí đã chọn", ToastKind.Error);
                }
            }
        }

        private void PrintItinerary()
        {
            string itinerary = BuildItinerary();
            using (var document = new PrintDocument())
            using (var preview = new PrintPreviewDialog { Document = document, Width = 900, Height = 700 })
            using (var font = new Font("Segoe UI", 11f))
            {
                document.PrintPage += (s, e) => e.Graphics.DrawString(itinerary, font, Brushes.Black, e.MarginBounds);
                preview.ShowDialog(FindForm());
            }
        }

        private static Poi FindByName(IEnumerable<Poi> pois, string key) =>
            pois.FirstOrDefault(p => p.Name != null && CultureInfo.CurrentCulture.CompareInfo.IndexOf(p.Name, key, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0);

        // Rows and the start list follow the places currently loaded (bundled data, then the server's).
        private void ResolvePlaces()
        {
            for (int i = 0; i < stops.Length; i++)
            {
                Poi chosen = chosenPois[i] == null ? null : PoiCatalog.All.FirstOrDefault(p => p.Id == chosenPois[i].Id && p.HasLocation);
                chosenPois[i] = chosen;
                Poi poi = chosen ?? PoiCatalog.All.FirstOrDefault(p => p.Id == StopIds[i] && p.HasLocation) ?? FindByName(PoiCatalog.All.Where(p => p.HasLocation), StopNames[i]);
                stopPois[i] = poi;
                stops[i].Visible = poi != null;
                if (poi == null) continue;
                stops[i].Text = (i + 1) + ". " + poi.Name;
                stops[i].Subtitle = string.IsNullOrEmpty(poi.TypeLabel) ? poi.Address : poi.TypeLabel;
            }
            string previous = cboStartPoint.Text;
            startPois = PoiCatalog.All.Where(p => p.Category == PoiCategory.Transport && p.HasLocation).ToList();
            cboStartPoint.Items.Clear();
            cboStartPoint.Items.AddRange(startPois.Select(p => p.Name).ToArray());
            Poi keep = startPois.FirstOrDefault(p => p.Name == previous) ?? FindByName(startPois, "Ga Huế") ?? startPois.FirstOrDefault();
            cboStartPoint.Text = keep != null ? keep.Name : "";
        }

        private Poi StartPoi() => startPois.FirstOrDefault(p => p.Name == cboStartPoint.Text);

        private void header_CloseClicked(object sender, EventArgs e) => RaiseCloseRequested();

        private static string StopName(RouteStopRow stop) => Regex.Replace(stop.Text, @"^\d+\.\s*", "");

        // "Ga Huế (Đường Bùi Thị Xuân)" -> "Ga Huế"
        private static string ShortName(string name) => Regex.Replace(name, @"\s*\(.*\)\s*$", "");

        private static string FormatKm(double km) => km.ToString("0.0", CultureInfo.InvariantCulture) + " km";

        private static string FormatMinutes(int minutes) =>
            minutes >= 60 ? (minutes / 60) + " giờ " + (minutes % 60) + " phút" : minutes + " phút";

        /// <summary>
        /// Orders the ticked stops for the shortest trip from the chosen start, numbers them in that order and fills in
        /// the distance, time, saving and leg list. Distances are estimates until the street network is available.
        /// </summary>
        private void UpdatePlan()
        {
            InvalidateOnlineRoute();
            var selected = new List<int>();
            var points = new List<GeoPoint>();
            for (int i = 0; i < stops.Length; i++)
            {
                if (!stops[i].Checked || stopPois[i] == null) continue;
                selected.Add(i);
                points.Add(new GeoPoint(stopPois[i].Longitude, stopPois[i].Latitude));
            }

            Poi startPoi = StartPoi();
            NamedPlace start = startPoi == null ? null : new NamedPlace(startPoi.Name, new GeoPoint(startPoi.Longitude, startPoi.Latitude));
            bool hasStart = start != null && start.Location.HasValue;
            currentPlan = RoutePlanner.Plan(hasStart ? start.Location : null, points);

            for (int rank = 0; rank < currentPlan.Order.Count; rank++) stops[selected[currentPlan.Order[rank]]].Order = rank + 1;
            lblSelectedCount.Text = $"Đã chọn {selected.Count} điểm";
            lblDistanceValue.Text = FormatKm(currentPlan.TotalKm);
            lblDurationValue.Text = FormatMinutes(currentPlan.TravelMinutes);
            lblSavingValue.Text = currentPlan.SavedMinutes > 0 ? currentPlan.SavedMinutes + " phút di chuyển" : "Đã là thứ tự tốt nhất";

            // Each leg runs from the previous place (the start, when it has a position) to the next stop.
            string previous = hasStart ? ShortName(start.Name) : null;
            for (int leg = 0; leg < legBoxes.Length; leg++)
            {
                bool shown = leg < currentPlan.LegsKm.Count;
                legBoxes[leg].Visible = shown;
                if (!shown) continue;

                if (previous == null) previous = StopName(stops[selected[currentPlan.Order[0]]]);
                int target = hasStart ? leg : leg + 1;
                string name = StopName(stops[selected[currentPlan.Order[target]]]);
                double km = currentPlan.LegsKm[leg];
                legTexts[leg].Text = $"**Chặng {leg + 1}:** {previous} → {name}";
                legValues[leg].Text = $"{FormatKm(km)} (khoảng {(int)Math.Round(km / RoutePlanner.AverageSpeedKmh * 60)} phút)";
                previous = name;
            }
        }

        /// <summary>
        /// Online: asks the server for the road route (and for the visiting order as listed, to show the saving) and fills the
        /// figures and legs from it. Offline: keeps the estimate computed by <see cref="UpdatePlan"/>.
        /// </summary>
        private async System.Threading.Tasks.Task FindRouteAsync()
        {
            UpdatePlan();
            if (currentPlan.Order.Count == 0)
            {
                Notify("Hãy chọn ít nhất một điểm muốn đến", ToastKind.Info);
                return;
            }
            Poi startPoi = StartPoi();
            if (routeService == null || startPoi == null)
            {
                AnnouncePlan();
                return;
            }

            var stopIds = new List<string>();
            for (int i = 0; i < stops.Length; i++) if (stops[i].Checked && stopPois[i] != null) stopIds.Add(stopPois[i].Id);
            int version = planVersion;
            planCts = new System.Threading.CancellationTokenSource();
            System.Threading.CancellationToken token = planCts.Token;
            btnFindRoute.Enabled = false;
            try
            {
                // The order as listed is only asked for to show how many minutes the best order saves.
                System.Threading.Tasks.Task<ApiResult<OnlineRoute>> bestCall = routeService.PlanAsync(startPoi.Id, stopIds, true, token);
                System.Threading.Tasks.Task<ApiResult<OnlineRoute>> listedCall = routeService.PlanAsync(startPoi.Id, stopIds, false, token);
                await System.Threading.Tasks.Task.WhenAll(bestCall, listedCall);
                ApiResult<OnlineRoute> best = bestCall.Result;
                ApiResult<OnlineRoute> listed = listedCall.Result;
                if (version != planVersion) return;   // the selection changed while the server was working
                if (!best.Ok)
                {
                    Notify(best.Error + " Đang hiển thị số liệu ước tính.", ToastKind.Error);
                    return;
                }
                ShowOnlineRoute(startPoi, best.Value, listed.Ok ? listed.Value : null);
                routeShown = true;
                RouteComputed?.Invoke(this, best.Value);
                Notify($"Đã tìm đường theo đường bộ: {FormatKm(best.Value.TotalMeters / 1000)}, {FormatMinutes((int)Math.Round(best.Value.TotalSeconds / 60))}", ToastKind.Success);
            }
            catch (OperationCanceledException)
            {
                // the selection changed: the answer is not wanted any more
            }
            finally
            {
                btnFindRoute.Enabled = true;
            }
        }

        private void ShowOnlineRoute(Poi start, OnlineRoute route, OnlineRoute asListed)
        {
            // Number the rows in the server's order and fill the figures and legs from the road route.
            for (int rank = 0; rank < route.Order.Count; rank++)
            {
                int row = Array.FindIndex(stopPois, p => p != null && p.Id == route.Order[rank]);
                if (row >= 0) stops[row].Order = rank + 1;
            }
            lblDistanceValue.Text = FormatKm(route.TotalMeters / 1000);
            lblDurationValue.Text = FormatMinutes((int)Math.Round(route.TotalSeconds / 60));
            int saved = asListed == null ? 0 : (int)Math.Round((asListed.TotalSeconds - route.TotalSeconds) / 60);
            lblSavingValue.Text = saved > 0 ? saved + " phút di chuyển" : "Đã là thứ tự tốt nhất";

            string previous = ShortName(start.Name);
            for (int leg = 0; leg < legBoxes.Length; leg++)
            {
                bool shown = leg < route.Legs.Count;
                legBoxes[leg].Visible = shown;
                if (!shown) continue;
                Poi to = Array.Find(stopPois, p => p != null && p.Id == route.Legs[leg].ToId);
                string name = to != null ? to.Name : route.Legs[leg].ToId;
                legTexts[leg].Text = $"**Chặng {leg + 1}:** {previous} → {name}";
                legValues[leg].Text = $"{FormatKm(route.Legs[leg].Meters / 1000)} (khoảng {(int)Math.Round(route.Legs[leg].Seconds / 60)} phút)";
                previous = name;
            }
        }

        private void AnnouncePlan()
        {
            Notify($"Đã sắp xếp {currentPlan.Order.Count} điểm: khoảng {FormatKm(currentPlan.TotalKm)}, {FormatMinutes(currentPlan.TravelMinutes)} (ước tính)", ToastKind.Success);
        }
    }
}
