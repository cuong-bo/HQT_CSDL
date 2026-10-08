using System.Drawing;
using System.Windows.Forms;
using Hue_travel.Theme;

namespace Hue_travel.Shell
{
    partial class CategoryRail
    {
        private System.ComponentModel.IContainer components = null;
        private Panel panelRightBorder;
        private IconButton btnAll;
        private IconButton btnHeritage;
        private IconButton btnFood;
        private IconButton btnStay;
        private IconButton btnRiver;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        // Every property is assigned inline (no helper calls) so the WinForms Designer can render it.
        private void InitializeComponent()
        {
            this.panelRightBorder = new Panel();
            this.btnAll = new IconButton();
            this.btnHeritage = new IconButton();
            this.btnFood = new IconButton();
            this.btnStay = new IconButton();
            this.btnRiver = new IconButton();
            this.SuspendLayout();

            // w-12 white rail with a 1px slate-200 right border; items are 9px-captioned icon tiles.
            this.BackColor = ThemeColors.SurfaceContainerLowest;

            this.panelRightBorder.Dock = DockStyle.Right;
            this.panelRightBorder.Width = 1;
            this.panelRightBorder.BackColor = ThemeColors.Slate200;

            // "Tất cả": active tile (sky-100 fill, primary icon, bold caption).
            this.btnAll.AutoWidth = false;
            this.btnAll.Stacked = true;
            this.btnAll.Text = "Tất cả";
            this.btnAll.IconName = "apps";
            this.btnAll.IconSize = 18;
            this.btnAll.Gap = 2;
            this.btnAll.Font = ThemeFonts.Bold(9);
            this.btnAll.ForeColor = ThemeColors.Primary;
            this.btnAll.IconColor = ThemeColors.Primary;
            this.btnAll.FillColor = ThemeColors.Sky100;
            this.btnAll.HoverFillColor = ThemeColors.Sky100;
            this.btnAll.Radius = 6;
            this.btnAll.Size = new Size(40, 50);
            this.btnAll.Location = new Point(4, 10);
            this.btnAll.Click += new System.EventHandler(this.btnAll_Click);

            this.btnHeritage.AutoWidth = false;
            this.btnHeritage.Stacked = true;
            this.btnHeritage.Text = "Di tích";
            this.btnHeritage.IconName = "account_balance";
            this.btnHeritage.IconSize = 18;
            this.btnHeritage.Gap = 2;
            this.btnHeritage.Font = ThemeFonts.Medium(9);
            this.btnHeritage.ForeColor = ThemeColors.Slate600;
            this.btnHeritage.IconColor = ThemeColors.Amber600;
            this.btnHeritage.FillColor = Color.Transparent;
            this.btnHeritage.HoverFillColor = ThemeColors.Slate100;
            this.btnHeritage.Radius = 6;
            this.btnHeritage.Size = new Size(40, 50);
            this.btnHeritage.Location = new Point(4, 66);
            this.btnHeritage.Click += new System.EventHandler(this.btnHeritage_Click);

            this.btnFood.AutoWidth = false;
            this.btnFood.Stacked = true;
            this.btnFood.Text = "Ẩm thực";
            this.btnFood.IconName = "restaurant";
            this.btnFood.IconSize = 18;
            this.btnFood.Gap = 2;
            this.btnFood.Font = ThemeFonts.Medium(9);
            this.btnFood.ForeColor = ThemeColors.Slate600;
            this.btnFood.IconColor = ThemeColors.Orange600;
            this.btnFood.FillColor = Color.Transparent;
            this.btnFood.HoverFillColor = ThemeColors.Slate100;
            this.btnFood.Radius = 6;
            this.btnFood.Size = new Size(40, 50);
            this.btnFood.Location = new Point(4, 122);
            this.btnFood.Click += new System.EventHandler(this.btnFood_Click);

            this.btnStay.AutoWidth = false;
            this.btnStay.Stacked = true;
            this.btnStay.Text = "Nghỉ ngơi";
            this.btnStay.IconName = "hotel";
            this.btnStay.IconSize = 18;
            this.btnStay.Gap = 2;
            this.btnStay.Font = ThemeFonts.Medium(9);
            this.btnStay.ForeColor = ThemeColors.Slate600;
            this.btnStay.IconColor = ThemeColors.Blue600;
            this.btnStay.FillColor = Color.Transparent;
            this.btnStay.HoverFillColor = ThemeColors.Slate100;
            this.btnStay.Radius = 6;
            this.btnStay.Size = new Size(40, 50);
            this.btnStay.Location = new Point(4, 178);
            this.btnStay.Click += new System.EventHandler(this.btnStay_Click);

            this.btnRiver.AutoWidth = false;
            this.btnRiver.Stacked = true;
            this.btnRiver.Text = "Sông Hương";
            this.btnRiver.IconName = "kayaking";
            this.btnRiver.IconSize = 18;
            this.btnRiver.Gap = 2;
            this.btnRiver.Font = ThemeFonts.Medium(9);
            this.btnRiver.ForeColor = ThemeColors.Slate600;
            this.btnRiver.IconColor = ThemeColors.Cyan600;
            this.btnRiver.FillColor = Color.Transparent;
            this.btnRiver.HoverFillColor = ThemeColors.Slate100;
            this.btnRiver.Radius = 6;
            this.btnRiver.Size = new Size(40, 50);
            this.btnRiver.Location = new Point(4, 234);
            this.btnRiver.Click += new System.EventHandler(this.btnRiver_Click);

            this.Controls.Add(this.btnRiver);
            this.Controls.Add(this.btnStay);
            this.Controls.Add(this.btnFood);
            this.Controls.Add(this.btnHeritage);
            this.Controls.Add(this.btnAll);
            this.Controls.Add(this.panelRightBorder);
            this.Name = "CategoryRail";
            this.Size = new Size(48, 700);
            this.ResumeLayout(false);
        }
    }
}
