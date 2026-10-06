using Guna.UI2.WinForms;
using Hue_travel.Theme;

namespace Hue_travel.Shell
{
    partial class CategoryRail
    {
        private System.ComponentModel.IContainer components = null;
        private Guna2Panel panelRoot;
        private Guna2Button btnAll;
        private Guna2Button btnHeritage;
        private Guna2Button btnFood;
        private Guna2Button btnStay;
        private Guna2Button btnRiver;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.panelRoot = new Guna2Panel();
            this.btnAll = new Guna2Button();
            this.btnHeritage = new Guna2Button();
            this.btnFood = new Guna2Button();
            this.btnStay = new Guna2Button();
            this.btnRiver = new Guna2Button();
            this.SuspendLayout();

            this.panelRoot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelRoot.FillColor = ThemeColors.SurfaceContainerLowest;

            this.btnAll.Text = "Tất cả";
            this.btnAll.Location = new System.Drawing.Point(4, 8);
            this.btnAll.Size = new System.Drawing.Size(40, 40);
            this.btnAll.Font = ThemeFonts.LabelSm;
            this.btnAll.FillColor = ThemeColors.SurfaceContainer;
            this.btnAll.ForeColor = ThemeColors.Primary;
            this.btnAll.BorderRadius = ThemeMetrics.RadiusLg;
            this.btnAll.Click += new System.EventHandler(this.btnAll_Click);

            this.btnHeritage.Text = "Di tích";
            this.btnHeritage.Location = new System.Drawing.Point(4, 52);
            this.btnHeritage.Size = new System.Drawing.Size(40, 40);
            this.btnHeritage.Font = ThemeFonts.LabelSm;
            this.btnHeritage.FillColor = ThemeColors.SurfaceContainerLowest;
            this.btnHeritage.ForeColor = ThemeColors.OnSurfaceVariant;
            this.btnHeritage.BorderRadius = ThemeMetrics.RadiusLg;
            this.btnHeritage.Click += new System.EventHandler(this.btnHeritage_Click);

            this.btnFood.Text = "Ẩm thực";
            this.btnFood.Location = new System.Drawing.Point(4, 96);
            this.btnFood.Size = new System.Drawing.Size(40, 40);
            this.btnFood.Font = ThemeFonts.LabelSm;
            this.btnFood.FillColor = ThemeColors.SurfaceContainerLowest;
            this.btnFood.ForeColor = ThemeColors.OnSurfaceVariant;
            this.btnFood.BorderRadius = ThemeMetrics.RadiusLg;
            this.btnFood.Click += new System.EventHandler(this.btnFood_Click);

            this.btnStay.Text = "Nghỉ ngơi";
            this.btnStay.Location = new System.Drawing.Point(4, 140);
            this.btnStay.Size = new System.Drawing.Size(40, 40);
            this.btnStay.Font = ThemeFonts.LabelSm;
            this.btnStay.FillColor = ThemeColors.SurfaceContainerLowest;
            this.btnStay.ForeColor = ThemeColors.OnSurfaceVariant;
            this.btnStay.BorderRadius = ThemeMetrics.RadiusLg;
            this.btnStay.Click += new System.EventHandler(this.btnStay_Click);

            this.btnRiver.Text = "Sông Hương";
            this.btnRiver.Location = new System.Drawing.Point(4, 184);
            this.btnRiver.Size = new System.Drawing.Size(40, 40);
            this.btnRiver.Font = ThemeFonts.LabelSm;
            this.btnRiver.FillColor = ThemeColors.SurfaceContainerLowest;
            this.btnRiver.ForeColor = ThemeColors.OnSurfaceVariant;
            this.btnRiver.BorderRadius = ThemeMetrics.RadiusLg;
            this.btnRiver.Click += new System.EventHandler(this.btnRiver_Click);

            this.panelRoot.Controls.Add(this.btnAll);
            this.panelRoot.Controls.Add(this.btnHeritage);
            this.panelRoot.Controls.Add(this.btnFood);
            this.panelRoot.Controls.Add(this.btnStay);
            this.panelRoot.Controls.Add(this.btnRiver);

            this.Controls.Add(this.panelRoot);
            this.Name = "CategoryRail";
            this.Size = new System.Drawing.Size(48, 700);
            this.ResumeLayout(false);
        }
    }
}
