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
        private IconButton btnScenery;
        private IconButton btnFood;
        private IconButton btnStay;
        private IconButton btnService;
        private IconButton btnTransport;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.panelRightBorder = new Panel();
            this.btnAll = new IconButton();
            this.btnHeritage = new IconButton();
            this.btnScenery = new IconButton();
            this.btnFood = new IconButton();
            this.btnStay = new IconButton();
            this.btnService = new IconButton();
            this.btnTransport = new IconButton();
            this.SuspendLayout();

            // w-12 white rail with a 1px slate-200 right border; items are 9px-captioned icon tiles.
            this.BackColor = ThemeColors.SurfaceContainerLowest;

            this.panelRightBorder.Dock = DockStyle.Right;
            this.panelRightBorder.Width = 1;
            this.panelRightBorder.BackColor = ThemeColors.Slate200;

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
            this.btnAll.Size = new Size(40, 54);
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
            this.btnHeritage.Size = new Size(40, 54);
            this.btnHeritage.Location = new Point(4, 70);
            this.btnHeritage.Click += new System.EventHandler(this.btnHeritage_Click);

            this.btnScenery.AutoWidth = false;
            this.btnScenery.Stacked = true;
            this.btnScenery.Text = "Cảnh quan";
            this.btnScenery.IconName = "landscape";
            this.btnScenery.IconSize = 18;
            this.btnScenery.Gap = 2;
            this.btnScenery.Font = ThemeFonts.Medium(9);
            this.btnScenery.ForeColor = ThemeColors.Slate600;
            this.btnScenery.IconColor = ThemeColors.Emerald700;
            this.btnScenery.FillColor = Color.Transparent;
            this.btnScenery.HoverFillColor = ThemeColors.Slate100;
            this.btnScenery.Radius = 6;
            this.btnScenery.Size = new Size(40, 54);
            this.btnScenery.Location = new Point(4, 130);
            this.btnScenery.Click += new System.EventHandler(this.btnScenery_Click);

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
            this.btnFood.Size = new Size(40, 54);
            this.btnFood.Location = new Point(4, 190);
            this.btnFood.Click += new System.EventHandler(this.btnFood_Click);

            this.btnStay.AutoWidth = false;
            this.btnStay.Stacked = true;
            this.btnStay.Text = "Lưu trú";
            this.btnStay.IconName = "hotel";
            this.btnStay.IconSize = 18;
            this.btnStay.Gap = 2;
            this.btnStay.Font = ThemeFonts.Medium(9);
            this.btnStay.ForeColor = ThemeColors.Slate600;
            this.btnStay.IconColor = ThemeColors.Blue600;
            this.btnStay.FillColor = Color.Transparent;
            this.btnStay.HoverFillColor = ThemeColors.Slate100;
            this.btnStay.Radius = 6;
            this.btnStay.Size = new Size(40, 54);
            this.btnStay.Location = new Point(4, 250);
            this.btnStay.Click += new System.EventHandler(this.btnStay_Click);

            this.btnService.AutoWidth = false;
            this.btnService.Stacked = true;
            this.btnService.Text = "Dịch vụ";
            this.btnService.IconName = "support_agent";
            this.btnService.IconSize = 18;
            this.btnService.Gap = 2;
            this.btnService.Font = ThemeFonts.Medium(9);
            this.btnService.ForeColor = ThemeColors.Slate600;
            this.btnService.IconColor = ThemeColors.Sky600;
            this.btnService.FillColor = Color.Transparent;
            this.btnService.HoverFillColor = ThemeColors.Slate100;
            this.btnService.Radius = 6;
            this.btnService.Size = new Size(40, 54);
            this.btnService.Location = new Point(4, 310);
            this.btnService.Click += new System.EventHandler(this.btnService_Click);

            this.btnTransport.AutoWidth = false;
            this.btnTransport.Stacked = true;
            this.btnTransport.Text = "Giao thông";
            this.btnTransport.IconName = "train";
            this.btnTransport.IconSize = 18;
            this.btnTransport.Gap = 2;
            this.btnTransport.Font = ThemeFonts.Medium(9);
            this.btnTransport.ForeColor = ThemeColors.Slate600;
            this.btnTransport.IconColor = ThemeColors.Cyan600;
            this.btnTransport.FillColor = Color.Transparent;
            this.btnTransport.HoverFillColor = ThemeColors.Slate100;
            this.btnTransport.Radius = 6;
            this.btnTransport.Size = new Size(40, 54);
            this.btnTransport.Location = new Point(4, 370);
            this.btnTransport.Click += new System.EventHandler(this.btnTransport_Click);

            this.Controls.Add(this.btnTransport);
            this.Controls.Add(this.btnService);
            this.Controls.Add(this.btnStay);
            this.Controls.Add(this.btnFood);
            this.Controls.Add(this.btnScenery);
            this.Controls.Add(this.btnHeritage);
            this.Controls.Add(this.btnAll);
            this.Controls.Add(this.panelRightBorder);
            this.Name = "CategoryRail";
            this.Size = new Size(48, 700);
            this.ResumeLayout(false);
        }
    }
}
