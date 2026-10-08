using System.Drawing;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using Hue_travel.Theme;

namespace Hue_travel.Panels.BaseMapSwitcher
{
    partial class BaseMapSwitcherPopup
    {
        private System.ComponentModel.IContainer components = null;
        private IconButton captionTitle;
        private IconButton badgeCount;
        private Panel dividerTop;
        private BaseMapTile tileStandard;
        private BaseMapTile tileSatellite;
        private BaseMapTile tileNight;
        private BaseMapTile tileRoadmap;
        private BaseMapTile tileHybrid;
        private BaseMapTile tileTerrain;
        private Panel dividerBottom;
        private Label lblOpacityCaption;
        private Label lblOpacityValue;
        private Guna2TrackBar sliderOpacity;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        // Every property is assigned inline (no helper calls) so the WinForms Designer can render it.
        private void InitializeComponent()
        {
            this.captionTitle = new IconButton();
            this.badgeCount = new IconButton();
            this.dividerTop = new Panel();
            this.tileStandard = new BaseMapTile();
            this.tileSatellite = new BaseMapTile();
            this.tileNight = new BaseMapTile();
            this.tileRoadmap = new BaseMapTile();
            this.tileHybrid = new BaseMapTile();
            this.tileTerrain = new BaseMapTile();
            this.dividerBottom = new Panel();
            this.lblOpacityCaption = new Label();
            this.lblOpacityValue = new Label();
            this.sliderOpacity = new Guna2TrackBar();
            this.SuspendLayout();

            // w-64 (256px) popup.
            this.BackColor = ThemeColors.SurfaceContainerLowest;
            this.Size = new Size(256, 234);

            this.captionTitle.AutoWidth = false;
            this.captionTitle.Text = "CHỌN LỚP NỀN";
            this.captionTitle.IconName = "layers";
            this.captionTitle.IconSize = 14;
            this.captionTitle.Font = ThemeFonts.Bold(11);
            this.captionTitle.ForeColor = ThemeColors.Slate700;
            this.captionTitle.IconColor = ThemeColors.Primary;
            this.captionTitle.FillColor = Color.Transparent;
            this.captionTitle.HoverFillColor = Color.Transparent;
            this.captionTitle.HorizontalPadding = 0;
            this.captionTitle.Gap = 4;
            this.captionTitle.Cursor = Cursors.Default;
            this.captionTitle.Size = new Size(150, 18);
            this.captionTitle.Location = new Point(10, 10);

            this.badgeCount.AutoWidth = false;
            this.badgeCount.Text = "6 kiểu";
            this.badgeCount.Font = ThemeFonts.SemiBold(10);
            this.badgeCount.ForeColor = ThemeColors.Primary;
            this.badgeCount.FillColor = Color.Transparent;
            this.badgeCount.HoverFillColor = Color.Transparent;
            this.badgeCount.Cursor = Cursors.Default;
            this.badgeCount.Size = new Size(50, 18);
            this.badgeCount.Location = new Point(196, 10);

            this.dividerTop.BackColor = ThemeColors.Slate100;
            this.dividerTop.Size = new Size(236, 1);
            this.dividerTop.Location = new Point(10, 32);

            this.tileStandard.Text = "Chuẩn sáng";
            this.tileStandard.IconName = "map";
            this.tileStandard.IconColor = ThemeColors.Primary;
            this.tileStandard.PreviewColor = ThemeColors.Sky200;
            this.tileStandard.Selected = true;
            this.tileStandard.Size = new Size(72, 58);
            this.tileStandard.Location = new Point(10, 40);
            this.tileStandard.Click += new System.EventHandler(this.btnStandard_Click);

            this.tileSatellite.Text = "Vệ tinh";
            this.tileSatellite.IconName = "satellite_alt";
            this.tileSatellite.IconColor = Color.White;
            this.tileSatellite.PreviewColor = ColorTranslator.FromHtml("#065F46");
            this.tileSatellite.Size = new Size(72, 58);
            this.tileSatellite.Location = new Point(166, 40);
            this.tileSatellite.Click += new System.EventHandler(this.btnSatellite_Click);

            this.tileNight.Text = "Ngắm đêm";
            this.tileNight.IconName = "dark_mode";
            this.tileNight.IconColor = ColorTranslator.FromHtml("#FCD34D");
            this.tileNight.PreviewColor = ThemeColors.Slate800;
            this.tileNight.Size = new Size(72, 58);
            this.tileNight.Location = new Point(166, 106);
            this.tileNight.Click += new System.EventHandler(this.btnNight_Click);

            this.tileRoadmap.Text = "Đường phố";
            this.tileRoadmap.IconName = "directions";
            this.tileRoadmap.IconColor = ThemeColors.Primary;
            this.tileRoadmap.PreviewColor = ColorTranslator.FromHtml("#FEF3C7");
            this.tileRoadmap.Size = new Size(72, 58);
            this.tileRoadmap.Location = new Point(88, 40);
            this.tileRoadmap.Click += new System.EventHandler(this.btnRoadmap_Click);

            this.tileHybrid.Text = "Lai";
            this.tileHybrid.IconName = "layers";
            this.tileHybrid.IconColor = Color.White;
            this.tileHybrid.PreviewColor = ColorTranslator.FromHtml("#047857");
            this.tileHybrid.Size = new Size(72, 58);
            this.tileHybrid.Location = new Point(10, 106);
            this.tileHybrid.Click += new System.EventHandler(this.btnHybrid_Click);

            this.tileTerrain.Text = "Địa hình";
            this.tileTerrain.IconName = "explore";
            this.tileTerrain.IconColor = ColorTranslator.FromHtml("#78350F");
            this.tileTerrain.PreviewColor = ColorTranslator.FromHtml("#D9C8A0");
            this.tileTerrain.Size = new Size(72, 58);
            this.tileTerrain.Location = new Point(88, 106);
            this.tileTerrain.Click += new System.EventHandler(this.btnTerrain_Click);

            this.dividerBottom.BackColor = ThemeColors.Slate100;
            this.dividerBottom.Size = new Size(236, 1);
            this.dividerBottom.Location = new Point(10, 174);

            this.lblOpacityCaption.Text = "Độ mờ / sáng nền";
            this.lblOpacityCaption.Font = ThemeFonts.Medium(11);
            this.lblOpacityCaption.ForeColor = ThemeColors.Slate600;
            this.lblOpacityCaption.AutoSize = true;
            this.lblOpacityCaption.Location = new Point(10, 182);

            this.lblOpacityValue.Text = "85%";
            this.lblOpacityValue.Font = ThemeFonts.Bold(11);
            this.lblOpacityValue.ForeColor = ThemeColors.Primary;
            this.lblOpacityValue.AutoSize = false;
            this.lblOpacityValue.TextAlign = ContentAlignment.MiddleRight;
            this.lblOpacityValue.Size = new Size(50, 16);
            this.lblOpacityValue.Location = new Point(196, 182);

            this.sliderOpacity.Minimum = 20;
            this.sliderOpacity.Maximum = 100;
            this.sliderOpacity.Value = 85;
            this.sliderOpacity.ThumbColor = ThemeColors.Primary;
            this.sliderOpacity.FillColor = ThemeColors.Slate200;
            this.sliderOpacity.Size = new Size(236, 20);
            this.sliderOpacity.Location = new Point(10, 204);
            this.sliderOpacity.ValueChanged += new System.EventHandler(this.sliderOpacity_ValueChanged);

            this.Controls.Add(this.sliderOpacity);
            this.Controls.Add(this.lblOpacityValue);
            this.Controls.Add(this.lblOpacityCaption);
            this.Controls.Add(this.dividerBottom);
            this.Controls.Add(this.tileNight);
            this.Controls.Add(this.tileTerrain);
            this.Controls.Add(this.tileHybrid);
            this.Controls.Add(this.tileRoadmap);
            this.Controls.Add(this.tileSatellite);
            this.Controls.Add(this.tileStandard);
            this.Controls.Add(this.dividerTop);
            this.Controls.Add(this.badgeCount);
            this.Controls.Add(this.captionTitle);
            this.Name = "BaseMapSwitcherPopup";
            this.ResumeLayout(false);
        }
    }
}
