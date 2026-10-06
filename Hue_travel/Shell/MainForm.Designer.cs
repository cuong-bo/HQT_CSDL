using Hue_travel.MapEngine;
using Hue_travel.Panels.BaseMapSwitcher;
using Hue_travel.Panels.LayerControl;
using Hue_travel.Panels.PoiIdentify;
using Hue_travel.Panels.RouteOptimizer;
using Hue_travel.Panels.SearchPoi;
using Hue_travel.Panels.TravelGuide;
using Hue_travel.Auth;

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
            this.SuspendLayout();

            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.ClientSize = new System.Drawing.Size(1280, 800);
            this.Text = "Cố Đô GIS";

            this.titleBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.titleBar.Size = new System.Drawing.Size(1280, 36);

            this.ribbonBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.ribbonBar.Size = new System.Drawing.Size(1280, 40);

            this.categoryRail.Dock = System.Windows.Forms.DockStyle.Left;
            this.categoryRail.Size = new System.Drawing.Size(48, 724);

            this.statusBar.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.statusBar.Size = new System.Drawing.Size(1280, 28);

            this.mapHost.Dock = System.Windows.Forms.DockStyle.Fill;

            this.Controls.Add(this.mapHost);

            // Each floating panel is a real Form (not a UserControl) per the user's request —
            // TopLevel must be set false before it can be embedded as a child control, and it
            // must be Show()'n once to force handle creation before Visible can be toggled later.
            this.layerControlPanel.TopLevel = false;
            this.layerControlPanel.Location = new System.Drawing.Point(64, 88);
            this.Controls.Add(this.layerControlPanel);
            this.layerControlPanel.Show();
            this.layerControlPanel.Visible = false;

            this.routeOptimizerPanel.TopLevel = false;
            this.routeOptimizerPanel.Location = new System.Drawing.Point(888, 88);
            this.Controls.Add(this.routeOptimizerPanel);
            this.routeOptimizerPanel.Show();
            this.routeOptimizerPanel.Visible = false;

            this.searchPoiPanel.TopLevel = false;
            this.searchPoiPanel.Location = new System.Drawing.Point(372, 88);
            this.Controls.Add(this.searchPoiPanel);
            this.searchPoiPanel.Show();
            this.searchPoiPanel.Visible = false;

            this.travelGuidePanel.TopLevel = false;
            this.travelGuidePanel.Location = new System.Drawing.Point(888, 400);
            this.Controls.Add(this.travelGuidePanel);
            this.travelGuidePanel.Show();
            this.travelGuidePanel.Visible = false;

            this.poiIdentifyCard.TopLevel = false;
            this.poiIdentifyCard.Location = new System.Drawing.Point(370, 560);
            this.Controls.Add(this.poiIdentifyCard);
            this.poiIdentifyCard.Show();
            this.poiIdentifyCard.Visible = false;

            this.baseMapSwitcherPopup.TopLevel = false;
            this.baseMapSwitcherPopup.Location = new System.Drawing.Point(1060, 84);
            this.Controls.Add(this.baseMapSwitcherPopup);
            this.baseMapSwitcherPopup.Show();
            this.baseMapSwitcherPopup.Visible = false;

            this.Controls.Add(this.categoryRail);
            this.Controls.Add(this.ribbonBar);
            this.Controls.Add(this.titleBar);

            this.authModal.TopLevel = false;
            this.authModal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Controls.Add(this.authModal);
            this.authModal.Show();
            this.authModal.Visible = false;

            this.Controls.Add(this.statusBar);

            this.ResumeLayout(false);
        }
    }
}
