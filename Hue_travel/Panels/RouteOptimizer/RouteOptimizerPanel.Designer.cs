using Guna.UI2.WinForms;
using Hue_travel.Theme;

namespace Hue_travel.Panels.RouteOptimizer
{
    partial class RouteOptimizerPanel
    {
        private System.ComponentModel.IContainer components = null;
        private Guna2Panel panelHeader;
        private System.Windows.Forms.Label lblTitle;
        private Guna2Button btnClose;
        private System.Windows.Forms.ComboBox cboStartPoint;
        private System.Windows.Forms.CheckedListBox clbDestinations;
        private System.Windows.Forms.Label lblSelectedCount;
        private Guna2Button btnFindRoute;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.panelHeader = new Guna2Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.btnClose = new Guna2Button();
            this.cboStartPoint = new System.Windows.Forms.ComboBox();
            this.clbDestinations = new System.Windows.Forms.CheckedListBox();
            this.lblSelectedCount = new System.Windows.Forms.Label();
            this.btnFindRoute = new Guna2Button();
            this.SuspendLayout();

            this.BackColor = ThemeColors.SurfaceContainerLowest;

            this.panelHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelHeader.Height = 32;
            this.panelHeader.FillColor = ThemeColors.Primary;

            this.lblTitle.Text = "Lên Lộ Trình Tham Quan Tự Động";
            this.lblTitle.Font = ThemeFonts.LabelMd;
            this.lblTitle.ForeColor = ThemeColors.OnPrimary;
            this.lblTitle.AutoSize = true;
            this.lblTitle.Location = new System.Drawing.Point(8, 8);

            this.btnClose.Text = "✕";
            this.btnClose.Size = new System.Drawing.Size(20, 20);
            this.btnClose.Location = new System.Drawing.Point(356, 6);
            this.btnClose.FillColor = ThemeColors.Primary;
            this.btnClose.ForeColor = ThemeColors.OnPrimary;
            this.btnClose.Click += (s, e) => this.Hide();

            this.panelHeader.Controls.Add(this.lblTitle);
            this.panelHeader.Controls.Add(this.btnClose);

            this.cboStartPoint.Location = new System.Drawing.Point(8, 40);
            this.cboStartPoint.Size = new System.Drawing.Size(368, 24);
            this.cboStartPoint.Font = ThemeFonts.BodySm;
            this.cboStartPoint.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;

            this.lblSelectedCount.Font = ThemeFonts.LabelSm;
            this.lblSelectedCount.ForeColor = ThemeColors.Primary;
            this.lblSelectedCount.AutoSize = true;
            this.lblSelectedCount.Location = new System.Drawing.Point(8, 72);

            this.clbDestinations.Font = ThemeFonts.BodySm;
            this.clbDestinations.Location = new System.Drawing.Point(8, 94);
            this.clbDestinations.Size = new System.Drawing.Size(368, 150);
            this.clbDestinations.CheckOnClick = true;
            this.clbDestinations.ItemCheck += (s, e) => BeginInvoke((System.Action)RefreshSelectionSummary);

            this.btnFindRoute.Text = "Bắt Đầu Tìm Đường Ngắn Nhất";
            this.btnFindRoute.Location = new System.Drawing.Point(8, 252);
            this.btnFindRoute.Size = new System.Drawing.Size(368, 32);
            this.btnFindRoute.FillColor = ThemeColors.Primary;
            this.btnFindRoute.ForeColor = ThemeColors.OnPrimary;
            this.btnFindRoute.Font = ThemeFonts.LabelSm;

            this.Controls.Add(this.btnFindRoute);
            this.Controls.Add(this.clbDestinations);
            this.Controls.Add(this.lblSelectedCount);
            this.Controls.Add(this.cboStartPoint);
            this.Controls.Add(this.panelHeader);
            this.Name = "RouteOptimizerPanel";
            this.Size = new System.Drawing.Size(384, 300);
            this.ResumeLayout(false);
        }
    }
}
