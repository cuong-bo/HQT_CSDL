using System;
using System.Drawing;
using System.Windows.Forms;
using Hue_travel.Models;
using Hue_travel.Theme;

namespace Hue_travel.Shell
{
    public partial class CategoryRail : UserControl
    {
        public event EventHandler<PoiCategory?> CategorySelected;

        public CategoryRail()
        {
            InitializeComponent();
        }

        private void btnAll_Click(object sender, EventArgs e) => Select(btnAll, null);

        private void btnHeritage_Click(object sender, EventArgs e) => Select(btnHeritage, PoiCategory.Heritage);

        private void btnFood_Click(object sender, EventArgs e) => Select(btnFood, PoiCategory.Food);

        private void btnStay_Click(object sender, EventArgs e) => Select(btnStay, PoiCategory.Stay);

        private void btnRiver_Click(object sender, EventArgs e) => Select(btnRiver, PoiCategory.River);

        private void Select(IconButton active, PoiCategory? category)
        {
            foreach (IconButton tile in new[] { btnAll, btnHeritage, btnFood, btnStay, btnRiver })
            {
                bool isActive = tile == active;
                tile.FillColor = isActive ? ThemeColors.Sky100 : Color.Transparent;
                tile.HoverFillColor = isActive ? ThemeColors.Sky100 : ThemeColors.Slate100;
                tile.ForeColor = isActive ? ThemeColors.Primary : ThemeColors.Slate600;
                tile.Font = isActive ? ThemeFonts.Bold(9) : ThemeFonts.Medium(9);
                tile.Invalidate();
            }
            CategorySelected?.Invoke(this, category);
        }
    }
}
