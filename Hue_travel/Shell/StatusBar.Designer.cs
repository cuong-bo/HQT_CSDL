using Guna.UI2.WinForms;
using Hue_travel.Theme;

namespace Hue_travel.Shell
{
    partial class StatusBar
    {
        private System.ComponentModel.IContainer components = null;
        private Guna2Panel panelRoot;
        private System.Windows.Forms.Label lblCoordinates;
        private System.Windows.Forms.Label lblScale;
        private System.Windows.Forms.Label lblWeather;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.panelRoot = new Guna2Panel();
            this.lblCoordinates = new System.Windows.Forms.Label();
            this.lblScale = new System.Windows.Forms.Label();
            this.lblWeather = new System.Windows.Forms.Label();
            this.SuspendLayout();

            this.panelRoot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelRoot.FillColor = ThemeColors.SurfaceContainerLowest;

            this.lblCoordinates.Font = ThemeFonts.BodySm;
            this.lblCoordinates.ForeColor = ThemeColors.OnSurfaceVariant;
            this.lblCoordinates.AutoSize = true;
            this.lblCoordinates.Location = new System.Drawing.Point(12, 7);
            this.lblCoordinates.Text = "Khu vực đang xem: Trung tâm Cố đô Huế & Bờ sông Hương";

            this.lblWeather.Font = ThemeFonts.BodySm;
            this.lblWeather.ForeColor = ThemeColors.OnSurfaceVariant;
            this.lblWeather.AutoSize = true;
            this.lblWeather.Location = new System.Drawing.Point(560, 7);
            this.lblWeather.Text = "Thời tiết hôm nay: Nắng đẹp, 26°C";

            this.lblScale.Font = ThemeFonts.BodySm;
            this.lblScale.ForeColor = ThemeColors.OnSurfaceVariant;
            this.lblScale.AutoSize = true;
            this.lblScale.Location = new System.Drawing.Point(900, 7);
            this.lblScale.Text = "Tỉ lệ: 1:15,000";

            this.panelRoot.Controls.Add(this.lblCoordinates);
            this.panelRoot.Controls.Add(this.lblWeather);
            this.panelRoot.Controls.Add(this.lblScale);

            this.Controls.Add(this.panelRoot);
            this.Name = "StatusBar";
            this.Size = new System.Drawing.Size(1000, 28);
            this.ResumeLayout(false);
        }
    }
}
