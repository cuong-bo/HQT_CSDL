using System;
using System.Windows.Forms;

namespace Hue_travel.Panels.BaseMapSwitcher
{
    public partial class BaseMapSwitcherPopup : UserControl
    {
        public event EventHandler<string> BaseMapSelected;

        public BaseMapSwitcherPopup()
        {
            InitializeComponent();
        }
    }
}
