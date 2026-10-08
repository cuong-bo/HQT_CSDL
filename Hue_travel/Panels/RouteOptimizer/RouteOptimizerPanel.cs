using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Hue_travel.Theme;

namespace Hue_travel.Panels.RouteOptimizer
{
    public partial class RouteOptimizerPanel : FloatingPanelForm
    {
        private static readonly string[] StartPoints =
        {
            "Ga Huế (Đường Bùi Thị Xuân)",
            "Bến thuyền Tòa Khâm (Đường Lê Lợi)",
            "Sân bay Quốc tế Phú Bài",
            "Bến xe phía Nam TP. Huế",
            "Khách sạn đang ở",
        };

        private readonly RouteStopRow[] stops;

        public RouteOptimizerPanel()
        {
            InitializeComponent();

            cboStartPoint.Items.AddRange(StartPoints);
            stops = new[] { stopDaiNoi, stopThienMu, stopBunBo, stopKhaiDinh };
            foreach (RouteStopRow stop in stops) stop.CheckedChanged += (s, e) => RefreshSelectionSummary();
            RefreshSelectionSummary();

            btnSave.Click += (s, e) => SaveItinerary();
            btnPrint.Click += (s, e) => PrintItinerary();
            btnFindRoute.Click += (s, e) => Notify("Tìm đường ngắn nhất sẽ hoạt động khi có dữ liệu mạng đường", ToastKind.Info);
        }

        /// <summary>The itinerary as plain text. The distance/time figures are the panel's sample values.</summary>
        private string BuildItinerary()
        {
            var text = new StringBuilder();
            text.AppendLine("LỘ TRÌNH THAM QUAN - CỐ ĐÔ GIS");
            text.AppendLine(new string('=', 40));
            text.AppendLine("Điểm bắt đầu: " + cboStartPoint.Text);
            text.AppendLine();
            text.AppendLine("Các điểm tham quan:");
            foreach (RouteStopRow stop in stops.Where(s => s.Checked))
            {
                text.AppendLine(stop.Order + ". " + Regex.Replace(stop.Text, @"^\d+\.\s*", ""));
                text.AppendLine("   " + stop.Subtitle);
            }
            text.AppendLine();
            text.AppendLine("Tổng quãng đường: " + lblDistanceValue.Text);
            text.AppendLine("Thời gian dự kiến: " + lblDurationValue.Text);
            text.AppendLine("Tiết kiệm: " + lblSavingValue.Text);
            text.AppendLine();
            text.AppendLine("Chi tiết các chặng di chuyển:");
            text.AppendLine(textLeg1.Text.Replace("**", "") + " - " + lblLeg1Value.Text);
            text.AppendLine(textLeg2.Text.Replace("**", "") + " - " + lblLeg2Value.Text);
            text.AppendLine(textLeg3.Text.Replace("**", "") + " - " + lblLeg3Value.Text);
            text.AppendLine();
            text.AppendLine("(Số liệu quãng đường và thời gian là dữ liệu mẫu.)");
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

        /// <summary>Numbers the ticked stops 1..N in list order and updates the "Đã chọn N điểm" counter.</summary>
        private void RefreshSelectionSummary()
        {
            int order = 0;
            foreach (RouteStopRow stop in stops.Where(s => s.Checked)) stop.Order = ++order;
            lblSelectedCount.Text = $"Đã chọn {order} điểm";
        }
    }
}
