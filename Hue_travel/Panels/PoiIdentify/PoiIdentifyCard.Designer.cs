using Guna.UI2.WinForms;
using Hue_travel.Theme;

namespace Hue_travel.Panels.PoiIdentify
{
    partial class PoiIdentifyCard
    {
        private System.ComponentModel.IContainer components = null;
        private Guna2Panel panelHeader;
        private System.Windows.Forms.Label lblBadge;
        private Guna2Button btnClose;
        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.Label lblAddress;
        private System.Windows.Forms.Label lblHours;
        private System.Windows.Forms.Label lblPrice;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.panelHeader = new Guna2Panel();
            this.lblBadge = new System.Windows.Forms.Label();
            this.btnClose = new Guna2Button();
            this.lblName = new System.Windows.Forms.Label();
            this.lblAddress = new System.Windows.Forms.Label();
            this.lblHours = new System.Windows.Forms.Label();
            this.lblPrice = new System.Windows.Forms.Label();
            this.SuspendLayout();

            this.BackColor = ThemeColors.SurfaceContainerLowest;

            this.panelHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelHeader.Height = 32;
            this.panelHeader.FillColor = ThemeColors.SurfaceContainerLow;

            this.lblBadge.Font = ThemeFonts.LabelSm;
            this.lblBadge.ForeColor = ThemeColors.OnPrimary;
            this.lblBadge.BackColor = ThemeColors.BadgeAmber;
            this.lblBadge.AutoSize = true;
            this.lblBadge.Location = new System.Drawing.Point(8, 6);

            this.btnClose.Text = "✕";
            this.btnClose.Size = new System.Drawing.Size(20, 20);
            this.btnClose.Location = new System.Drawing.Point(512, 6);
            this.btnClose.FillColor = ThemeColors.SurfaceContainerLow;
            this.btnClose.ForeColor = ThemeColors.OnSurfaceVariant;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);

            this.panelHeader.Controls.Add(this.lblBadge);
            this.panelHeader.Controls.Add(this.btnClose);

            this.lblName.Font = ThemeFonts.HeadlineMd;
            this.lblName.ForeColor = ThemeColors.OnSurface;
            this.lblName.AutoSize = true;
            this.lblName.Location = new System.Drawing.Point(8, 42);

            this.lblAddress.Font = ThemeFonts.BodySm;
            this.lblAddress.ForeColor = ThemeColors.OnSurfaceVariant;
            this.lblAddress.AutoSize = true;
            this.lblAddress.Location = new System.Drawing.Point(8, 64);

            this.lblHours.Font = ThemeFonts.BodySm;
            this.lblHours.ForeColor = ThemeColors.OnSurfaceVariant;
            this.lblHours.AutoSize = true;
            this.lblHours.Location = new System.Drawing.Point(8, 86);

            this.lblPrice.Font = ThemeFonts.BodySm;
            this.lblPrice.ForeColor = ThemeColors.Primary;
            this.lblPrice.AutoSize = true;
            this.lblPrice.Location = new System.Drawing.Point(8, 108);

            this.Controls.Add(this.lblPrice);
            this.Controls.Add(this.lblHours);
            this.Controls.Add(this.lblAddress);
            this.Controls.Add(this.lblName);
            this.Controls.Add(this.panelHeader);
            this.Name = "PoiIdentifyCard";
            this.Size = new System.Drawing.Size(540, 140);
            this.ResumeLayout(false);
        }
    }
}
