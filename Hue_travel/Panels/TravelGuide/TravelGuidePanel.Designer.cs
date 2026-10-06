using Guna.UI2.WinForms;
using Hue_travel.Theme;

namespace Hue_travel.Panels.TravelGuide
{
    partial class TravelGuidePanel
    {
        private System.ComponentModel.IContainer components = null;
        private Guna2Panel panelHeader;
        private System.Windows.Forms.Label lblTitle;
        private Guna2Button btnClose;
        private System.Windows.Forms.FlowLayoutPanel flowTours;

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
            this.flowTours = new System.Windows.Forms.FlowLayoutPanel();
            this.SuspendLayout();

            this.BackColor = ThemeColors.SurfaceContainerLowest;

            this.panelHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelHeader.Height = 32;
            this.panelHeader.FillColor = ThemeColors.Primary;

            this.lblTitle.Text = "Cẩm Nang & Hướng Dẫn Du Lịch Huế";
            this.lblTitle.Font = ThemeFonts.LabelMd;
            this.lblTitle.ForeColor = ThemeColors.OnPrimary;
            this.lblTitle.AutoSize = true;
            this.lblTitle.Location = new System.Drawing.Point(8, 8);

            this.btnClose.Text = "✕";
            this.btnClose.Size = new System.Drawing.Size(20, 20);
            this.btnClose.Location = new System.Drawing.Point(356, 6);
            this.btnClose.FillColor = ThemeColors.Primary;
            this.btnClose.ForeColor = ThemeColors.OnPrimary;
            this.btnClose.Click += (s, e) => CloseRequested?.Invoke(this, System.EventArgs.Empty);

            this.panelHeader.Controls.Add(this.lblTitle);
            this.panelHeader.Controls.Add(this.btnClose);

            this.flowTours.Location = new System.Drawing.Point(8, 40);
            this.flowTours.Size = new System.Drawing.Size(368, 260);
            this.flowTours.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowTours.WrapContents = false;
            this.flowTours.AutoScroll = true;

            this.Controls.Add(this.flowTours);
            this.Controls.Add(this.panelHeader);
            this.Name = "TravelGuidePanel";
            this.Size = new System.Drawing.Size(384, 320);
            this.ResumeLayout(false);
        }
    }
}
