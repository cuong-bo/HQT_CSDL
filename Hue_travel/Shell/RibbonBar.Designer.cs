using Guna.UI2.WinForms;
using Hue_travel.Theme;

namespace Hue_travel.Shell
{
    partial class RibbonBar
    {
        private System.ComponentModel.IContainer components = null;
        private Guna2Panel panelRoot;
        private Guna2Button btnLayerControl;
        private Guna2Button btnRouteOptimizer;
        private Guna2Button btnSearchPoi;
        private Guna2Button btnTravelGuide;
        private Guna2Button btnZoomIn;
        private Guna2Button btnZoomOut;
        private Guna2Button btnBaseMap;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.panelRoot = new Guna2Panel();
            this.btnLayerControl = new Guna2Button();
            this.btnRouteOptimizer = new Guna2Button();
            this.btnSearchPoi = new Guna2Button();
            this.btnTravelGuide = new Guna2Button();
            this.btnZoomIn = new Guna2Button();
            this.btnZoomOut = new Guna2Button();
            this.btnBaseMap = new Guna2Button();
            this.SuspendLayout();

            this.panelRoot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelRoot.FillColor = ThemeColors.SurfaceContainerLowest;
            this.panelRoot.BorderRadius = 0;

            this.btnLayerControl.Text = "Danh mục địa điểm";
            this.btnLayerControl.Location = new System.Drawing.Point(8, 4);
            this.btnLayerControl.Size = new System.Drawing.Size(140, 32);
            this.btnLayerControl.Font = ThemeFonts.LabelSm;
            this.btnLayerControl.FillColor = ThemeColors.SurfaceContainer;
            this.btnLayerControl.ForeColor = ThemeColors.Primary;
            this.btnLayerControl.BorderColor = ThemeColors.SurfaceContainerHigh;
            this.btnLayerControl.BorderThickness = 1;
            this.btnLayerControl.BorderRadius = ThemeMetrics.RadiusSm;
            this.btnLayerControl.Click += new System.EventHandler(this.btnLayerControl_Click);

            this.btnRouteOptimizer.Text = "Tìm đường ngắn nhất";
            this.btnRouteOptimizer.Location = new System.Drawing.Point(156, 4);
            this.btnRouteOptimizer.Size = new System.Drawing.Size(140, 32);
            this.btnRouteOptimizer.Font = ThemeFonts.LabelSm;
            this.btnRouteOptimizer.FillColor = ThemeColors.SurfaceContainer;
            this.btnRouteOptimizer.ForeColor = ThemeColors.Primary;
            this.btnRouteOptimizer.BorderColor = ThemeColors.SurfaceContainerHigh;
            this.btnRouteOptimizer.BorderThickness = 1;
            this.btnRouteOptimizer.BorderRadius = ThemeMetrics.RadiusSm;
            this.btnRouteOptimizer.Click += new System.EventHandler(this.btnRouteOptimizer_Click);

            this.btnSearchPoi.Text = "Tra cứu địa điểm";
            this.btnSearchPoi.Location = new System.Drawing.Point(304, 4);
            this.btnSearchPoi.Size = new System.Drawing.Size(140, 32);
            this.btnSearchPoi.Font = ThemeFonts.LabelSm;
            this.btnSearchPoi.FillColor = ThemeColors.SurfaceContainer;
            this.btnSearchPoi.ForeColor = ThemeColors.Primary;
            this.btnSearchPoi.BorderColor = ThemeColors.SurfaceContainerHigh;
            this.btnSearchPoi.BorderThickness = 1;
            this.btnSearchPoi.BorderRadius = ThemeMetrics.RadiusSm;
            this.btnSearchPoi.Click += new System.EventHandler(this.btnSearchPoi_Click);

            this.btnTravelGuide.Text = "Hướng dẫn du lịch";
            this.btnTravelGuide.Location = new System.Drawing.Point(452, 4);
            this.btnTravelGuide.Size = new System.Drawing.Size(140, 32);
            this.btnTravelGuide.Font = ThemeFonts.LabelSm;
            this.btnTravelGuide.FillColor = ThemeColors.SurfaceContainer;
            this.btnTravelGuide.ForeColor = ThemeColors.Primary;
            this.btnTravelGuide.BorderColor = ThemeColors.SurfaceContainerHigh;
            this.btnTravelGuide.BorderThickness = 1;
            this.btnTravelGuide.BorderRadius = ThemeMetrics.RadiusSm;
            this.btnTravelGuide.Click += new System.EventHandler(this.btnTravelGuide_Click);

            this.btnZoomIn.Text = "Phóng to (+)";
            this.btnZoomIn.Location = new System.Drawing.Point(608, 4);
            this.btnZoomIn.Size = new System.Drawing.Size(140, 32);
            this.btnZoomIn.Font = ThemeFonts.LabelSm;
            this.btnZoomIn.FillColor = ThemeColors.SurfaceContainerLowest;
            this.btnZoomIn.ForeColor = ThemeColors.OnSurfaceVariant;
            this.btnZoomIn.BorderColor = ThemeColors.SurfaceContainerHigh;
            this.btnZoomIn.BorderThickness = 1;
            this.btnZoomIn.BorderRadius = ThemeMetrics.RadiusSm;
            this.btnZoomIn.Click += new System.EventHandler(this.btnZoomIn_Click);

            this.btnZoomOut.Text = "Thu nhỏ (-)";
            this.btnZoomOut.Location = new System.Drawing.Point(756, 4);
            this.btnZoomOut.Size = new System.Drawing.Size(140, 32);
            this.btnZoomOut.Font = ThemeFonts.LabelSm;
            this.btnZoomOut.FillColor = ThemeColors.SurfaceContainerLowest;
            this.btnZoomOut.ForeColor = ThemeColors.OnSurfaceVariant;
            this.btnZoomOut.BorderColor = ThemeColors.SurfaceContainerHigh;
            this.btnZoomOut.BorderThickness = 1;
            this.btnZoomOut.BorderRadius = ThemeMetrics.RadiusSm;
            this.btnZoomOut.Click += new System.EventHandler(this.btnZoomOut_Click);

            this.btnBaseMap.Text = "Bản đồ nền ▾";
            this.btnBaseMap.Location = new System.Drawing.Point(904, 4);
            this.btnBaseMap.Size = new System.Drawing.Size(140, 32);
            this.btnBaseMap.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnBaseMap.Font = ThemeFonts.LabelSm;
            this.btnBaseMap.FillColor = ThemeColors.SurfaceContainer;
            this.btnBaseMap.ForeColor = ThemeColors.Primary;
            this.btnBaseMap.BorderColor = ThemeColors.SurfaceContainerHigh;
            this.btnBaseMap.BorderThickness = 1;
            this.btnBaseMap.BorderRadius = ThemeMetrics.RadiusSm;
            this.btnBaseMap.Click += new System.EventHandler(this.btnBaseMap_Click);

            this.panelRoot.Controls.Add(this.btnLayerControl);
            this.panelRoot.Controls.Add(this.btnRouteOptimizer);
            this.panelRoot.Controls.Add(this.btnSearchPoi);
            this.panelRoot.Controls.Add(this.btnTravelGuide);
            this.panelRoot.Controls.Add(this.btnZoomIn);
            this.panelRoot.Controls.Add(this.btnZoomOut);
            this.panelRoot.Controls.Add(this.btnBaseMap);

            this.Controls.Add(this.panelRoot);
            this.Name = "RibbonBar";
            this.Size = new System.Drawing.Size(1280, 40);
            this.ResumeLayout(false);
        }
    }
}
