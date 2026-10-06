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

        private void panelBackdrop_Click(object sender, EventArgs e) => CloseRequested?.Invoke(this, EventArgs.Empty);

        private void btnClose_Click(object sender, EventArgs e) => CloseRequested?.Invoke(this, EventArgs.Empty);

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (panelCard == null) return;
            panelCard.Location = new System.Drawing.Point(
                (Width - panelCard.Width) / 2,
                (Height - panelCard.Height) / 2);
        }
    }
}
