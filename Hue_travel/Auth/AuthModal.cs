using System;
using System.Windows.Forms;

namespace Hue_travel.Auth
{
    public partial class AuthModal : UserControl
    {
        public event EventHandler CloseRequested;

        public AuthModal()
        {
            InitializeComponent();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            panelCard.Location = new System.Drawing.Point(
                (Width - panelCard.Width) / 2,
                (Height - panelCard.Height) / 2);
        }
    }
}
