using System;
using System.Windows.Forms;

namespace Hue_travel.Panels.LayerControl
{
    public partial class LayerControlPanel : Form
    {
        public event EventHandler CloseRequested;

        public LayerControlPanel()
        {
            InitializeComponent();
        }

        private void btnClose_Click(object sender, EventArgs e) => CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
