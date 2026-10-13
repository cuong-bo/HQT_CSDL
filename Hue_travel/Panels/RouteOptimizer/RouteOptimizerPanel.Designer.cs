using System.Drawing;
using System.Windows.Forms;
using Hue_travel.Theme;

namespace Hue_travel.Panels.RouteOptimizer
{
    partial class RouteOptimizerPanel
    {
        private System.ComponentModel.IContainer components = null;
        private PanelHeader header;
        private NoAutoScrollPanel panelBody;
        private RoundedPanel boxTip;
        private IconButton iconTip;
        private RichTextBlock textTip;
        private Label lblStart;
        private SelectField cboStartPoint;
        private Label lblDestinations;
        private Label lblSelectedCount;
        private RoundedPanel boxStops;
        private RouteStopRow stopDaiNoi;
        private RouteStopRow stopThienMu;
        private RouteStopRow stopBunBo;
        private RouteStopRow stopKhaiDinh;
        private IconButton btnFindRoute;
        private RoundedPanel boxStats;
        private Label lblDistanceCaption;
        private Label lblDistanceValue;
        private Panel dividerStats1;
        private Label lblDurationCaption;
        private Label lblDurationValue;
        private Panel dividerStats2;
        private Label lblSavingCaption;
        private Label lblSavingValue;
        private Label lblDetail;
        private RoundedPanel boxLeg1;
        private RichTextBlock textLeg1;
        private Label lblLeg1Value;
        private RoundedPanel boxLeg2;
        private RichTextBlock textLeg2;
        private Label lblLeg2Value;
        private RoundedPanel boxLeg3;
        private RoundedPanel boxLeg4;
        private RichTextBlock textLeg4;
        private Label lblLeg4Value;
        private RichTextBlock textLeg3;
        private Label lblLeg3Value;
        private Panel panelFooter;
        private IconButton btnSave;
        private IconButton btnPrint;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        // Every property is assigned inline (no helper calls) so the WinForms Designer can render it.
        private void InitializeComponent()
        {
            this.header = new PanelHeader();
            this.panelBody = new NoAutoScrollPanel();
            this.boxTip = new RoundedPanel();
            this.iconTip = new IconButton();
            this.textTip = new RichTextBlock();
            this.lblStart = new Label();
            this.cboStartPoint = new SelectField();
            this.lblDestinations = new Label();
            this.lblSelectedCount = new Label();
            this.boxStops = new RoundedPanel();
            this.stopDaiNoi = new RouteStopRow();
            this.stopThienMu = new RouteStopRow();
            this.stopBunBo = new RouteStopRow();
            this.stopKhaiDinh = new RouteStopRow();
            this.btnFindRoute = new IconButton();
            this.boxStats = new RoundedPanel();
            this.lblDistanceCaption = new Label();
            this.lblDistanceValue = new Label();
            this.dividerStats1 = new Panel();
            this.lblDurationCaption = new Label();
            this.lblDurationValue = new Label();
            this.dividerStats2 = new Panel();
            this.lblSavingCaption = new Label();
            this.lblSavingValue = new Label();
            this.lblDetail = new Label();
            this.boxLeg1 = new RoundedPanel();
            this.textLeg1 = new RichTextBlock();
            this.lblLeg1Value = new Label();
            this.boxLeg2 = new RoundedPanel();
            this.textLeg2 = new RichTextBlock();
            this.lblLeg2Value = new Label();
            this.boxLeg3 = new RoundedPanel();
            this.boxLeg4 = new RoundedPanel();
            this.textLeg4 = new RichTextBlock();
            this.lblLeg4Value = new Label();
            this.textLeg3 = new RichTextBlock();
            this.lblLeg3Value = new Label();
            this.panelFooter = new Panel();
            this.btnSave = new IconButton();
            this.btnPrint = new IconButton();
            this.SuspendLayout();
            this.panelBody.SuspendLayout();
            this.panelFooter.SuspendLayout();

            // w-96 (384px) panel.
            this.BackColor = ThemeColors.SurfaceContainerLowest;
            this.Size = new Size(384, 680);

            this.header.IconName = "alt_route";
            this.header.Text = "Lên Lộ Trình Tham Quan Tự Động";
            this.header.Prominent = true;
            this.header.CloseClicked += new System.EventHandler(this.header_CloseClicked);

            // Footer: slate-50 strip with two outlined actions.
            this.panelFooter.Dock = DockStyle.Bottom;
            this.panelFooter.Height = 44;
            this.panelFooter.BackColor = ThemeColors.Slate50;

            this.btnSave.Text = "Lưu lịch trình vào máy";
            this.btnSave.IconName = "save_alt";
            this.btnSave.IconSize = 15;
            this.btnSave.Font = ThemeFonts.Medium(12);
            this.btnSave.ForeColor = ThemeColors.Slate700;
            this.btnSave.IconColor = ThemeColors.Primary;
            this.btnSave.FillColor = Color.White;
            this.btnSave.HoverFillColor = ThemeColors.Slate100;
            this.btnSave.BorderColor = ThemeColors.Slate200;
            this.btnSave.Radius = 4;
            this.btnSave.HorizontalPadding = 10;
            this.btnSave.Gap = 4;
            this.btnSave.Size = new Size(170, 28);
            this.btnSave.Location = new Point(12, 8);

            this.btnPrint.Text = "In lộ trình tham quan";
            this.btnPrint.IconName = "print";
            this.btnPrint.IconSize = 15;
            this.btnPrint.Font = ThemeFonts.Medium(12);
            this.btnPrint.ForeColor = ThemeColors.Slate700;
            this.btnPrint.IconColor = ThemeColors.Primary;
            this.btnPrint.FillColor = Color.White;
            this.btnPrint.HoverFillColor = ThemeColors.Slate100;
            this.btnPrint.BorderColor = ThemeColors.Slate200;
            this.btnPrint.Radius = 4;
            this.btnPrint.HorizontalPadding = 10;
            this.btnPrint.Gap = 4;
            this.btnPrint.Size = new Size(160, 28);
            this.btnPrint.Location = new Point(212, 8);

            this.panelFooter.Controls.Add(this.btnSave);
            this.panelFooter.Controls.Add(this.btnPrint);

            // Scrollable body (the mockup's max-h overflow-y-auto region).
            this.panelBody.Dock = DockStyle.Fill;
            this.panelBody.AutoScroll = true;
            this.panelBody.BackColor = ThemeColors.SurfaceContainerLowest;

            // Tip box: sky-50 / sky-100, lightbulb + 12px sky-900 text.
            this.boxTip.FillColor = ThemeColors.Sky50;
            this.boxTip.BorderColor = ThemeColors.Sky100;
            this.boxTip.Size = new Size(356, 92);
            this.boxTip.Location = new Point(14, 14);
            this.iconTip.AutoWidth = false;
            this.iconTip.IconName = "lightbulb";
            this.iconTip.IconSize = 18;
            this.iconTip.IconColor = ThemeColors.Primary;
            this.iconTip.FillColor = Color.Transparent;
            this.iconTip.HoverFillColor = Color.Transparent;
            this.iconTip.Cursor = Cursors.Default;
            this.iconTip.Size = new Size(18, 18);
            this.iconTip.Location = new Point(10, 12);
            this.textTip.Text = "Chọn các điểm bạn muốn ghé thăm. Ứng dụng sẽ tự động sắp xếp thứ tự đường đi ngắn nhất và tiết kiệm thời gian nhất cho bạn.";
            this.textTip.Font = ThemeFonts.Regular(12);
            this.textTip.ForeColor = ThemeColors.Sky900;
            this.textTip.LineSpacing = 1.6f;
            this.textTip.Size = new Size(308, 76);
            this.textTip.Location = new Point(38, 10);
            this.boxTip.Controls.Add(this.iconTip);
            this.boxTip.Controls.Add(this.textTip);

            this.lblStart.Text = "Điểm bắt đầu đi:";
            this.lblStart.Font = ThemeFonts.SemiBold(12);
            this.lblStart.ForeColor = ThemeColors.Slate800;
            this.lblStart.AutoSize = true;
            this.lblStart.Location = new Point(14, 118);

            this.cboStartPoint.Text = "Ga Huế (Đường Bùi Thị Xuân)";
            this.cboStartPoint.Size = new Size(356, 32);
            this.cboStartPoint.Location = new Point(14, 140);

            this.lblDestinations.Text = "Danh sách điểm muốn đến hôm nay:";
            this.lblDestinations.Font = ThemeFonts.SemiBold(12);
            this.lblDestinations.ForeColor = ThemeColors.Slate800;
            this.lblDestinations.AutoSize = true;
            this.lblDestinations.Location = new Point(14, 184);

            this.lblSelectedCount.Text = "Đã chọn 3 điểm";
            this.lblSelectedCount.Font = ThemeFonts.SemiBold(11);
            this.lblSelectedCount.ForeColor = ThemeColors.Primary;
            this.lblSelectedCount.AutoSize = false;
            this.lblSelectedCount.TextAlign = ContentAlignment.MiddleRight;
            this.lblSelectedCount.Size = new Size(110, 18);
            this.lblSelectedCount.Location = new Point(260, 184);

            // Stop list: slate-50 box with four rows.
            this.boxStops.FillColor = ThemeColors.Slate50;
            this.boxStops.BorderColor = ThemeColors.Slate100;
            this.boxStops.Size = new Size(356, 212);
            this.boxStops.Location = new Point(14, 208);

            this.stopDaiNoi.Text = "1. Đại Nội - Hoàng Thành Huế";
            this.stopDaiNoi.Subtitle = "Di tích lịch sử Hoàng Cung triều Nguyễn";
            this.stopDaiNoi.Checked = true;
            this.stopDaiNoi.Order = 1;
            this.stopDaiNoi.Size = new Size(340, 44);
            this.stopDaiNoi.Location = new Point(8, 8);

            this.stopThienMu.Text = "2. Chùa Thiên Mụ";
            this.stopThienMu.Subtitle = "Ngắm cảnh ven bờ sông Hương thơ mộng";
            this.stopThienMu.Checked = true;
            this.stopThienMu.Order = 2;
            this.stopThienMu.Size = new Size(340, 44);
            this.stopThienMu.Location = new Point(8, 58);

            this.stopBunBo.Text = "3. Quán Bún Bò Huế Mụ Rơi";
            this.stopBunBo.Subtitle = "Thưởng thức ẩm thực trưa xứ Cố Đô";
            this.stopBunBo.Checked = true;
            this.stopBunBo.Order = 3;
            this.stopBunBo.Size = new Size(340, 44);
            this.stopBunBo.Location = new Point(8, 108);

            this.stopKhaiDinh.Text = "4. Lăng Khải Định";
            this.stopKhaiDinh.Subtitle = "Kiến trúc lăng tẩm độc đáo Tây - Đông";
            this.stopKhaiDinh.Checked = false;
            this.stopKhaiDinh.Order = 4;
            this.stopKhaiDinh.Size = new Size(340, 44);
            this.stopKhaiDinh.Location = new Point(8, 158);

            this.boxStops.Controls.Add(this.stopDaiNoi);
            this.boxStops.Controls.Add(this.stopThienMu);
            this.boxStops.Controls.Add(this.stopBunBo);
            this.boxStops.Controls.Add(this.stopKhaiDinh);

            // Primary call to action (h-9).
            this.btnFindRoute.AutoWidth = false;
            this.btnFindRoute.Text = "Bắt Đầu Tìm Đường Ngắn Nhất";
            this.btnFindRoute.IconName = "directions";
            this.btnFindRoute.IconSize = 18;
            this.btnFindRoute.Font = ThemeFonts.Bold(12);
            this.btnFindRoute.ForeColor = Color.White;
            this.btnFindRoute.IconColor = Color.White;
            this.btnFindRoute.FillColor = ThemeColors.Primary;
            this.btnFindRoute.HoverFillColor = ThemeColors.PrimaryContainer;
            this.btnFindRoute.Elevated = true;
            this.btnFindRoute.Radius = 6;
            this.btnFindRoute.Gap = 6;
            this.btnFindRoute.Size = new Size(356, 36);
            this.btnFindRoute.Location = new Point(14, 432);

            // Stats strip: sky-50 to blue-50 gradient, three centred columns.
            this.boxStats.FillColor = ThemeColors.Sky50;
            this.boxStats.FillColor2 = ColorTranslator.FromHtml("#EFF6FF");
            this.boxStats.BorderColor = ThemeColors.Sky200;
            this.boxStats.Size = new Size(356, 58);
            this.boxStats.Location = new Point(14, 480);

            this.lblDistanceCaption.Text = "Tổng quãng đường";
            this.lblDistanceCaption.Font = ThemeFonts.Medium(10);
            this.lblDistanceCaption.ForeColor = ThemeColors.Slate500;
            this.lblDistanceCaption.AutoSize = false;
            this.lblDistanceCaption.TextAlign = ContentAlignment.MiddleCenter;
            this.lblDistanceCaption.BackColor = Color.Transparent;
            this.lblDistanceCaption.Size = new Size(118, 16);
            this.lblDistanceCaption.Location = new Point(0, 9);
            this.lblDistanceValue.Text = "14.8 km";
            this.lblDistanceValue.Font = ThemeFonts.Bold(14);
            this.lblDistanceValue.ForeColor = ThemeColors.Sky800;
            this.lblDistanceValue.AutoSize = false;
            this.lblDistanceValue.TextAlign = ContentAlignment.MiddleCenter;
            this.lblDistanceValue.BackColor = Color.Transparent;
            this.lblDistanceValue.Size = new Size(118, 22);
            this.lblDistanceValue.Location = new Point(0, 26);

            this.dividerStats1.BackColor = ThemeColors.Sky200;
            this.dividerStats1.Size = new Size(1, 24);
            this.dividerStats1.Location = new Point(119, 17);

            this.lblDurationCaption.Text = "Thời gian dự kiến";
            this.lblDurationCaption.Font = ThemeFonts.Medium(10);
            this.lblDurationCaption.ForeColor = ThemeColors.Slate500;
            this.lblDurationCaption.AutoSize = false;
            this.lblDurationCaption.TextAlign = ContentAlignment.MiddleCenter;
            this.lblDurationCaption.BackColor = Color.Transparent;
            this.lblDurationCaption.Size = new Size(118, 16);
            this.lblDurationCaption.Location = new Point(120, 9);
            this.lblDurationValue.Text = "3 giờ 45 phút";
            this.lblDurationValue.Font = ThemeFonts.Bold(14);
            this.lblDurationValue.ForeColor = ThemeColors.Slate800;
            this.lblDurationValue.AutoSize = false;
            this.lblDurationValue.TextAlign = ContentAlignment.MiddleCenter;
            this.lblDurationValue.BackColor = Color.Transparent;
            this.lblDurationValue.Size = new Size(118, 22);
            this.lblDurationValue.Location = new Point(120, 26);

            this.dividerStats2.BackColor = ThemeColors.Sky200;
            this.dividerStats2.Size = new Size(1, 24);
            this.dividerStats2.Location = new Point(238, 17);

            this.lblSavingCaption.Text = "Tiết kiệm";
            this.lblSavingCaption.Font = ThemeFonts.Medium(10);
            this.lblSavingCaption.ForeColor = ThemeColors.Slate500;
            this.lblSavingCaption.AutoSize = false;
            this.lblSavingCaption.TextAlign = ContentAlignment.MiddleCenter;
            this.lblSavingCaption.BackColor = Color.Transparent;
            this.lblSavingCaption.Size = new Size(116, 16);
            this.lblSavingCaption.Location = new Point(239, 9);
            this.lblSavingValue.Text = "45 phút di chuyển";
            this.lblSavingValue.Font = ThemeFonts.Bold(12);
            this.lblSavingValue.ForeColor = ThemeColors.Amber700;
            this.lblSavingValue.AutoSize = false;
            this.lblSavingValue.TextAlign = ContentAlignment.MiddleCenter;
            this.lblSavingValue.BackColor = Color.Transparent;
            this.lblSavingValue.Size = new Size(116, 22);
            this.lblSavingValue.Location = new Point(239, 26);

            this.boxStats.Controls.Add(this.lblDistanceCaption);
            this.boxStats.Controls.Add(this.lblDistanceValue);
            this.boxStats.Controls.Add(this.dividerStats1);
            this.boxStats.Controls.Add(this.lblDurationCaption);
            this.boxStats.Controls.Add(this.lblDurationValue);
            this.boxStats.Controls.Add(this.dividerStats2);
            this.boxStats.Controls.Add(this.lblSavingCaption);
            this.boxStats.Controls.Add(this.lblSavingValue);

            this.lblDetail.Text = "Chi tiết các chặng di chuyển (ước tính):";
            this.lblDetail.Font = ThemeFonts.Bold(12);
            this.lblDetail.ForeColor = ThemeColors.Slate800;
            this.lblDetail.AutoSize = true;
            this.lblDetail.Location = new Point(14, 550);

            // Three leg rows: slate-50 / slate-100, bold lead-in, right-aligned distance in primary.
            this.boxLeg1.FillColor = ThemeColors.Slate50;
            this.boxLeg1.BorderColor = ThemeColors.Slate100;
            this.boxLeg1.Radius = 4;
            this.boxLeg1.Size = new Size(356, 46);
            this.boxLeg1.Location = new Point(14, 574);
            this.textLeg1.Text = "**Chặng 1:** Ga Huế → Đại Nội Huế";
            this.textLeg1.Font = ThemeFonts.Regular(12);
            this.textLeg1.BoldFont = ThemeFonts.Bold(12);
            this.textLeg1.ForeColor = ThemeColors.Slate800;
            this.textLeg1.LineSpacing = 1.25f;
            this.textLeg1.Size = new Size(336, 18);
            this.textLeg1.Location = new Point(8, 5);
            this.lblLeg1Value.Text = "3.2 km (khoảng 12 phút)";
            this.lblLeg1Value.Font = ThemeFonts.SemiBold(11);
            this.lblLeg1Value.ForeColor = ThemeColors.Primary;
            this.lblLeg1Value.BackColor = Color.Transparent;
            this.lblLeg1Value.AutoSize = false;
            this.lblLeg1Value.TextAlign = ContentAlignment.MiddleRight;
            this.lblLeg1Value.Size = new Size(336, 18);
            this.lblLeg1Value.Location = new Point(8, 24);
            this.boxLeg1.Controls.Add(this.textLeg1);
            this.boxLeg1.Controls.Add(this.lblLeg1Value);

            this.boxLeg2.FillColor = ThemeColors.Slate50;
            this.boxLeg2.BorderColor = ThemeColors.Slate100;
            this.boxLeg2.Radius = 4;
            this.boxLeg2.Size = new Size(356, 46);
            this.boxLeg2.Location = new Point(14, 624);
            this.textLeg2.Text = "**Chặng 2:** Đại Nội Huế → Chùa Thiên Mụ";
            this.textLeg2.Font = ThemeFonts.Regular(12);
            this.textLeg2.BoldFont = ThemeFonts.Bold(12);
            this.textLeg2.ForeColor = ThemeColors.Slate800;
            this.textLeg2.LineSpacing = 1.25f;
            this.textLeg2.Size = new Size(336, 18);
            this.textLeg2.Location = new Point(8, 5);
            this.lblLeg2Value.Text = "4.6 km (khoảng 16 phút)";
            this.lblLeg2Value.Font = ThemeFonts.SemiBold(11);
            this.lblLeg2Value.ForeColor = ThemeColors.Primary;
            this.lblLeg2Value.BackColor = Color.Transparent;
            this.lblLeg2Value.AutoSize = false;
            this.lblLeg2Value.TextAlign = ContentAlignment.MiddleRight;
            this.lblLeg2Value.Size = new Size(336, 18);
            this.lblLeg2Value.Location = new Point(8, 24);
            this.boxLeg2.Controls.Add(this.textLeg2);
            this.boxLeg2.Controls.Add(this.lblLeg2Value);

            this.boxLeg3.FillColor = ThemeColors.Slate50;
            this.boxLeg3.BorderColor = ThemeColors.Slate100;
            this.boxLeg3.Radius = 4;
            this.boxLeg3.Size = new Size(356, 46);
            this.boxLeg3.Location = new Point(14, 674);
            this.textLeg3.Text = "**Chặng 3:** Chùa Thiên Mụ → Quán Bún Bò Mụ Rơi";
            this.textLeg3.Font = ThemeFonts.Regular(12);
            this.textLeg3.BoldFont = ThemeFonts.Bold(12);
            this.textLeg3.ForeColor = ThemeColors.Slate800;
            this.textLeg3.LineSpacing = 1.25f;
            this.textLeg3.Size = new Size(336, 18);
            this.textLeg3.Location = new Point(8, 5);
            this.lblLeg3Value.Text = "7.0 km (khoảng 25 phút)";
            this.lblLeg3Value.Font = ThemeFonts.SemiBold(11);
            this.lblLeg3Value.ForeColor = ThemeColors.Primary;
            this.lblLeg3Value.BackColor = Color.Transparent;
            this.lblLeg3Value.AutoSize = false;
            this.lblLeg3Value.TextAlign = ContentAlignment.MiddleRight;
            this.lblLeg3Value.Size = new Size(336, 18);
            this.lblLeg3Value.Location = new Point(8, 24);
            this.boxLeg3.Controls.Add(this.textLeg3);
            this.boxLeg3.Controls.Add(this.lblLeg3Value);

            this.boxLeg4.FillColor = ThemeColors.Slate50;
            this.boxLeg4.BorderColor = ThemeColors.Slate100;
            this.boxLeg4.Radius = 4;
            this.boxLeg4.Size = new Size(356, 46);
            this.boxLeg4.Location = new Point(14, 724);
            this.textLeg4.Text = "";
            this.textLeg4.Font = ThemeFonts.Regular(12);
            this.textLeg4.BoldFont = ThemeFonts.Bold(12);
            this.textLeg4.ForeColor = ThemeColors.Slate800;
            this.textLeg4.LineSpacing = 1.25f;
            this.textLeg4.Size = new Size(336, 18);
            this.textLeg4.Location = new Point(8, 5);
            this.lblLeg4Value.Text = "";
            this.lblLeg4Value.Font = ThemeFonts.SemiBold(11);
            this.lblLeg4Value.ForeColor = ThemeColors.Primary;
            this.lblLeg4Value.BackColor = Color.Transparent;
            this.lblLeg4Value.AutoSize = false;
            this.lblLeg4Value.TextAlign = ContentAlignment.MiddleRight;
            this.lblLeg4Value.Size = new Size(336, 18);
            this.lblLeg4Value.Location = new Point(8, 24);
            this.boxLeg4.Controls.Add(this.textLeg4);
            this.boxLeg4.Controls.Add(this.lblLeg4Value);

            this.panelBody.Controls.Add(this.boxLeg4);
            this.panelBody.Controls.Add(this.boxLeg3);
            this.panelBody.Controls.Add(this.boxLeg2);
            this.panelBody.Controls.Add(this.boxLeg1);
            this.panelBody.Controls.Add(this.lblDetail);
            this.panelBody.Controls.Add(this.boxStats);
            this.panelBody.Controls.Add(this.btnFindRoute);
            this.panelBody.Controls.Add(this.boxStops);
            this.panelBody.Controls.Add(this.lblSelectedCount);
            this.panelBody.Controls.Add(this.lblDestinations);
            this.panelBody.Controls.Add(this.cboStartPoint);
            this.panelBody.Controls.Add(this.lblStart);
            this.panelBody.Controls.Add(this.boxTip);

            this.Controls.Add(this.panelBody);
            this.Controls.Add(this.panelFooter);
            this.Controls.Add(this.header);
            this.Name = "RouteOptimizerPanel";
            this.panelFooter.ResumeLayout(false);
            this.panelBody.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
