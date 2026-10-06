using Guna.UI2.WinForms;
using Hue_travel.Theme;

namespace Hue_travel.Shell
{
    partial class TitleBar
    {
        private System.ComponentModel.IContainer components = null;
        private Guna2Panel panelRoot;
        private System.Windows.Forms.Label lblLogo;
        private Guna2Button btnAuth;
        private Guna2Button btnMinimize;
        private Guna2Button btnMaximize;
        private Guna2Button btnClose;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.panelRoot = new Guna2Panel();
            this.lblLogo = new System.Windows.Forms.Label();
            this.btnAuth = new Guna2Button();
            this.btnMinimize = new Guna2Button();
            this.btnMaximize = new Guna2Button();
            this.btnClose = new Guna2Button();
            this.SuspendLayout();

            this.panelRoot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelRoot.FillColor = ThemeColors.SurfaceContainerLow;
            this.panelRoot.BorderRadius = 0;
            this.panelRoot.Controls.Add(this.lblLogo);
            this.panelRoot.Controls.Add(this.btnAuth);
            this.panelRoot.Controls.Add(this.btnMinimize);
            this.panelRoot.Controls.Add(this.btnMaximize);
            this.panelRoot.Controls.Add(this.btnClose);

            this.lblLogo.Text = "Cố Đô GIS";
            this.lblLogo.Font = ThemeFonts.LabelMd;
            this.lblLogo.ForeColor = ThemeColors.OnSurface;
            this.lblLogo.AutoSize = true;
            this.lblLogo.Location = new System.Drawing.Point(12, 10);

            this.btnClose.Size = new System.Drawing.Size(32, 28);
            this.btnClose.Location = new System.Drawing.Point(958, 4);
            this.btnClose.Text = "✕";
            this.btnClose.FillColor = ThemeColors.SurfaceContainerLow;
            this.btnClose.ForeColor = ThemeColors.OnSurfaceVariant;
            this.btnClose.BorderRadius = ThemeMetrics.RadiusSm;
            this.btnClose.Click += (s, e) => CloseClicked?.Invoke(this, System.EventArgs.Empty);

            this.btnMaximize.Size = new System.Drawing.Size(32, 28);
            this.btnMaximize.Location = new System.Drawing.Point(922, 4);
            this.btnMaximize.Text = "☐";
            this.btnMaximize.FillColor = ThemeColors.SurfaceContainerLow;
            this.btnMaximize.ForeColor = ThemeColors.OnSurfaceVariant;
            this.btnMaximize.BorderRadius = ThemeMetrics.RadiusSm;
            this.btnMaximize.Click += (s, e) => MaximizeClicked?.Invoke(this, System.EventArgs.Empty);

            this.btnMinimize.Size = new System.Drawing.Size(32, 28);
            this.btnMinimize.Location = new System.Drawing.Point(886, 4);
            this.btnMinimize.Text = "—";
            this.btnMinimize.FillColor = ThemeColors.SurfaceContainerLow;
            this.btnMinimize.ForeColor = ThemeColors.OnSurfaceVariant;
            this.btnMinimize.BorderRadius = ThemeMetrics.RadiusSm;
            this.btnMinimize.Click += (s, e) => MinimizeClicked?.Invoke(this, System.EventArgs.Empty);

            this.btnAuth.Size = new System.Drawing.Size(170, 26);
            this.btnAuth.Location = new System.Drawing.Point(706, 5);
            this.btnAuth.Text = "Đăng nhập / Đăng ký";
            this.btnAuth.Font = ThemeFonts.LabelSm;
            this.btnAuth.FillColor = ThemeColors.Primary;
            this.btnAuth.ForeColor = ThemeColors.OnPrimary;
            this.btnAuth.BorderRadius = ThemeMetrics.RadiusSm;
            this.btnAuth.Click += (s, e) => AuthClicked?.Invoke(this, System.EventArgs.Empty);

            this.Controls.Add(this.panelRoot);
            this.Name = "TitleBar";
            this.Size = new System.Drawing.Size(1000, 36);
            this.ResumeLayout(false);
        }
    }
}
