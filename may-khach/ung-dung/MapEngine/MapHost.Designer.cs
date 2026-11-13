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
        private CompassButton compass;
        private MapToolStack stackZoom;
        private MapToolStack stackTools;

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
            this.compass = new CompassButton();
            this.stackZoom = new MapToolStack();
            this.stackTools = new MapToolStack();
            ((System.ComponentModel.ISupportInitialize)(this.axMap)).BeginInit();
            this.SuspendLayout();

            this.axMap.Dock = DockStyle.Fill;
            this.axMap.Name = "axMap";
            this.axMap.TabIndex = 0;

            // Right column (top-16 right-4): compass, zoom/pan.
            this.compass.Location = new Point(1021, 64);
            this.compass.Click += new System.EventHandler(this.compass_Click);

            this.stackZoom.IconList = "add,remove,pan_tool";
            this.stackZoom.ToolTipList = "Phóng to|Thu nhỏ|Di chuyển bàn tay";
            this.stackZoom.ActiveIndex = 2;
            this.stackZoom.ActiveFill = ThemeColors.Sky50;
            this.stackZoom.ActiveIconColor = ThemeColors.Sky600;
            this.stackZoom.Location = new Point(1020, 226);
            this.stackZoom.ItemClicked += new System.EventHandler<int>(this.stackZoom_ItemClicked);

            // Whole-city view and measuring, moved here from the old ribbon.
            this.stackTools.IconList = "travel_explore,straighten,measure_area";
            this.stackTools.ToolTipList = "Xem toàn cảnh Huế|Đo khoảng cách|Đo diện tích";
            this.stackTools.ActiveIndex = -1;
            this.stackTools.ActiveFill = ThemeColors.Sky50;
            this.stackTools.ActiveIconColor = ThemeColors.Sky600;
            this.stackTools.Location = new Point(1020, 350);
            this.stackTools.ItemClicked += new System.EventHandler<int>(this.stackTools_ItemClicked);

            this.Controls.Add(this.stackTools);
            this.Controls.Add(this.stackZoom);
            this.Controls.Add(this.compass);
            this.Controls.Add(this.axMap);
            this.Name = "MapHost";
            this.Size = new Size(1080, 700);

            ((System.ComponentModel.ISupportInitialize)(this.axMap)).EndInit();
            this.ResumeLayout(false);
        }
    }
}
