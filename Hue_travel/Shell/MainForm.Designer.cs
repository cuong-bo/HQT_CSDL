using Hue_travel.MapEngine;
using Hue_travel.Panels.BaseMapSwitcher;
using Hue_travel.Panels.LayerControl;
using Hue_travel.Panels.PoiIdentify;
using Hue_travel.Panels.RouteOptimizer;
using Hue_travel.Panels.SearchPoi;
using Hue_travel.Panels.TravelGuide;
using Hue_travel.Auth;
using Hue_travel.Theme;

namespace Hue_travel.Shell
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        private TitleBar titleBar;
        private RibbonBar ribbonBar;
        private CategoryRail categoryRail;
        private AppStatusBar statusBar;
        private MapHost mapHost;

        private LayerControlPanel layerControlPanel;
        private RouteOptimizerPanel routeOptimizerPanel;
        private SearchPoiPanel searchPoiPanel;
        private TravelGuidePanel travelGuidePanel;
        private PoiIdentifyCard poiIdentifyCard;
        private BaseMapSwitcherPopup baseMapSwitcherPopup;
        private AuthModal authModal;
        private Toast toast;
        private RegisterPanel registerPanel;
        private ForgotPasswordPanel forgotPasswordPanel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.titleBar = new TitleBar();
            this.ribbonBar = new RibbonBar();
            this.categoryRail = new CategoryRail();
            this.statusBar = new AppStatusBar();
            this.mapHost = new MapHost();
            this.layerControlPanel = new LayerControlPanel();
            this.routeOptimizerPanel = new RouteOptimizerPanel();
            this.searchPoiPanel = new SearchPoiPanel();
            this.travelGuidePanel = new TravelGuidePanel();
            this.poiIdentifyCard = new PoiIdentifyCard();
            this.baseMapSwitcherPopup = new BaseMapSwitcherPopup();
            this.authModal = new AuthModal();
            this.toast = new Toast();
            this.registerPanel = new RegisterPanel();
            this.forgotPasswordPanel = new ForgotPasswordPanel();
            this.SuspendLayout();

            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.ClientSize = new System.Drawing.Size(1536, 860);
            this.Text = "Cố Đô GIS";

            this.titleBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.titleBar.Size = new System.Drawing.Size(1536, 56);

            this.ribbonBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.ribbonBar.Size = new System.Drawing.Size(1536, 40);

            this.categoryRail.Dock = System.Windows.Forms.DockStyle.Left;
            this.categoryRail.Size = new System.Drawing.Size(48, 724);

            this.statusBar.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.statusBar.Size = new System.Drawing.Size(1536, 28);

            this.mapHost.Dock = System.Windows.Forms.DockStyle.Fill;

            this.Controls.Add(this.mapHost);

            this.Controls.Add(this.categoryRail);
            this.Controls.Add(this.ribbonBar);
            this.Controls.Add(this.titleBar);

            // Floating panels are real Forms owned by this window (not child controls): top-level windows
            // can carry the system's soft shadow and anti-aliased corners over the MapWinGIS map.
            // AttachTo records where each sits in this window and which edges it follows when resized.
            this.layerControlPanel.AttachTo(this, new System.Drawing.Point(64, 144), System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left);
            this.searchPoiPanel.AttachTo(this, new System.Drawing.Point(348, 144), System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left);
            this.routeOptimizerPanel.AttachTo(this, new System.Drawing.Point(1136, 144), System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right);
            this.travelGuidePanel.AttachTo(this, new System.Drawing.Point(1136, 272), System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right);
            this.poiIdentifyCard.AttachTo(this, new System.Drawing.Point(522, 618), System.Windows.Forms.AnchorStyles.Bottom);
            this.baseMapSwitcherPopup.AttachTo(this, new System.Drawing.Point(1264, 98), System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right);

            // Auth cards share one spot (top-16, centred over the map); only one is visible at a time.
            this.authModal.AttachTo(this, new System.Drawing.Point(582, 160), System.Windows.Forms.AnchorStyles.Top);
            this.registerPanel.AttachTo(this, new System.Drawing.Point(582, 160), System.Windows.Forms.AnchorStyles.Top);
            this.forgotPasswordPanel.AttachTo(this, new System.Drawing.Point(572, 144), System.Windows.Forms.AnchorStyles.Top);

            this.Controls.Add(this.statusBar);

            this.toast.TopOffset = 104;
            this.Controls.Add(this.toast);

            this.ResumeLayout(false);
        }
    }
}
