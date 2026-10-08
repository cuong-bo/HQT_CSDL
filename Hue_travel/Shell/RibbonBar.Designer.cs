using System.Drawing;
using System.Windows.Forms;
using Hue_travel.Theme;

namespace Hue_travel.Shell
{
    partial class RibbonBar
    {
        private System.ComponentModel.IContainer components = null;
        private FlowLayoutPanel flowItems;
        private IconButton btnRouteOptimizer;
        private IconButton btnSearchPoi;
        private IconButton btnTravelGuide;
        private Panel separatorTools;
        private IconButton btnZoomIn;
        private IconButton btnZoomOut;
        private IconButton btnZoomFull;
        private IconButton btnMeasure;
        private IconButton btnMeasureArea;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        // Every property is assigned inline (no helper calls) so the WinForms Designer can render it.
        private void InitializeComponent()
        {
            this.flowItems = new FlowLayoutPanel();
            this.btnRouteOptimizer = new IconButton();
            this.btnSearchPoi = new IconButton();
            this.btnTravelGuide = new IconButton();
            this.separatorTools = new Panel();
            this.btnZoomIn = new IconButton();
            this.btnZoomOut = new IconButton();
            this.btnZoomFull = new IconButton();
            this.btnMeasure = new IconButton();
            this.btnMeasureArea = new IconButton();
            this.SuspendLayout();
            this.flowItems.SuspendLayout();

            // h-10 white bar; items are h-7 (28px) with a 6px flex gap (3px margin each side).
            this.BackColor = ThemeColors.SurfaceContainerLowest;

            this.flowItems.Dock = DockStyle.Fill;
            this.flowItems.FlowDirection = FlowDirection.LeftToRight;
            this.flowItems.WrapContents = false;
            this.flowItems.Padding = new Padding(9, 0, 0, 0);
            this.flowItems.BackColor = ThemeColors.SurfaceContainerLowest;

            // --- Primary buttons: sky-50 fill, sky-200 border, sky-800 text, primary icon ---

            this.btnRouteOptimizer.Text = "Tìm đường ngắn nhất";
            this.btnRouteOptimizer.IconName = "alt_route";
            this.btnRouteOptimizer.IconSize = 16;
            this.btnRouteOptimizer.Font = ThemeFonts.SemiBold(12);
            this.btnRouteOptimizer.ForeColor = ThemeColors.Sky800;
            this.btnRouteOptimizer.IconColor = ThemeColors.Primary;
            this.btnRouteOptimizer.FillColor = ThemeColors.Sky50;
            this.btnRouteOptimizer.HoverFillColor = ThemeColors.Sky100;
            this.btnRouteOptimizer.BorderColor = ThemeColors.Sky200;
            this.btnRouteOptimizer.Radius = 6;
            this.btnRouteOptimizer.HorizontalPadding = 10;
            this.btnRouteOptimizer.Gap = 6;
            this.btnRouteOptimizer.Size = new Size(150, 28);
            this.btnRouteOptimizer.Margin = new Padding(3, 6, 3, 6);
            this.btnRouteOptimizer.Click += new System.EventHandler(this.btnRouteOptimizer_Click);

            this.btnSearchPoi.Text = "Tra cứu địa điểm";
            this.btnSearchPoi.IconName = "search";
            this.btnSearchPoi.IconSize = 16;
            this.btnSearchPoi.Font = ThemeFonts.SemiBold(12);
            this.btnSearchPoi.ForeColor = ThemeColors.Sky800;
            this.btnSearchPoi.IconColor = ThemeColors.Primary;
            this.btnSearchPoi.FillColor = ThemeColors.Sky50;
            this.btnSearchPoi.HoverFillColor = ThemeColors.Sky100;
            this.btnSearchPoi.BorderColor = ThemeColors.Sky200;
            this.btnSearchPoi.Radius = 6;
            this.btnSearchPoi.HorizontalPadding = 10;
            this.btnSearchPoi.Gap = 6;
            this.btnSearchPoi.Size = new Size(140, 28);
            this.btnSearchPoi.Margin = new Padding(3, 6, 3, 6);
            this.btnSearchPoi.Click += new System.EventHandler(this.btnSearchPoi_Click);

            this.btnTravelGuide.Text = "Hướng dẫn du lịch";
            this.btnTravelGuide.IconName = "menu_book";
            this.btnTravelGuide.IconSize = 16;
            this.btnTravelGuide.Font = ThemeFonts.SemiBold(12);
            this.btnTravelGuide.ForeColor = ThemeColors.Sky800;
            this.btnTravelGuide.IconColor = ThemeColors.Primary;
            this.btnTravelGuide.FillColor = ThemeColors.Sky50;
            this.btnTravelGuide.HoverFillColor = ThemeColors.Sky100;
            this.btnTravelGuide.BorderColor = ThemeColors.Sky200;
            this.btnTravelGuide.Radius = 6;
            this.btnTravelGuide.HorizontalPadding = 10;
            this.btnTravelGuide.Gap = 6;
            this.btnTravelGuide.Size = new Size(145, 28);
            this.btnTravelGuide.Margin = new Padding(3, 6, 3, 6);
            this.btnTravelGuide.Click += new System.EventHandler(this.btnTravelGuide_Click);

            // --- h-4 w-px bg-slate-200 mx-1 ---
            this.separatorTools.BackColor = ThemeColors.Slate200;
            this.separatorTools.Size = new Size(1, 16);
            this.separatorTools.Margin = new Padding(7, 12, 7, 12);

            // --- Ghost buttons: no fill, slate text, hover slate-100 ---
            this.btnZoomIn.Text = "Phóng to (+)";
            this.btnZoomIn.IconName = "zoom_in";
            this.btnZoomIn.IconSize = 16;
            this.btnZoomIn.Font = ThemeFonts.Medium(12);
            this.btnZoomIn.ForeColor = ThemeColors.Slate700;
            this.btnZoomIn.IconColor = ThemeColors.Slate600;
            this.btnZoomIn.FillColor = Color.Transparent;
            this.btnZoomIn.HoverFillColor = ThemeColors.Slate100;
            this.btnZoomIn.Radius = 4;
            this.btnZoomIn.HorizontalPadding = 8;
            this.btnZoomIn.Gap = 4;
            this.btnZoomIn.Size = new Size(105, 28);
            this.btnZoomIn.Margin = new Padding(3, 6, 3, 6);
            this.btnZoomIn.Click += new System.EventHandler(this.btnZoomIn_Click);

            this.btnZoomOut.Text = "Thu nhỏ (-)";
            this.btnZoomOut.IconName = "zoom_out";
            this.btnZoomOut.IconSize = 16;
            this.btnZoomOut.Font = ThemeFonts.Medium(12);
            this.btnZoomOut.ForeColor = ThemeColors.Slate700;
            this.btnZoomOut.IconColor = ThemeColors.Slate600;
            this.btnZoomOut.FillColor = Color.Transparent;
            this.btnZoomOut.HoverFillColor = ThemeColors.Slate100;
            this.btnZoomOut.Radius = 4;
            this.btnZoomOut.HorizontalPadding = 8;
            this.btnZoomOut.Gap = 4;
            this.btnZoomOut.Size = new Size(100, 28);
            this.btnZoomOut.Margin = new Padding(3, 6, 3, 6);
            this.btnZoomOut.Click += new System.EventHandler(this.btnZoomOut_Click);

            this.btnZoomFull.Text = "Xem toàn cảnh Huế";
            this.btnZoomFull.IconName = "travel_explore";
            this.btnZoomFull.IconSize = 16;
            this.btnZoomFull.Font = ThemeFonts.Medium(12);
            this.btnZoomFull.ForeColor = ThemeColors.Slate700;
            this.btnZoomFull.IconColor = ThemeColors.Slate600;
            this.btnZoomFull.FillColor = Color.Transparent;
            this.btnZoomFull.HoverFillColor = ThemeColors.Slate100;
            this.btnZoomFull.Radius = 4;
            this.btnZoomFull.HorizontalPadding = 8;
            this.btnZoomFull.Gap = 4;
            this.btnZoomFull.Size = new Size(150, 28);
            this.btnZoomFull.Margin = new Padding(3, 6, 3, 6);
            this.btnZoomFull.Click += new System.EventHandler(this.btnZoomFull_Click);

            this.btnMeasure.Text = "Đo khoảng cách";
            this.btnMeasure.IconName = "straighten";
            this.btnMeasure.IconSize = 16;
            this.btnMeasure.Font = ThemeFonts.Medium(12);
            this.btnMeasure.ForeColor = ThemeColors.Slate700;
            this.btnMeasure.IconColor = ThemeColors.Slate600;
            this.btnMeasure.FillColor = Color.Transparent;
            this.btnMeasure.HoverFillColor = ThemeColors.Slate100;
            this.btnMeasure.Radius = 4;
            this.btnMeasure.HorizontalPadding = 8;
            this.btnMeasure.Gap = 4;
            this.btnMeasure.Size = new Size(130, 28);
            this.btnMeasure.Margin = new Padding(3, 6, 3, 6);
            this.btnMeasure.Click += new System.EventHandler(this.btnMeasure_Click);

            this.btnMeasureArea.Text = "\u0110o di\u1EC7n t\u00EDch";
            this.btnMeasureArea.IconName = "measure_area";
            this.btnMeasureArea.IconSize = 16;
            this.btnMeasureArea.Font = ThemeFonts.Medium(12);
            this.btnMeasureArea.ForeColor = ThemeColors.Slate700;
            this.btnMeasureArea.IconColor = ThemeColors.Slate600;
            this.btnMeasureArea.FillColor = Color.Transparent;
            this.btnMeasureArea.HoverFillColor = ThemeColors.Slate100;
            this.btnMeasureArea.Radius = 4;
            this.btnMeasureArea.HorizontalPadding = 8;
            this.btnMeasureArea.Gap = 4;
            this.btnMeasureArea.Size = new Size(120, 28);
            this.btnMeasureArea.Margin = new Padding(3, 6, 3, 6);
            this.btnMeasureArea.Click += new System.EventHandler(this.btnMeasureArea_Click);


            this.flowItems.Controls.Add(this.btnRouteOptimizer);
            this.flowItems.Controls.Add(this.btnSearchPoi);
            this.flowItems.Controls.Add(this.btnTravelGuide);
            this.flowItems.Controls.Add(this.separatorTools);
            this.flowItems.Controls.Add(this.btnZoomIn);
            this.flowItems.Controls.Add(this.btnZoomOut);
            this.flowItems.Controls.Add(this.btnZoomFull);
            this.flowItems.Controls.Add(this.btnMeasure);
            this.flowItems.Controls.Add(this.btnMeasureArea);

            this.Controls.Add(this.flowItems);
            this.Name = "RibbonBar";
            this.Size = new Size(1280, 40);
            this.flowItems.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
