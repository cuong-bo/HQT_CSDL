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
            this.stackBaseMaps = new MapToolStack();
            this.compass = new CompassButton();
            this.stackZoom = new MapToolStack();
            this.attribution = new Label();
            ((System.ComponentModel.ISupportInitialize)(this.axMap)).BeginInit();
            this.SuspendLayout();

            this.axMap.Dock = DockStyle.Fill;
            this.axMap.Name = "axMap";
            this.axMap.TabIndex = 0;

            // Right column (top-16 right-4): base map switch, compass, zoom/pan.
            this.stackBaseMaps.IconList = "map,satellite_alt,layers,explore";
            this.stackBaseMaps.ToolTipList = "Bản đồ đường phố|Bản đồ ảnh vệ tinh|Bản đồ lai (vệ tinh và tên đường)|Bản đồ địa hình";
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
            this.Controls.Add(this.axMap);
            this.Name = "MapHost";
            this.Size = new Size(1080, 700);

            ((System.ComponentModel.ISupportInitialize)(this.axMap)).EndInit();
            this.ResumeLayout(false);
        }
    }
}
