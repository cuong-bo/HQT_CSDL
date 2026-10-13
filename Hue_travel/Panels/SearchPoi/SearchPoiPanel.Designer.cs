using System.Drawing;
using System.Windows.Forms;
using Hue_travel.Theme;

namespace Hue_travel.Panels.SearchPoi
{
    partial class SearchPoiPanel
    {
        private System.ComponentModel.IContainer components = null;
        private PanelHeader header;
        private NoAutoScrollPanel panelBody;
        private RoundedPanel boxSearch;
        private IconButton iconSearch;
        private TextBox txtKeyword;
        private IconButton chipAll;
        private IconButton chipHeritage;
        private IconButton chipScenery;
        private IconButton chipFood;
        private IconButton chipStay;
        private IconButton chipService;
        private IconButton chipTransport;
        private RichTextBlock lblResultCount;
        private IconButton btnAdvancedFilter;
        private Panel panelFooter;
        private Label lblClear;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        // Every property is assigned inline (no helper calls) so the WinForms Designer can render it.
        // The result cards are created in code, one per sample POI.
        private void InitializeComponent()
        {
            this.header = new PanelHeader();
            this.panelBody = new NoAutoScrollPanel();
            this.boxSearch = new RoundedPanel();
            this.iconSearch = new IconButton();
            this.txtKeyword = new TextBox();
            this.chipAll = new IconButton();
            this.chipHeritage = new IconButton();
            this.chipScenery = new IconButton();
            this.chipFood = new IconButton();
            this.chipStay = new IconButton();
            this.chipService = new IconButton();
            this.chipTransport = new IconButton();
            this.lblResultCount = new RichTextBlock();
            this.btnAdvancedFilter = new IconButton();
            this.panelFooter = new Panel();
            this.lblClear = new Label();
            this.SuspendLayout();
            this.panelBody.SuspendLayout();
            this.panelFooter.SuspendLayout();

            // w-96 (384px) panel.
            this.BackColor = ThemeColors.SurfaceContainerLowest;
            this.Size = new Size(384, 480);

            this.header.IconName = "search";
            this.header.Text = "Tra Cứu Địa Điểm Du Lịch & Ẩm Thực Huế";
            this.header.Prominent = true;
            this.header.CloseClicked += new System.EventHandler(this.header_CloseClicked);

            // Footer: "Xóa tìm kiếm".
            this.panelFooter.Dock = DockStyle.Bottom;
            this.panelFooter.Height = 30;
            this.panelFooter.BackColor = ThemeColors.Slate50;
            this.lblClear.Text = "Xóa tìm kiếm";
            this.lblClear.Font = ThemeFonts.SemiBold(11);
            this.lblClear.ForeColor = ThemeColors.Primary;
            this.lblClear.Cursor = Cursors.Hand;
            this.lblClear.AutoSize = false;
            this.lblClear.TextAlign = ContentAlignment.MiddleRight;
            this.lblClear.Size = new Size(100, 30);
            this.lblClear.Location = new Point(272, 0);
            this.lblClear.Click += new System.EventHandler(this.lblClear_Click);
            this.panelFooter.Controls.Add(this.lblClear);

            this.panelBody.Dock = DockStyle.Fill;
            this.panelBody.AutoScroll = true;
            this.panelBody.BackColor = ThemeColors.SurfaceContainerLowest;

            // Search field: h-8, slate-50 / slate-200, search icon at left.
            this.boxSearch.FillColor = ThemeColors.Slate50;
            this.boxSearch.BorderColor = ThemeColors.Slate200;
            this.boxSearch.Size = new Size(340, 32);
            this.boxSearch.Location = new Point(12, 12);
            this.iconSearch.AutoWidth = false;
            this.iconSearch.IconName = "search";
            this.iconSearch.IconSize = 15;
            this.iconSearch.IconColor = ThemeColors.Slate400;
            this.iconSearch.FillColor = Color.Transparent;
            this.iconSearch.HoverFillColor = Color.Transparent;
            this.iconSearch.Cursor = Cursors.IBeam;
            this.iconSearch.Size = new Size(16, 16);
            this.iconSearch.Location = new Point(10, 8);
            this.txtKeyword.BorderStyle = BorderStyle.None;
            this.txtKeyword.BackColor = ThemeColors.Slate50;
            this.txtKeyword.ForeColor = ThemeColors.Slate800;
            this.txtKeyword.Font = ThemeFonts.Regular(12);
            this.txtKeyword.Text = string.Empty;
            this.txtKeyword.AutoSize = false;   // the height is set below; auto size would cut the tails of g, y, p
            this.txtKeyword.Size = new Size(296, 24);
            this.txtKeyword.Location = new Point(32, 4);
            this.txtKeyword.TextChanged += new System.EventHandler(this.txtKeyword_TextChanged);
            this.boxSearch.Controls.Add(this.iconSearch);
            this.boxSearch.Controls.Add(this.txtKeyword);

            // Category chips (11px), one per Hue group; several can be on at once. Placed in code (they wrap onto two rows).

            this.chipAll.Text = "Tất cả";
            this.chipAll.Font = ThemeFonts.SemiBold(11);
            this.chipAll.ForeColor = ThemeColors.Primary;
            this.chipAll.FillColor = ThemeColors.Sky100;
            this.chipAll.HoverFillColor = ThemeColors.Sky100;
            this.chipAll.BorderColor = ThemeColors.Sky200;
            this.chipAll.Radius = 4;
            this.chipAll.HorizontalPadding = 8;
            this.chipAll.Height = 22;
            this.chipAll.Location = new Point(12, 54);

            this.chipHeritage.Text = "Di tích";
            this.chipHeritage.Font = ThemeFonts.Medium(11);
            this.chipHeritage.ForeColor = ThemeColors.Slate600;
            this.chipHeritage.FillColor = ThemeColors.Slate100;
            this.chipHeritage.HoverFillColor = ThemeColors.Slate200;
            this.chipHeritage.BorderColor = ThemeColors.Slate200;
            this.chipHeritage.Radius = 4;
            this.chipHeritage.HorizontalPadding = 8;
            this.chipHeritage.Height = 22;
            this.chipHeritage.Location = new Point(12, 54);

            this.chipScenery.Text = "Cảnh quan";
            this.chipScenery.Font = ThemeFonts.Medium(11);
            this.chipScenery.ForeColor = ThemeColors.Slate600;
            this.chipScenery.FillColor = ThemeColors.Slate100;
            this.chipScenery.HoverFillColor = ThemeColors.Slate200;
            this.chipScenery.BorderColor = ThemeColors.Slate200;
            this.chipScenery.Radius = 4;
            this.chipScenery.HorizontalPadding = 8;
            this.chipScenery.Height = 22;
            this.chipScenery.Location = new Point(12, 54);

            this.chipFood.Text = "Ẩm thực";
            this.chipFood.Font = ThemeFonts.Medium(11);
            this.chipFood.ForeColor = ThemeColors.Slate600;
            this.chipFood.FillColor = ThemeColors.Slate100;
            this.chipFood.HoverFillColor = ThemeColors.Slate200;
            this.chipFood.BorderColor = ThemeColors.Slate200;
            this.chipFood.Radius = 4;
            this.chipFood.HorizontalPadding = 8;
            this.chipFood.Height = 22;
            this.chipFood.Location = new Point(12, 54);

            this.chipStay.Text = "Lưu trú";
            this.chipStay.Font = ThemeFonts.Medium(11);
            this.chipStay.ForeColor = ThemeColors.Slate600;
            this.chipStay.FillColor = ThemeColors.Slate100;
            this.chipStay.HoverFillColor = ThemeColors.Slate200;
            this.chipStay.BorderColor = ThemeColors.Slate200;
            this.chipStay.Radius = 4;
            this.chipStay.HorizontalPadding = 8;
            this.chipStay.Height = 22;
            this.chipStay.Location = new Point(12, 54);

            this.chipService.Text = "Dịch vụ";
            this.chipService.Font = ThemeFonts.Medium(11);
            this.chipService.ForeColor = ThemeColors.Slate600;
            this.chipService.FillColor = ThemeColors.Slate100;
            this.chipService.HoverFillColor = ThemeColors.Slate200;
            this.chipService.BorderColor = ThemeColors.Slate200;
            this.chipService.Radius = 4;
            this.chipService.HorizontalPadding = 8;
            this.chipService.Height = 22;
            this.chipService.Location = new Point(12, 54);

            this.chipTransport.Text = "Giao thông";
            this.chipTransport.Font = ThemeFonts.Medium(11);
            this.chipTransport.ForeColor = ThemeColors.Slate600;
            this.chipTransport.FillColor = ThemeColors.Slate100;
            this.chipTransport.HoverFillColor = ThemeColors.Slate200;
            this.chipTransport.BorderColor = ThemeColors.Slate200;
            this.chipTransport.Radius = 4;
            this.chipTransport.HorizontalPadding = 8;
            this.chipTransport.Height = 22;
            this.chipTransport.Location = new Point(12, 54);

            // Result count + advanced filter link.
            this.lblResultCount.Text = "Kết quả tìm kiếm: **0 địa điểm**";
            this.lblResultCount.Font = ThemeFonts.Regular(11);
            this.lblResultCount.BoldFont = ThemeFonts.Bold(11);
            this.lblResultCount.ForeColor = ThemeColors.Slate500;
            this.lblResultCount.LineSpacing = 1.4f;
            this.lblResultCount.Size = new Size(200, 18);
            this.lblResultCount.Location = new Point(13, 112);

            this.btnAdvancedFilter.AutoWidth = true;
            this.btnAdvancedFilter.Text = "Bộ lọc nâng cao";
            this.btnAdvancedFilter.IconName = "tune";
            this.btnAdvancedFilter.IconSize = 13;
            this.btnAdvancedFilter.Font = ThemeFonts.Medium(11);
            this.btnAdvancedFilter.ForeColor = ThemeColors.Primary;
            this.btnAdvancedFilter.IconColor = ThemeColors.Primary;
            this.btnAdvancedFilter.FillColor = Color.Transparent;
            this.btnAdvancedFilter.HoverFillColor = Color.Transparent;
            this.btnAdvancedFilter.HorizontalPadding = 0;
            this.btnAdvancedFilter.Gap = 2;
            this.btnAdvancedFilter.Height = 18;
            this.btnAdvancedFilter.Location = new Point(240, 112);

            this.panelBody.Controls.Add(this.btnAdvancedFilter);
            this.panelBody.Controls.Add(this.lblResultCount);
            this.panelBody.Controls.Add(this.chipTransport);
            this.panelBody.Controls.Add(this.chipService);
            this.panelBody.Controls.Add(this.chipStay);
            this.panelBody.Controls.Add(this.chipFood);
            this.panelBody.Controls.Add(this.chipScenery);
            this.panelBody.Controls.Add(this.chipHeritage);
            this.panelBody.Controls.Add(this.chipAll);
            this.panelBody.Controls.Add(this.boxSearch);

            this.Controls.Add(this.panelBody);
            this.Controls.Add(this.panelFooter);
            this.Controls.Add(this.header);
            this.Name = "SearchPoiPanel";
            this.panelFooter.ResumeLayout(false);
            this.panelBody.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
