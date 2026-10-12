using System;
using Hue_travel.Services.Online;
using Hue_travel.Theme;

namespace Hue_travel.Auth
{
    /// <summary>Login card. The register tab asks the host to switch forms; the host sets <see cref="Service"/> when a server is configured.</summary>
    public partial class AuthModal : FloatingPanelForm
    {
        public event EventHandler RegisterRequested;

        /// <summary>Raised after a successful login.</summary>
        public event EventHandler Authenticated;

        public AuthService Service { get; set; }

        public AuthModal()
        {
            InitializeComponent();
            btnLogin.Click += async (s, e) => await SubmitAsync();
        }

        private async System.Threading.Tasks.Task SubmitAsync()
        {
            txtLoginId.HasError = !AuthValidation.IsUsername(txtLoginId.Text);
            txtLoginPassword.HasError = string.IsNullOrEmpty(txtLoginPassword.Text);
            if (txtLoginId.HasError) { Notify("Tên đăng nhập gồm 3 đến 40 ký tự: chữ, số, _ hoặc .", ToastKind.Error); return; }
            if (txtLoginPassword.HasError) { Notify("Hãy nhập mật khẩu", ToastKind.Error); return; }
            if (Service == null) { Notify("Chưa có địa chỉ máy chủ nên chưa đăng nhập được (xem hướng dẫn cài đặt)", ToastKind.Info); return; }

            btnLogin.Enabled = false;
            try
            {
                string error = await Service.LoginAsync(txtLoginId.Text.Trim(), txtLoginPassword.Text, chkRemember.Checked);
                if (error != null) { Notify(error, ToastKind.Error); return; }
                txtLoginPassword.Text = "";
                Authenticated?.Invoke(this, EventArgs.Empty);
            }
            finally
            {
                btnLogin.Enabled = true;
            }
        }

        private void header_CloseClicked(object sender, EventArgs e) => RaiseCloseRequested();

        private void tabs_TabSelected(object sender, int tab)
        {
            if (tab == 1) RegisterRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
