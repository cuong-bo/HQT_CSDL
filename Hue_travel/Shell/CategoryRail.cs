using System;
using System.Drawing;
using System.Windows.Forms;
using Hue_travel.Models;
using Hue_travel.Theme;

namespace Hue_travel.Shell
{
    public partial class CategoryRail : UserControl
    {
        /// <summary>The picked kind of object; null means "Tất cả" (everything).</summary>
        public event EventHandler<PoiCategory?> CategorySelected;

        public CategoryRail()
        {
            InitializeComponent();
        }

        private void btnAll_Click(object sender, EventArgs e) => Select(btnAll, null);

        private void btnHeritage_Click(object sender, EventArgs e) => Select(btnHeritage, PoiCategory.Heritage);

        private void btnScenery_Click(object sender, EventArgs e) => Select(btnScenery, PoiCategory.Scenery);

        private void btnFood_Click(object sender, EventArgs e) => Select(btnFood, PoiCategory.Food);

        private void btnStay_Click(object sender, EventArgs e) => Select(btnStay, PoiCategory.Stay);

        private void btnService_Click(object sender, EventArgs e) => Select(btnService, PoiCategory.Service);

        private void btnTransport_Click(object sender, EventArgs e) => Select(btnTransport, PoiCategory.Transport);

        private void Select(IconButton active, PoiCategory? category)
        {
            foreach (IconButton tile in new[] { btnAll, btnHeritage, btnScenery, btnFood, btnStay, btnService, btnTransport })
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
