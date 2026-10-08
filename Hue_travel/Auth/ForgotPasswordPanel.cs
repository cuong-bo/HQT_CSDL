using System;
using Hue_travel.Theme;

namespace Hue_travel.Auth
{
    /// <summary>UI-only "forgot password" card (email, OTP, new password). The back link asks the host to return to the login form.</summary>
    public partial class ForgotPasswordPanel : FloatingPanelForm
    {
        public event EventHandler BackToLoginRequested;

        public ForgotPasswordPanel()
        {
            InitializeComponent();
        }

        private void header_CloseClicked(object sender, EventArgs e) => RaiseCloseRequested();

        private void btnBack_Click(object sender, EventArgs e) => BackToLoginRequested?.Invoke(this, EventArgs.Empty);
    }
}
