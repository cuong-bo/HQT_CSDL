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
    }
}
