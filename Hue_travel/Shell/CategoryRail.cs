using System;
using System.Windows.Forms;
using Hue_travel.Models;

namespace Hue_travel.Shell
{
    public partial class CategoryRail : UserControl
    {
        public event EventHandler<PoiCategory?> CategorySelected;

        public CategoryRail()
        {
            InitializeComponent();
        }

        private void btnAll_Click(object sender, EventArgs e) => CategorySelected?.Invoke(this, null);

        private void btnHeritage_Click(object sender, EventArgs e) => CategorySelected?.Invoke(this, PoiCategory.Heritage);

        private void btnFood_Click(object sender, EventArgs e) => CategorySelected?.Invoke(this, PoiCategory.Food);

        private void btnStay_Click(object sender, EventArgs e) => CategorySelected?.Invoke(this, PoiCategory.Stay);

        private void btnRiver_Click(object sender, EventArgs e) => CategorySelected?.Invoke(this, PoiCategory.River);
    }
}
