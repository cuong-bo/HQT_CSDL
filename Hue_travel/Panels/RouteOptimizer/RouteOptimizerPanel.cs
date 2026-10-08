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
using Hue_travel.Services.Area;
using Hue_travel.Services.Routing;
using Hue_travel.Theme;

namespace Hue_travel.Panels.RouteOptimizer
{
    public partial class RouteOptimizerPanel : FloatingPanelForm
    {
        // Sample POI behind each row of the stop list, in row order.
        private static readonly string[] StopPoiIds = { "dai-noi", "thien-mu", "bun-bo-mu-roi", "khai-dinh" };

        private readonly RouteStopRow[] stops;
        private readonly RoundedPanel[] legBoxes;
        private readonly RichTextBlock[] legTexts;
        private readonly Label[] legValues;
        private RoutePlan currentPlan;

        public RouteOptimizerPanel()
        {
            InitializeComponent();

            cboStartPoint.Items.AddRange(DemoPlaces.StartPoints.Select(p => p.Name).ToArray());
            stops = new[] { stopDaiNoi, stopThienMu, stopBunBo, stopKhaiDinh };
            legBoxes = new[] { boxLeg1, boxLeg2, boxLeg3, boxLeg4 };
            legTexts = new[] { textLeg1, textLeg2, textLeg3, textLeg4 };
            legValues = new[] { lblLeg1Value, lblLeg2Value, lblLeg3Value, lblLeg4Value };
            foreach (RouteStopRow stop in stops) stop.CheckedChanged += (s, e) => UpdatePlan();
            cboStartPoint.SelectedChanged += (s, e) => UpdatePlan();
            UpdatePlan();

            btnSave.Click += (s, e) => SaveItinerary();
            btnPrint.Click += (s, e) => PrintItinerary();
            btnFindRoute.Click += (s, e) => AnnouncePlan();
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
            text.AppendLine("(Quãng đường ước tính theo đường chim bay nhân hệ số 1,3; thời gian tính ở tốc độ trung bình 25 km/h.)");
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
            var selected = new List<int>();
            var points = new List<GeoPoint>();
            for (int i = 0; i < stops.Length; i++)
            {
                GeoPoint place;
                if (!stops[i].Checked || !DemoPlaces.TryGetPoi(StopPoiIds[i], out place)) continue;
                selected.Add(i);
                points.Add(place);
            }

            int startIndex = cboStartPoint.Items.IndexOf(cboStartPoint.Text);
            NamedPlace start = startIndex >= 0 ? DemoPlaces.StartPoints[startIndex] : null;
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

        private void AnnouncePlan()
        {
            UpdatePlan();
            if (currentPlan.Order.Count == 0)
            {
                Notify("Hãy chọn ít nhất một điểm muốn đến", ToastKind.Info);
                return;
            }
            Notify($"Đã sắp xếp {currentPlan.Order.Count} điểm: khoảng {FormatKm(currentPlan.TotalKm)}, {FormatMinutes(currentPlan.TravelMinutes)} (ước tính)", ToastKind.Success);
        }
    }
}
