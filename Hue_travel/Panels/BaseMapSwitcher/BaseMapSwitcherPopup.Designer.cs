using Guna.UI2.WinForms;
using Hue_travel.Theme;

namespace Hue_travel.Panels.BaseMapSwitcher
{
    partial class BaseMapSwitcherPopup
    {
        private System.ComponentModel.IContainer components = null;
        private Guna2Button btnStandard;
        private Guna2Button btnSatellite;
        private Guna2Button btnNight;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.btnStandard = new Guna2Button();
            this.btnSatellite = new Guna2Button();
            this.btnNight = new Guna2Button();
            this.SuspendLayout();

            this.BackColor = ThemeColors.SurfaceContainerLowest;

            this.btnStandard.Text = "Chuẩn sáng";
            this.btnStandard.Location = new System.Drawing.Point(8, 8);
            this.btnStandard.Size = new System.Drawing.Size(80, 48);
            this.btnStandard.FillColor = ThemeColors.SurfaceContainer;
            this.btnStandard.ForeColor = ThemeColors.Primary;
            this.btnStandard.Click += new System.EventHandler(this.btnStandard_Click);

            this.btnSatellite.Text = "Vệ tinh";
            this.btnSatellite.Location = new System.Drawing.Point(96, 8);
            this.btnSatellite.Size = new System.Drawing.Size(80, 48);
            this.btnSatellite.FillColor = ThemeColors.SurfaceContainerLowest;
            this.btnSatellite.ForeColor = ThemeColors.OnSurfaceVariant;
            this.btnSatellite.Click += new System.EventHandler(this.btnSatellite_Click);

            this.btnNight.Text = "Ngắm đêm";
            this.btnNight.Location = new System.Drawing.Point(184, 8);
            this.btnNight.Size = new System.Drawing.Size(80, 48);
            this.btnNight.FillColor = ThemeColors.SurfaceContainerLowest;
            this.btnNight.ForeColor = ThemeColors.OnSurfaceVariant;
            this.btnNight.Click += new System.EventHandler(this.btnNight_Click);

            this.Controls.Add(this.btnStandard);
            this.Controls.Add(this.btnSatellite);
            this.Controls.Add(this.btnNight);
            this.Name = "BaseMapSwitcherPopup";
            this.Size = new System.Drawing.Size(272, 64);
            this.ResumeLayout(false);
        }
    }
}
