using Guna.UI2.WinForms;
using Hue_travel.Theme;

namespace Hue_travel.Panels.LayerControl
{
    partial class LayerControlPanel
    {
        private System.ComponentModel.IContainer components = null;
        private Guna2Panel panelHeader;
        private System.Windows.Forms.Label lblTitle;
        private Guna2Button btnClose;
        private Guna2Panel panelBody;
        private Guna2CheckBox chkHeritage;
        private Guna2CheckBox chkFood;
        private Guna2CheckBox chkStay;
        private Guna2CheckBox chkRoads;
        private Guna2CheckBox chkRiver;
        private Guna2CheckBox chkPiers;
        private Guna2TrackBar sliderBrightness;

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
            this.panelBody = new Guna2Panel();
            this.chkHeritage = new Guna2CheckBox();
            this.chkFood = new Guna2CheckBox();
            this.chkStay = new Guna2CheckBox();
            this.chkRoads = new Guna2CheckBox();
            this.chkRiver = new Guna2CheckBox();
            this.chkPiers = new Guna2CheckBox();
            this.sliderBrightness = new Guna2TrackBar();
            this.SuspendLayout();

            this.BackColor = ThemeColors.SurfaceContainerLowest;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.ShowInTaskbar = false;

            this.panelHeader.FillColor = ThemeColors.SurfaceContainer;
            this.panelHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelHeader.Height = 32;

            this.lblTitle.Text = "Danh Mục Địa Điểm";
            this.lblTitle.Font = ThemeFonts.LabelMd;
            this.lblTitle.ForeColor = ThemeColors.OnSurface;
            this.lblTitle.AutoSize = true;
            this.lblTitle.Location = new System.Drawing.Point(8, 8);

            this.btnClose.Text = "✕";
            this.btnClose.Size = new System.Drawing.Size(20, 20);
            this.btnClose.Location = new System.Drawing.Point(272, 6);
            this.btnClose.FillColor = ThemeColors.SurfaceContainer;
            this.btnClose.ForeColor = ThemeColors.OnSurfaceVariant;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);

            this.panelHeader.Controls.Add(this.lblTitle);
            this.panelHeader.Controls.Add(this.btnClose);

            this.panelBody.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelBody.FillColor = ThemeColors.SurfaceContainerLowest;

            this.chkHeritage.Text = "Di tích lịch sử & Điểm tham quan (38 điểm)";
            this.chkHeritage.Checked = true;
            this.chkHeritage.Location = new System.Drawing.Point(8, 8);
            this.chkHeritage.Size = new System.Drawing.Size(270, 22);
            this.chkHeritage.Font = ThemeFonts.BodySm;
            this.chkHeritage.CheckedState.BorderColor = ThemeColors.Primary;
            this.chkHeritage.CheckedState.FillColor = ThemeColors.Primary;

            this.chkFood.Text = "Món ngon & Quán đặc sản Huế (64 quán)";
            this.chkFood.Checked = true;
            this.chkFood.Location = new System.Drawing.Point(8, 36);
            this.chkFood.Size = new System.Drawing.Size(270, 22);
            this.chkFood.Font = ThemeFonts.BodySm;
            this.chkFood.CheckedState.BorderColor = ThemeColors.Primary;
            this.chkFood.CheckedState.FillColor = ThemeColors.Primary;

            this.chkStay.Text = "Khách sạn & Nơi lưu trú (52 nơi)";
            this.chkStay.Checked = true;
            this.chkStay.Location = new System.Drawing.Point(8, 64);
            this.chkStay.Size = new System.Drawing.Size(270, 22);
            this.chkStay.Font = ThemeFonts.BodySm;
            this.chkStay.CheckedState.BorderColor = ThemeColors.Primary;
            this.chkStay.CheckedState.FillColor = ThemeColors.Primary;

            this.chkRoads.Text = "Tuyến đường chính & Cầu qua sông";
            this.chkRoads.Checked = true;
            this.chkRoads.Location = new System.Drawing.Point(8, 92);
            this.chkRoads.Size = new System.Drawing.Size(270, 22);
            this.chkRoads.Font = ThemeFonts.BodySm;
            this.chkRoads.CheckedState.BorderColor = ThemeColors.Primary;
            this.chkRoads.CheckedState.FillColor = ThemeColors.Primary;

            this.chkRiver.Text = "Sông Hương & Các hồ cảnh quan";
            this.chkRiver.Checked = true;
            this.chkRiver.Location = new System.Drawing.Point(8, 120);
            this.chkRiver.Size = new System.Drawing.Size(270, 22);
            this.chkRiver.Font = ThemeFonts.BodySm;
            this.chkRiver.CheckedState.BorderColor = ThemeColors.Primary;
            this.chkRiver.CheckedState.FillColor = ThemeColors.Primary;

            this.chkPiers.Text = "Bến thuyền rồng & Điểm xe buýt";
            this.chkPiers.Checked = false;
            this.chkPiers.Location = new System.Drawing.Point(8, 148);
            this.chkPiers.Size = new System.Drawing.Size(270, 22);
            this.chkPiers.Font = ThemeFonts.BodySm;
            this.chkPiers.CheckedState.BorderColor = ThemeColors.Primary;
            this.chkPiers.CheckedState.FillColor = ThemeColors.Primary;

            this.sliderBrightness.Location = new System.Drawing.Point(8, 190);
            this.sliderBrightness.Size = new System.Drawing.Size(270, 24);
            this.sliderBrightness.Minimum = 20;
            this.sliderBrightness.Maximum = 100;
            this.sliderBrightness.Value = 85;

            this.panelBody.Controls.Add(this.chkHeritage);
            this.panelBody.Controls.Add(this.chkFood);
            this.panelBody.Controls.Add(this.chkStay);
            this.panelBody.Controls.Add(this.chkRoads);
            this.panelBody.Controls.Add(this.chkRiver);
            this.panelBody.Controls.Add(this.chkPiers);
            this.panelBody.Controls.Add(this.sliderBrightness);

            this.Controls.Add(this.panelBody);
            this.Controls.Add(this.panelHeader);
            this.Name = "LayerControlPanel";
            this.Size = new System.Drawing.Size(300, 300);
            this.ResumeLayout(false);
        }
    }
}
