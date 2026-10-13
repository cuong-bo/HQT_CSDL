using Hue_travel.Theme;

namespace Hue_travel.Shell
{
    partial class TitleBar
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Panel panelRoot;
        private System.Windows.Forms.Panel panelBottomBorder;
        private System.Windows.Forms.PictureBox picLogo;
        private System.Windows.Forms.Label lblLogo;
        private RoundedPanel boxSearch;
        private IconButton iconSearch;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.Label lblSearchHint;
        private IconButton btnClearSearch;
        private IconButton btnAuth;
        private IconButton btnMinimize;
        private IconButton btnMaximize;
        private IconButton btnClose;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.panelRoot = new System.Windows.Forms.Panel();
            this.panelBottomBorder = new System.Windows.Forms.Panel();
            this.picLogo = new System.Windows.Forms.PictureBox();
            this.lblLogo = new System.Windows.Forms.Label();
            this.boxSearch = new RoundedPanel();
            this.iconSearch = new IconButton();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.lblSearchHint = new System.Windows.Forms.Label();
            this.btnClearSearch = new IconButton();
            this.btnAuth = new IconButton();
            this.btnMinimize = new IconButton();
            this.btnMaximize = new IconButton();
            this.btnClose = new IconButton();
            ((System.ComponentModel.ISupportInitialize)(this.picLogo)).BeginInit();
            this.SuspendLayout();

            // h-9 bar on surface-container-low with a 1px surface-container bottom border.
            this.panelRoot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelRoot.BackColor = ThemeColors.SurfaceContainerLow;
            this.panelRoot.MouseDown += new System.Windows.Forms.MouseEventHandler(this.panelRoot_MouseDown);

            this.panelBottomBorder.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelBottomBorder.Height = 1;
            this.panelBottomBorder.BackColor = ThemeColors.SurfaceContainer;

            // Logo (h-7 w-7) + "Cố Đô GIS" (text-xs font-bold).
            this.picLogo.Location = new System.Drawing.Point(14, 8);
            this.picLogo.Size = new System.Drawing.Size(40, 40);
            this.picLogo.Image = AppImages.Logo(Ui.S(40));
            this.picLogo.BackColor = System.Drawing.Color.Transparent;
            this.picLogo.MouseDown += new System.Windows.Forms.MouseEventHandler(this.panelRoot_MouseDown);

            this.lblLogo.Text = "Cố Đô GIS";
            this.lblLogo.Font = ThemeFonts.Bold(16);
            this.lblLogo.ForeColor = ThemeColors.OnSurface;
            this.lblLogo.BackColor = System.Drawing.Color.Transparent;
            this.lblLogo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblLogo.Location = new System.Drawing.Point(64, 0);
            this.lblLogo.Size = new System.Drawing.Size(160, 55);
            this.lblLogo.MouseDown += new System.Windows.Forms.MouseEventHandler(this.panelRoot_MouseDown);

            // Search box in the middle of the bar (pill, slate-100; white with a blue ring while typing). Placed by TitleBar.LayoutSearch().
            this.boxSearch.Radius = 19;
            this.boxSearch.FillColor = ThemeColors.Slate100;
            this.boxSearch.BorderColor = ThemeColors.Slate200;
            this.boxSearch.Size = new System.Drawing.Size(520, 38);
            this.boxSearch.Location = new System.Drawing.Point(300, 9);

            this.iconSearch.AutoWidth = false;
            this.iconSearch.IconName = "search";
            this.iconSearch.IconSize = 18;
            this.iconSearch.IconColor = ThemeColors.Slate500;
            this.iconSearch.FillColor = System.Drawing.Color.Transparent;
            this.iconSearch.HoverFillColor = System.Drawing.Color.Transparent;
            this.iconSearch.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.iconSearch.Size = new System.Drawing.Size(18, 18);
            this.iconSearch.Location = new System.Drawing.Point(14, 10);

            this.txtSearch.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtSearch.BackColor = ThemeColors.Slate100;
            this.txtSearch.ForeColor = ThemeColors.Slate800;
            this.txtSearch.Font = ThemeFonts.Regular(13);
            this.txtSearch.Size = new System.Drawing.Size(420, 20);
            this.txtSearch.Location = new System.Drawing.Point(40, 9);
            this.txtSearch.TextChanged += new System.EventHandler(this.txtSearch_TextChanged);
            this.txtSearch.Enter += new System.EventHandler(this.txtSearch_FocusChanged);
            this.txtSearch.Leave += new System.EventHandler(this.txtSearch_FocusChanged);
            this.txtSearch.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtSearch_KeyDown);

            // The hint is a label, not the TextBox's own cue banner: the banner is drawn without font fallback and loses Vietnamese marks.
            this.lblSearchHint.Text = "Tìm địa điểm, di tích, khách sạn, quán ăn…";
            this.lblSearchHint.Font = ThemeFonts.Regular(13);
            this.lblSearchHint.ForeColor = ThemeColors.Slate400;
            this.lblSearchHint.BackColor = System.Drawing.Color.Transparent;
            this.lblSearchHint.UseMnemonic = false;
            this.lblSearchHint.AutoSize = false;
            this.lblSearchHint.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblSearchHint.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.lblSearchHint.Size = new System.Drawing.Size(420, 20);
            this.lblSearchHint.Location = new System.Drawing.Point(40, 9);
            this.lblSearchHint.Click += new System.EventHandler(this.lblSearchHint_Click);

            this.btnClearSearch.AutoWidth = false;
            this.btnClearSearch.IconName = "close";
            this.btnClearSearch.IconSize = 14;
            this.btnClearSearch.IconColor = ThemeColors.Slate500;
            this.btnClearSearch.FillColor = System.Drawing.Color.Transparent;
            this.btnClearSearch.HoverFillColor = ThemeColors.Slate200;
            this.btnClearSearch.Radius = 11;
            this.btnClearSearch.Size = new System.Drawing.Size(22, 22);
            this.btnClearSearch.Location = new System.Drawing.Point(488, 8);
            this.btnClearSearch.Visible = false;
            this.btnClearSearch.Click += new System.EventHandler(this.btnClearSearch_Click);

            this.boxSearch.Controls.Add(this.lblSearchHint);
            this.boxSearch.Controls.Add(this.iconSearch);
            this.boxSearch.Controls.Add(this.txtSearch);
            this.boxSearch.Controls.Add(this.btnClearSearch);

            // Right group, positions refined by TitleBar.LayoutRightGroup() from real text widths.
            this.btnClose.AutoWidth = false;
            this.btnClose.Size = new System.Drawing.Size(48, 55);
            this.btnClose.Location = new System.Drawing.Point(964, 0);
            this.btnClose.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnClose.IconName = "close";
            this.btnClose.IconSize = 18;
            this.btnClose.IconColor = ThemeColors.OnSurfaceVariant;
            this.btnClose.HoverIconColor = System.Drawing.Color.White;
            this.btnClose.FillColor = System.Drawing.Color.Transparent;
            this.btnClose.HoverFillColor = ThemeColors.Error;
            this.btnClose.Radius = 0;
            this.btnClose.AutoWidth = false;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);

            this.btnMaximize.AutoWidth = false;
            this.btnMaximize.Size = new System.Drawing.Size(48, 55);
            this.btnMaximize.Location = new System.Drawing.Point(932, 0);
            this.btnMaximize.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnMaximize.IconName = "crop_square";
            this.btnMaximize.IconSize = 18;
            this.btnMaximize.IconColor = ThemeColors.OnSurfaceVariant;
            this.btnMaximize.FillColor = System.Drawing.Color.Transparent;
            this.btnMaximize.HoverFillColor = ThemeColors.SurfaceContainerHigh;
            this.btnMaximize.Radius = 0;
            this.btnMaximize.AutoWidth = false;
            this.btnMaximize.Click += new System.EventHandler(this.btnMaximize_Click);

            this.btnMinimize.AutoWidth = false;
            this.btnMinimize.Size = new System.Drawing.Size(48, 55);
            this.btnMinimize.Location = new System.Drawing.Point(900, 0);
            this.btnMinimize.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnMinimize.IconName = "minimize";
            this.btnMinimize.IconSize = 18;
            this.btnMinimize.IconColor = ThemeColors.OnSurfaceVariant;
            this.btnMinimize.FillColor = System.Drawing.Color.Transparent;
            this.btnMinimize.HoverFillColor = ThemeColors.SurfaceContainerHigh;
            this.btnMinimize.Radius = 0;
            this.btnMinimize.AutoWidth = false;
            this.btnMinimize.Click += new System.EventHandler(this.btnMinimize_Click);

            // "Đăng nhập / Đăng ký": bg-sky-600, 11px semibold white, account_circle 14px, rounded.
            this.btnAuth.Text = "Đăng nhập / Đăng ký";
            this.btnAuth.Font = ThemeFonts.SemiBold(13);
            this.btnAuth.ForeColor = System.Drawing.Color.White;
            this.btnAuth.IconName = "account_circle";
            this.btnAuth.IconSize = 18;
            this.btnAuth.IconColor = System.Drawing.Color.White;
            this.btnAuth.Gap = 6;
            this.btnAuth.HorizontalPadding = 16;
            this.btnAuth.FillColor = ThemeColors.Sky600;
            this.btnAuth.HoverFillColor = ThemeColors.Sky700;
            this.btnAuth.Elevated = true;
            this.btnAuth.Radius = 4;
            this.btnAuth.Size = new System.Drawing.Size(210, 38);
            this.btnAuth.Location = new System.Drawing.Point(736, 9);
            this.btnAuth.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnAuth.Click += new System.EventHandler(this.btnAuth_Click);

            this.panelRoot.Controls.Add(this.picLogo);
            this.panelRoot.Controls.Add(this.lblLogo);
            this.panelRoot.Controls.Add(this.boxSearch);
            this.panelRoot.Controls.Add(this.btnAuth);
            this.panelRoot.Controls.Add(this.btnMinimize);
            this.panelRoot.Controls.Add(this.btnMaximize);
            this.panelRoot.Controls.Add(this.btnClose);

            this.Controls.Add(this.panelRoot);
            this.Controls.Add(this.panelBottomBorder);
            this.Name = "TitleBar";
            this.Size = new System.Drawing.Size(1000, 56);
            ((System.ComponentModel.ISupportInitialize)(this.picLogo)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
