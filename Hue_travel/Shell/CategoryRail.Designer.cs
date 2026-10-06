using Guna.UI2.WinForms;
using Hue_travel.Theme;

namespace Hue_travel.Shell
{
    partial class CategoryRail
    {
        private System.ComponentModel.IContainer components = null;
        private Guna2Panel panelRoot;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private Guna2Button MakeRailButton(string text, int y)
        {
            return new Guna2Button
            {
                Text = text,
                Location = new System.Drawing.Point(4, y),
                Size = new System.Drawing.Size(40, 40),
                Font = ThemeFonts.LabelSm,
                FillColor = ThemeColors.SurfaceContainerLowest,
                ForeColor = ThemeColors.OnSurfaceVariant,
                BorderRadius = ThemeMetrics.RadiusLg
            };
        }

        private void InitializeComponent()
        {
            this.panelRoot = new Guna2Panel();
            this.SuspendLayout();

            this.panelRoot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelRoot.FillColor = ThemeColors.SurfaceContainerLowest;

            var btnAll = MakeRailButton("Tất cả", 8);
            btnAll.FillColor = ThemeColors.SurfaceContainer;
            btnAll.ForeColor = ThemeColors.Primary;
            btnAll.Click += (s, e) => CategorySelected?.Invoke(this, null);

            var btnHeritage = MakeRailButton("Di tích", 52);
            btnHeritage.Click += (s, e) => CategorySelected?.Invoke(this, Models.PoiCategory.Heritage);

            var btnFood = MakeRailButton("Ẩm thực", 96);
            btnFood.Click += (s, e) => CategorySelected?.Invoke(this, Models.PoiCategory.Food);

            var btnStay = MakeRailButton("Nghỉ ngơi", 140);
            btnStay.Click += (s, e) => CategorySelected?.Invoke(this, Models.PoiCategory.Stay);

            var btnRiver = MakeRailButton("Sông Hương", 184);
            btnRiver.Click += (s, e) => CategorySelected?.Invoke(this, Models.PoiCategory.River);

            this.panelRoot.Controls.Add(btnAll);
            this.panelRoot.Controls.Add(btnHeritage);
            this.panelRoot.Controls.Add(btnFood);
            this.panelRoot.Controls.Add(btnStay);
            this.panelRoot.Controls.Add(btnRiver);

            this.Controls.Add(this.panelRoot);
            this.Name = "CategoryRail";
            this.Size = new System.Drawing.Size(48, 700);
            this.ResumeLayout(false);
        }
    }
}
