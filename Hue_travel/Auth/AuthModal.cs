using System;
using Hue_travel.Theme;

namespace Hue_travel.Auth
{
    /// <summary>UI-only login card (no real authentication in this phase). The register tab and the forgot-password link ask the host to switch forms.</summary>
    public partial class AuthModal : FloatingPanelForm
    {
        public event EventHandler RegisterRequested;
        public event EventHandler ForgotPasswordRequested;

        public AuthModal()
        {
            InitializeComponent();
        }

        private void header_CloseClicked(object sender, EventArgs e) => RaiseCloseRequested();

        private void tabs_TabSelected(object sender, int tab)
        {
            if (tab == 1) RegisterRequested?.Invoke(this, EventArgs.Empty);
        }

        private void lblForgot_Click(object sender, EventArgs e) => ForgotPasswordRequested?.Invoke(this, EventArgs.Empty);
    }
}
