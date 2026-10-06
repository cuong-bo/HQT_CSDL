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

        private Guna2Button MakeButton(string text, int x)
        {
            var btn = new Guna2Button
            {
                Text = text,
                Location = new System.Drawing.Point(x, 4),
                Size = new System.Drawing.Size(140, 32),
                Font = ThemeFonts.LabelSm,
                FillColor = ThemeColors.SurfaceContainer,
                ForeColor = ThemeColors.Primary,
                BorderColor = ThemeColors.SurfaceContainerHigh,
                BorderThickness = 1,
                BorderRadius = ThemeMetrics.RadiusSm
            };
            return btn;
        }

        private void InitializeComponent()
        {
            this.panelRoot = new Guna2Panel();
            this.SuspendLayout();

            this.panelRoot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelRoot.FillColor = ThemeColors.SurfaceContainerLowest;
            this.panelRoot.BorderRadius = 0;

            this.btnLayerControl = MakeButton("Danh mục địa điểm", 8);
            this.btnLayerControl.Click += (s, e) => LayerControlToggled?.Invoke(this, System.EventArgs.Empty);

            this.btnRouteOptimizer = MakeButton("Tìm đường ngắn nhất", 156);
            this.btnRouteOptimizer.Click += (s, e) => RouteOptimizerToggled?.Invoke(this, System.EventArgs.Empty);

            this.btnSearchPoi = MakeButton("Tra cứu địa điểm", 304);
            this.btnSearchPoi.Click += (s, e) => SearchPoiToggled?.Invoke(this, System.EventArgs.Empty);

            this.btnTravelGuide = MakeButton("Hướng dẫn du lịch", 452);
            this.btnTravelGuide.Click += (s, e) => TravelGuideToggled?.Invoke(this, System.EventArgs.Empty);

            this.btnZoomIn = MakeButton("Phóng to (+)", 608);
            this.btnZoomIn.FillColor = ThemeColors.SurfaceContainerLowest;
            this.btnZoomIn.ForeColor = ThemeColors.OnSurfaceVariant;
            this.btnZoomIn.Click += (s, e) => ZoomInClicked?.Invoke(this, System.EventArgs.Empty);

            this.btnZoomOut = MakeButton("Thu nhỏ (-)", 756);
            this.btnZoomOut.FillColor = ThemeColors.SurfaceContainerLowest;
            this.btnZoomOut.ForeColor = ThemeColors.OnSurfaceVariant;
            this.btnZoomOut.Click += (s, e) => ZoomOutClicked?.Invoke(this, System.EventArgs.Empty);

            this.btnBaseMap = MakeButton("Bản đồ nền ▾", 904);
            this.btnBaseMap.Click += (s, e) => BaseMapSwitcherToggled?.Invoke(this, System.EventArgs.Empty);

            this.panelRoot.Controls.Add(this.btnLayerControl);
            this.panelRoot.Controls.Add(this.btnRouteOptimizer);
            this.panelRoot.Controls.Add(this.btnSearchPoi);
            this.panelRoot.Controls.Add(this.btnTravelGuide);
            this.panelRoot.Controls.Add(this.btnZoomIn);
            this.panelRoot.Controls.Add(this.btnZoomOut);
            this.panelRoot.Controls.Add(this.btnBaseMap);

            this.Controls.Add(this.panelRoot);
            this.Name = "RibbonBar";
            this.Size = new System.Drawing.Size(1000, 40);
            this.ResumeLayout(false);
        }
    }
}
