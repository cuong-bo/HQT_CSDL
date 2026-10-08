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

            // Each floating panel is a real Form (not a UserControl) per the user's request —
            // TopLevel must be set false before it can be embedded as a child control, and it
            // must be Show()'n once to force handle creation before Visible can be toggled later.
            this.layerControlPanel.TopLevel = false;
            this.layerControlPanel.Location = new System.Drawing.Point(64, 144);
            this.Controls.Add(this.layerControlPanel);
            this.layerControlPanel.Show();
            this.layerControlPanel.Visible = true;

            this.routeOptimizerPanel.TopLevel = false;
            this.routeOptimizerPanel.Location = new System.Drawing.Point(1136, 144);
            this.routeOptimizerPanel.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.Controls.Add(this.routeOptimizerPanel);
            this.routeOptimizerPanel.Show();
            this.routeOptimizerPanel.Visible = true;

            this.searchPoiPanel.TopLevel = false;
            this.searchPoiPanel.Location = new System.Drawing.Point(348, 144);
            this.Controls.Add(this.searchPoiPanel);
            this.searchPoiPanel.Show();
            this.searchPoiPanel.Visible = true;

            this.travelGuidePanel.TopLevel = false;
            this.travelGuidePanel.Location = new System.Drawing.Point(1136, 272);
            this.travelGuidePanel.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            this.Controls.Add(this.travelGuidePanel);
            this.travelGuidePanel.Show();
            this.travelGuidePanel.Visible = true;

            this.poiIdentifyCard.TopLevel = false;
            this.poiIdentifyCard.Location = new System.Drawing.Point(522, 618);
            this.poiIdentifyCard.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.Controls.Add(this.poiIdentifyCard);
            this.poiIdentifyCard.Show();
            this.poiIdentifyCard.Visible = true;

            this.baseMapSwitcherPopup.TopLevel = false;
            this.baseMapSwitcherPopup.Location = new System.Drawing.Point(1264, 98);
            this.baseMapSwitcherPopup.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.Controls.Add(this.baseMapSwitcherPopup);
            this.baseMapSwitcherPopup.Show();
            this.baseMapSwitcherPopup.Visible = false;

            this.Controls.Add(this.categoryRail);
            this.Controls.Add(this.ribbonBar);
            this.Controls.Add(this.titleBar);

            // Auth cards share one spot (top-16, centred over the map); only one is visible at a time.
            this.authModal.TopLevel = false;
            this.authModal.Location = new System.Drawing.Point(582, 160);
            this.authModal.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.Controls.Add(this.authModal);
            this.authModal.Show();
            this.authModal.Visible = false;

            this.registerPanel.TopLevel = false;
            this.registerPanel.Location = new System.Drawing.Point(582, 160);
            this.registerPanel.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.Controls.Add(this.registerPanel);
            this.registerPanel.Show();
            this.registerPanel.Visible = false;

            this.forgotPasswordPanel.TopLevel = false;
            this.forgotPasswordPanel.Location = new System.Drawing.Point(572, 144);
            this.forgotPasswordPanel.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.Controls.Add(this.forgotPasswordPanel);
            this.forgotPasswordPanel.Show();
            this.forgotPasswordPanel.Visible = false;

            this.Controls.Add(this.statusBar);

            this.toast.TopOffset = 104;
            this.Controls.Add(this.toast);

            this.ResumeLayout(false);
        }
    }
}
