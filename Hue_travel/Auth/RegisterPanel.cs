using System;
using Hue_travel.Theme;

namespace Hue_travel.Auth
{
    /// <summary>UI-only visitor registration card. The login tab asks the host to switch back to the login form.</summary>
    public partial class RegisterPanel : FloatingPanelForm
    {
        public event EventHandler LoginRequested;

        public RegisterPanel()
        {
            InitializeComponent();
        }

        private void header_CloseClicked(object sender, EventArgs e) => RaiseCloseRequested();

        private void tabs_TabSelected(object sender, int tab)
        {
            if (tab == 0) LoginRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
