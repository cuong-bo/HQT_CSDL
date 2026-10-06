using System;
using System.Windows.Forms;

namespace Hue_travel.Panels.LayerControl
{
    public partial class LayerControlPanel : UserControl
    {
        public event EventHandler CloseRequested;

        public LayerControlPanel()
        {
            InitializeComponent();
        }
    }
}
