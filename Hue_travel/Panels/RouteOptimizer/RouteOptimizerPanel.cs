using System;
using System.Linq;
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
