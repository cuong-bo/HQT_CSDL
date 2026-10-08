using System.Drawing;
using System.Windows.Forms;
using AxInterop.MapWinGIS;
using Hue_travel.MapEngine.Overlays;
using Hue_travel.Theme;

namespace Hue_travel.MapEngine
{
    partial class MapHost
    {
        private System.ComponentModel.IContainer components = null;
        private AxMap axMap;
        private Label attribution;
        private RoundedPanel boxInfo;
        private IconButton captionInfo;
        private IconButton tagInfo;
        private MapToolStack stackBaseMaps;
        private CompassButton compass;
        private MapToolStack stackZoom;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        // Every property is assigned inline (no helper calls) so the WinForms Designer can render it.
        private void InitializeComponent()
        {
            this.axMap = new AxMap();
            this.boxInfo = new RoundedPanel();
            this.captionInfo = new IconButton();
            this.tagInfo = new IconButton();
            this.stackBaseMaps = new MapToolStack();
            this.compass = new CompassButton();
            this.stackZoom = new MapToolStack();
            this.attribution = new Label();
            ((System.ComponentModel.ISupportInitialize)(this.axMap)).BeginInit();
            this.SuspendLayout();

            this.axMap.Dock = DockStyle.Fill;
            this.axMap.Name = "axMap";
            this.axMap.TabIndex = 0;

            // Top info pill: white, sky-100 border, explore icon + hint + "route shown" tag.
            this.boxInfo.FillColor = Color.White;
            this.boxInfo.BorderColor = ThemeColors.Sky100;
            this.boxInfo.Radius = 16;
            this.boxInfo.Size = new Size(680, 32);
            this.boxInfo.Location = new Point(240, 12);

            this.captionInfo.AutoWidth = false;
            this.captionInfo.Text = "Bản đồ du lịch Cố đô Huế - Nhấn vào các địa điểm để xem thông tin chi tiết";
            this.captionInfo.IconName = "explore";
            this.captionInfo.IconSize = 18;
            this.captionInfo.IconColor = ThemeColors.Sky600;
            this.captionInfo.Font = ThemeFonts.Medium(12);
            this.captionInfo.ForeColor = ThemeColors.Slate700;
            this.captionInfo.FillColor = Color.Transparent;
            this.captionInfo.HoverFillColor = Color.Transparent;
            this.captionInfo.HorizontalPadding = 0;
            this.captionInfo.Gap = 8;
            this.captionInfo.Cursor = Cursors.Default;
            this.captionInfo.Size = new Size(410, 28);
            this.captionInfo.Location = new Point(16, 2);

            this.tagInfo.AutoWidth = true;
            this.tagInfo.Text = "Đang hiển thị lộ trình";
            this.tagInfo.Font = ThemeFonts.SemiBold(11);
            this.tagInfo.ForeColor = ThemeColors.Sky600;
            this.tagInfo.FillColor = ThemeColors.Sky50;
            this.tagInfo.HoverFillColor = ThemeColors.Sky50;
            this.tagInfo.Radius = 12;
            this.tagInfo.HorizontalPadding = 8;
            this.tagInfo.Cursor = Cursors.Default;
            this.tagInfo.Height = 22;
            this.tagInfo.Location = new Point(430, 5);
            this.boxInfo.Controls.Add(this.captionInfo);
            this.boxInfo.Controls.Add(this.tagInfo);

            // Right column (top-16 right-4): base map switch, compass, zoom/pan.
            this.stackBaseMaps.IconList = "map,satellite_alt,dark_mode";
            this.stackBaseMaps.ToolTipList = "Bản đồ chuẩn sáng|Bản đồ ảnh vệ tinh|Bản đồ ngắm đêm Cố Đô";
            this.stackBaseMaps.ActiveIndex = 0;
            this.stackBaseMaps.Location = new Point(1020, 64);
            this.stackBaseMaps.ItemClicked += new System.EventHandler<int>(this.stackBaseMaps_ItemClicked);

            this.compass.Location = new Point(1021, 184);
            this.compass.Click += new System.EventHandler(this.compass_Click);

            this.stackZoom.IconList = "add,remove,pan_tool";
            this.stackZoom.ToolTipList = "Phóng to|Thu nhỏ|Di chuyển bàn tay";
            this.stackZoom.ActiveIndex = 2;
            this.stackZoom.ActiveFill = ThemeColors.Sky50;
            this.stackZoom.ActiveIconColor = ThemeColors.Sky600;
            this.stackZoom.Location = new Point(1020, 226);
            this.stackZoom.ItemClicked += new System.EventHandler<int>(this.stackZoom_ItemClicked);

            // Credit line for tile base maps (bottom-right, positioned in LayoutOverlays).
            this.attribution.AutoSize = true;
            this.attribution.Visible = false;
            this.attribution.UseMnemonic = false;
            this.attribution.Font = ThemeFonts.Regular(10);
            this.attribution.ForeColor = ThemeColors.Slate600;
            this.attribution.BackColor = ThemeColors.Slate100;
            this.attribution.Padding = new Padding(4, 1, 4, 1);

            this.Controls.Add(this.attribution);
            this.Controls.Add(this.stackZoom);
            this.Controls.Add(this.compass);
            this.Controls.Add(this.stackBaseMaps);
            this.Controls.Add(this.boxInfo);
            this.Controls.Add(this.axMap);
            this.Name = "MapHost";
            this.Size = new Size(1080, 700);

            ((System.ComponentModel.ISupportInitialize)(this.axMap)).EndInit();
            this.ResumeLayout(false);
        }
    }
}
