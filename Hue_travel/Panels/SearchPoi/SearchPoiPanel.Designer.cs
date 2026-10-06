using Guna.UI2.WinForms;
using Hue_travel.Theme;

namespace Hue_travel.Panels.SearchPoi
{
    partial class SearchPoiPanel
    {
        private System.ComponentModel.IContainer components = null;
        private Guna2Panel panelHeader;
        private System.Windows.Forms.Label lblTitle;
        private Guna2Button btnClose;
        private Guna2TextBox txtKeyword;
        private System.Windows.Forms.ListBox lstResults;
        private System.Windows.Forms.Label lblResultCount;

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
            this.txtKeyword = new Guna2TextBox();
            this.lstResults = new System.Windows.Forms.ListBox();
            this.lblResultCount = new System.Windows.Forms.Label();
            this.SuspendLayout();

            this.BackColor = ThemeColors.SurfaceContainerLowest;

            this.panelHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelHeader.Height = 32;
            this.panelHeader.FillColor = ThemeColors.Primary;

            this.lblTitle.Text = "Tra Cứu Địa Điểm Du Lịch & Ẩm Thực Huế";
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

            this.txtKeyword.PlaceholderText = "Tìm kiếm: Bún bò, Chùa, Lăng tẩm, Khách sạn...";
            this.txtKeyword.Location = new System.Drawing.Point(8, 40);
            this.txtKeyword.Size = new System.Drawing.Size(368, 32);
            this.txtKeyword.Font = ThemeFonts.BodySm;
            this.txtKeyword.TextChanged += (s, e) => RefreshResults();

            this.lblResultCount.Font = ThemeFonts.BodySm;
            this.lblResultCount.ForeColor = ThemeColors.OnSurfaceVariant;
            this.lblResultCount.AutoSize = true;
            this.lblResultCount.Location = new System.Drawing.Point(8, 78);

            this.lstResults.Font = ThemeFonts.BodySm;
            this.lstResults.Location = new System.Drawing.Point(8, 100);
            this.lstResults.Size = new System.Drawing.Size(368, 220);
            this.lstResults.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lstResults.SelectedIndexChanged += (s, e) => OnResultSelected();

            this.Controls.Add(this.lstResults);
            this.Controls.Add(this.lblResultCount);
            this.Controls.Add(this.txtKeyword);
            this.Controls.Add(this.panelHeader);
            this.Name = "SearchPoiPanel";
            this.Size = new System.Drawing.Size(384, 340);
            this.ResumeLayout(false);
        }
    }
}
