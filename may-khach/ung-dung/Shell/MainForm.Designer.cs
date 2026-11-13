using Hue_travel.MapEngine;
using Hue_travel.Panels.PoiIdentify;
using Hue_travel.Panels.RouteOptimizer;
using Hue_travel.Panels.Explore;
using Hue_travel.Panels.Layers;
using Hue_travel.Panels.Assistant;
using Hue_travel.Auth;
using Hue_travel.Theme;

namespace Hue_travel.Shell
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        // Side panel slot, in design pixels: map edge (12) + rail (64) + 12; header (40) + 16 + search (48) + 12.
        private const int SideLeft = 88;
        private const int SideTop = 116;
        private const int SideBottom = 56;

        private TitleBar titleBar;
        private SearchPill searchPill;
        private FilterBar filterBar;
        private NavRail navRail;
        private AppStatusBar statusBar;
        private MapHost mapHost;

        private RouteOptimizerPanel routeOptimizerPanel;
        private TripResultPanel tripResultPanel;
        private ExplorePanel explorePanel;
        private LayerPanel layerPanel;
        private SearchDropdown searchDropdown;
        private ChatPanel chatPanel;
        private PoiIdentifyCard poiIdentifyCard;
        private Toast toast;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.titleBar = new TitleBar();
            this.searchPill = new SearchPill();
            this.filterBar = new FilterBar();
            this.navRail = new NavRail();
            this.statusBar = new AppStatusBar();
            this.mapHost = new MapHost();
            this.routeOptimizerPanel = new RouteOptimizerPanel();
            this.tripResultPanel = new TripResultPanel();
            this.explorePanel = new ExplorePanel();
            this.layerPanel = new LayerPanel();
            this.searchDropdown = new SearchDropdown();
            this.chatPanel = new ChatPanel();
            this.poiIdentifyCard = new PoiIdentifyCard();
            this.toast = new Toast();
            this.SuspendLayout();

            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.ClientSize = new System.Drawing.Size(1536, 860);
            this.Text = "Hue Travel";

            this.titleBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.titleBar.Size = new System.Drawing.Size(1536, 40);

            // The map fills the window under the slim title bar. The search box, the chip bar, the tool rail
            // and the status pill are child controls of MapHost so they float over the map (see HostOverlays).
            this.mapHost.Dock = System.Windows.Forms.DockStyle.Fill;

            this.Controls.Add(this.mapHost);
            this.Controls.Add(this.titleBar);

            // Floating panels are real Forms owned by this window (not child controls): top-level windows
            // can carry the system's soft shadow and anti-aliased corners over the MapWinGIS map.
            // AttachTo records where each sits in this window and which edges it follows when resized.
            this.explorePanel.AttachToSide(this, SideLeft, SideTop, SideBottom);
            this.layerPanel.AttachToSide(this, SideLeft, SideTop, SideBottom);
            this.routeOptimizerPanel.AttachToSide(this, SideLeft, SideTop, SideBottom);
            // The route result is a wide panel centred under the map (no left or right edge: it stays centred horizontally).
            this.tripResultPanel.AttachTo(this, new System.Drawing.Point((this.ClientSize.Width - TripResultPanel.DesignWidth) / 2, this.ClientSize.Height - TripResultPanel.DesignHeight - 56), System.Windows.Forms.AnchorStyles.Bottom);
            this.chatPanel.AttachToSide(this, SideLeft, SideTop, SideBottom);
            // The place card sits at the bottom right of the map, clear of the map's control column.
            this.poiIdentifyCard.AttachTo(this, new System.Drawing.Point(1044, 198), System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right);

            this.toast.TopOffset = 56;
            this.Controls.Add(this.toast);
            this.Controls.Add(this.searchDropdown);

            this.ResumeLayout(false);
        }
    }
}
