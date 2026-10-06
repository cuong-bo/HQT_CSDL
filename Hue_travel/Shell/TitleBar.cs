using System;
using System.Windows.Forms;

namespace Hue_travel.Shell
{
    public partial class TitleBar : UserControl
    {
        public event EventHandler AuthClicked;
        public event EventHandler MinimizeClicked;
        public event EventHandler MaximizeClicked;
        public event EventHandler CloseClicked;

        public TitleBar()
        {
            InitializeComponent();
        }
    }
}
