using System.Drawing;
using System.Windows.Forms;
using Hue_travel.Theme;

namespace Hue_travel.Panels.TravelGuide
{
    partial class TravelGuidePanel
    {
        private System.ComponentModel.IContainer components = null;
        private PanelHeader header;
        private NoAutoScrollPanel panelBody;
        private RoundedPanel boxHotline;
        private IconButton iconHotline;
        private RichTextBlock textHotline;
        private IconButton headingTours;
        private TourCard tourHeritage;
        private TourCard tourNight;
        private TourCard tourFood;
        private IconButton headingRules;
        private RoundedPanel boxRules;
        private IconButton iconRule1;
        private RichTextBlock textRule1;
        private IconButton iconRule2;
        private RichTextBlock textRule2;
        private Panel panelFooter;
        private IconButton btnHotline;
        private IconButton btnDownload;

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
            this.boxHotline = new RoundedPanel();
            this.iconHotline = new IconButton();
            this.textHotline = new RichTextBlock();
            this.headingTours = new IconButton();
            this.tourHeritage = new TourCard();
            this.tourNight = new TourCard();
            this.tourFood = new TourCard();
            this.headingRules = new IconButton();
            this.boxRules = new RoundedPanel();
            this.iconRule1 = new IconButton();
            this.textRule1 = new RichTextBlock();
            this.iconRule2 = new IconButton();
            this.textRule2 = new RichTextBlock();
            this.panelFooter = new Panel();
            this.btnHotline = new IconButton();
            this.btnDownload = new IconButton();
            this.SuspendLayout();
            this.panelBody.SuspendLayout();
            this.panelFooter.SuspendLayout();

            // w-96 (384px) panel.
            this.BackColor = ThemeColors.SurfaceContainerLowest;
            this.Size = new Size(384, 560);

            this.header.IconName = "menu_book";
            this.header.Text = "Cẩm Nang & Hướng Dẫn Du Lịch Huế";
            this.header.Prominent = true;
            this.header.CloseClicked += new System.EventHandler(this.header_CloseClicked);

            // Footer: outlined hotline button + primary download button.
            this.panelFooter.Dock = DockStyle.Bottom;
            this.panelFooter.Height = 44;
            this.panelFooter.BackColor = ThemeColors.Slate50;
            this.btnHotline.Text = "Gọi hotline trợ giúp";
            this.btnHotline.IconName = "call";
            this.btnHotline.IconSize = 15;
            this.btnHotline.Font = ThemeFonts.Medium(12);
            this.btnHotline.ForeColor = ThemeColors.Slate700;
            this.btnHotline.IconColor = ThemeColors.Primary;
            this.btnHotline.FillColor = Color.White;
            this.btnHotline.HoverFillColor = ThemeColors.Slate100;
            this.btnHotline.BorderColor = ThemeColors.Slate200;
            this.btnHotline.Radius = 4;
            this.btnHotline.HorizontalPadding = 10;
            this.btnHotline.Gap = 4;
            this.btnHotline.Size = new Size(150, 28);
            this.btnHotline.Location = new Point(12, 8);
            this.btnDownload.Text = "Tải cẩm nang PDF";
            this.btnDownload.IconName = "download";
            this.btnDownload.IconSize = 15;
            this.btnDownload.Font = ThemeFonts.Medium(12);
            this.btnDownload.ForeColor = Color.White;
            this.btnDownload.IconColor = Color.White;
            this.btnDownload.FillColor = ThemeColors.Primary;
            this.btnDownload.HoverFillColor = ThemeColors.PrimaryContainer;
            this.btnDownload.Radius = 4;
            this.btnDownload.HorizontalPadding = 10;
            this.btnDownload.Gap = 4;
            this.btnDownload.Size = new Size(150, 28);
            this.btnDownload.Location = new Point(222, 8);
            this.panelFooter.Controls.Add(this.btnHotline);
            this.panelFooter.Controls.Add(this.btnDownload);

            this.panelBody.Dock = DockStyle.Fill;
            this.panelBody.AutoScroll = true;
            this.panelBody.BackColor = ThemeColors.SurfaceContainerLowest;

            // Hotline banner: amber-50 / amber-100.
            this.boxHotline.FillColor = ThemeColors.Amber50;
            this.boxHotline.BorderColor = ThemeColors.Amber100;
            this.boxHotline.Radius = 4;
            this.boxHotline.Size = new Size(356, 66);
            this.boxHotline.Location = new Point(14, 14);
            this.iconHotline.AutoWidth = false;
            this.iconHotline.IconName = "campaign";
            this.iconHotline.IconSize = 18;
            this.iconHotline.IconColor = ThemeColors.Amber600;
            this.iconHotline.FillColor = Color.Transparent;
            this.iconHotline.HoverFillColor = Color.Transparent;
            this.iconHotline.Cursor = Cursors.Default;
            this.iconHotline.Size = new Size(18, 18);
            this.iconHotline.Location = new Point(8, 8);
            this.textHotline.Text = "**Đường dây nóng hỗ trợ du khách: 0234.3828288** (Hỗ trợ khẩn cấp 24/7, phản ánh giá cả dịch vụ & tìm đồ thất lạc).";
            this.textHotline.Font = ThemeFonts.Regular(11);
            this.textHotline.BoldFont = ThemeFonts.SemiBold(11);
            this.textHotline.ForeColor = ThemeColors.Amber900;
            this.textHotline.BoldColor = ThemeColors.Amber700;
            this.textHotline.LineSpacing = 1.6f;
            this.textHotline.Size = new Size(314, 54);
            this.textHotline.Location = new Point(34, 6);
            this.boxHotline.Controls.Add(this.iconHotline);
            this.boxHotline.Controls.Add(this.textHotline);

            // Tours.
            this.headingTours.AutoWidth = true;
            this.headingTours.Text = "Gợi Ý Tour Trải Nghiệm Tiêu Biểu";
            this.headingTours.IconName = "route";
            this.headingTours.IconSize = 15;
            this.headingTours.Font = ThemeFonts.Bold(12);
            this.headingTours.ForeColor = ThemeColors.Slate800;
            this.headingTours.IconColor = ThemeColors.Primary;
            this.headingTours.FillColor = Color.Transparent;
            this.headingTours.HoverFillColor = Color.Transparent;
            this.headingTours.HorizontalPadding = 0;
            this.headingTours.Gap = 4;
            this.headingTours.Cursor = Cursors.Default;
            this.headingTours.Height = 20;
            this.headingTours.Location = new Point(14, 92);

            this.tourHeritage.Text = "Tour 1 ngày: Di Sản Cố Đô";
            this.tourHeritage.Description = "Ga Huế → Đại Nội → Chùa Thiên Mụ → Lăng Tự Đức";
            this.tourHeritage.Badge = "Khuyên dùng";
            this.tourHeritage.BadgeFillColor = ThemeColors.Sky100;
            this.tourHeritage.BadgeTextColor = ThemeColors.Primary;
            this.tourHeritage.Size = new Size(356, 52);
            this.tourHeritage.Location = new Point(14, 118);

            this.tourNight.Text = "Tour Đêm Sông Hương & Ca Huế";
            this.tourNight.Description = "Thuyền rồng Bến Tòa Khâm, thả hoa đăng ngắm cầu Trường Tiền";
            this.tourNight.Badge = "19:00 - 21:00";
            this.tourNight.BadgeFillColor = ColorTranslator.FromHtml("#D3F4FA");
            this.tourNight.BadgeTextColor = ThemeColors.Cyan600;
            this.tourNight.Size = new Size(356, 52);
            this.tourNight.Location = new Point(14, 176);

            this.tourFood.Text = "Tour Ẩm Thực Cố Đô Về Chiều";
            this.tourFood.Description = "Bún bò Mụ Rơi, bánh bèo nậm lọc Bà Đỏ, chè hẻm Hùng Vương";
            this.tourFood.Badge = "Ẩm thực";
            this.tourFood.BadgeFillColor = ThemeColors.Amber100;
            this.tourFood.BadgeTextColor = ThemeColors.Orange600;
            this.tourFood.Size = new Size(356, 52);
            this.tourFood.Location = new Point(14, 234);

            // Cultural rules.
            this.headingRules.AutoWidth = true;
            this.headingRules.Text = "Quy Định & Lưu Ý Văn Hóa Khi Tham Quan";
            this.headingRules.IconName = "check_box";
            this.headingRules.IconSize = 15;
            this.headingRules.Font = ThemeFonts.Bold(12);
            this.headingRules.ForeColor = ThemeColors.Slate800;
            this.headingRules.IconColor = ThemeColors.Primary;
            this.headingRules.FillColor = Color.Transparent;
            this.headingRules.HoverFillColor = Color.Transparent;
            this.headingRules.HorizontalPadding = 0;
            this.headingRules.Gap = 4;
            this.headingRules.Cursor = Cursors.Default;
            this.headingRules.Height = 20;
            this.headingRules.Location = new Point(14, 300);

            this.boxRules.FillColor = ThemeColors.Slate50;
            this.boxRules.BorderColor = ThemeColors.Slate200;
            this.boxRules.Radius = 4;
            this.boxRules.Size = new Size(356, 150);
            this.boxRules.Location = new Point(14, 326);
            this.iconRule1.AutoWidth = false;
            this.iconRule1.IconName = "check";
            this.iconRule1.IconSize = 14;
            this.iconRule1.IconColor = ThemeColors.Emerald700;
            this.iconRule1.FillColor = Color.Transparent;
            this.iconRule1.HoverFillColor = Color.Transparent;
            this.iconRule1.Cursor = Cursors.Default;
            this.iconRule1.Size = new Size(14, 14);
            this.iconRule1.Location = new Point(10, 12);
            this.textRule1.Text = "**Trang phục lịch sự:** Khi vào Điện Thái Hòa, Thế Miếu và Chùa Thiên Mụ cần mặc trang phục kín đáo (áo có tay, quần/váy dài qua gối).";
            this.textRule1.Font = ThemeFonts.Regular(11);
            this.textRule1.BoldFont = ThemeFonts.Bold(11);
            this.textRule1.ForeColor = ThemeColors.Slate700;
            this.textRule1.LineSpacing = 1.6f;
            this.textRule1.Size = new Size(322, 54);
            this.textRule1.Location = new Point(28, 8);
            this.iconRule2.AutoWidth = false;
            this.iconRule2.IconName = "check";
            this.iconRule2.IconSize = 14;
            this.iconRule2.IconColor = ThemeColors.Emerald700;
            this.iconRule2.FillColor = Color.Transparent;
            this.iconRule2.HoverFillColor = Color.Transparent;
            this.iconRule2.Cursor = Cursors.Default;
            this.iconRule2.Size = new Size(14, 14);
            this.iconRule2.Location = new Point(10, 78);
            this.textRule2.Text = "**Chụp ảnh & Bảo tồn di vật:** Không sờ tay vào hiện vật cổ, không bật đèn flash bên trong khu vực thờ tự các Vua triều Nguyễn.";
            this.textRule2.Font = ThemeFonts.Regular(11);
            this.textRule2.BoldFont = ThemeFonts.Bold(11);
            this.textRule2.ForeColor = ThemeColors.Slate700;
            this.textRule2.LineSpacing = 1.6f;
            this.textRule2.Size = new Size(322, 54);
            this.textRule2.Location = new Point(28, 74);
            this.boxRules.Controls.Add(this.iconRule1);
            this.boxRules.Controls.Add(this.textRule1);
            this.boxRules.Controls.Add(this.iconRule2);
            this.boxRules.Controls.Add(this.textRule2);

            this.panelBody.Controls.Add(this.boxRules);
            this.panelBody.Controls.Add(this.headingRules);
            this.panelBody.Controls.Add(this.tourFood);
            this.panelBody.Controls.Add(this.tourNight);
            this.panelBody.Controls.Add(this.tourHeritage);
            this.panelBody.Controls.Add(this.headingTours);
            this.panelBody.Controls.Add(this.boxHotline);

            this.Controls.Add(this.panelBody);
            this.Controls.Add(this.panelFooter);
            this.Controls.Add(this.header);
            this.Name = "TravelGuidePanel";
            this.panelFooter.ResumeLayout(false);
            this.panelBody.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
