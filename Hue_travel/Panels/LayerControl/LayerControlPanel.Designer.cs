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

        private Guna2CheckBox MakeLayerCheck(string text, int y, bool @checked)
        {
            return new Guna2CheckBox
            {
                Text = text,
                Checked = @checked,
                Location = new System.Drawing.Point(8, y),
                Size = new System.Drawing.Size(270, 22),
                Font = ThemeFonts.BodySm,
                CheckedState = { BorderColor = ThemeColors.Primary, FillColor = ThemeColors.Primary }
            };
        }

        private void InitializeComponent()
        {
            this.panelHeader = new Guna2Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.btnClose = new Guna2Button();
            this.panelBody = new Guna2Panel();
            this.sliderBrightness = new Guna2TrackBar();
            this.SuspendLayout();

            this.BackColor = ThemeColors.SurfaceContainerLowest;

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
            this.btnClose.Click += (s, e) => CloseRequested?.Invoke(this, System.EventArgs.Empty);

            this.panelHeader.Controls.Add(this.lblTitle);
            this.panelHeader.Controls.Add(this.btnClose);

            this.panelBody.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelBody.FillColor = ThemeColors.SurfaceContainerLowest;

            this.chkHeritage = MakeLayerCheck("Di tích lịch sử & Điểm tham quan (38 điểm)", 8, true);
            this.chkFood = MakeLayerCheck("Món ngon & Quán đặc sản Huế (64 quán)", 36, true);
            this.chkStay = MakeLayerCheck("Khách sạn & Nơi lưu trú (52 nơi)", 64, true);
            this.chkRoads = MakeLayerCheck("Tuyến đường chính & Cầu qua sông", 92, true);
            this.chkRiver = MakeLayerCheck("Sông Hương & Các hồ cảnh quan", 120, true);
            this.chkPiers = MakeLayerCheck("Bến thuyền rồng & Điểm xe buýt", 148, false);

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
