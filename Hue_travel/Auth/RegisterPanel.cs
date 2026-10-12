using System;
using Hue_travel.Services.Online;
using Hue_travel.Theme;

namespace Hue_travel.Auth
{
    /// <summary>Registration card (user name, display name, password). The login tab asks the host to switch back.</summary>
    public partial class RegisterPanel : FloatingPanelForm
    {
        public event EventHandler LoginRequested;

        /// <summary>Raised after the account was created and signed in.</summary>
        public event EventHandler Registered;

        public AuthService Service { get; set; }

        public RegisterPanel()
        {
            InitializeComponent();
            btnRegister.Click += async (s, e) => await SubmitAsync();
        }

        private async System.Threading.Tasks.Task SubmitAsync()
        {
            txtFullName.HasError = !AuthValidation.IsFullName(txtFullName.Text);
            txtEmail.HasError = !AuthValidation.IsUsername(txtEmail.Text);   // this box holds the user name
            txtPassword.HasError = !AuthValidation.IsStrongEnoughPassword(txtPassword.Text);
            if (txtFullName.HasError) { Notify("Vui lòng nhập họ và tên", ToastKind.Error); return; }
            if (txtEmail.HasError) { Notify("Tên đăng nhập gồm 3 đến 40 ký tự: chữ, số, _ hoặc .", ToastKind.Error); return; }
            if (txtPassword.HasError) { Notify("Mật khẩu cần tối thiểu " + AuthValidation.MinPasswordLength + " ký tự", ToastKind.Error); return; }
            if (Service == null) { Notify("Chưa có địa chỉ máy chủ nên chưa đăng ký được (xem hướng dẫn cài đặt)", ToastKind.Info); return; }

            btnRegister.Enabled = false;
            try
            {
                string error = await Service.RegisterAsync(txtEmail.Text.Trim(), txtPassword.Text, txtFullName.Text.Trim(), true);
                if (error != null) { Notify(error, ToastKind.Error); return; }
                txtPassword.Text = "";
                Registered?.Invoke(this, EventArgs.Empty);
            }
            finally
            {
                btnRegister.Enabled = true;
            }
        }

        private void header_CloseClicked(object sender, EventArgs e) => RaiseCloseRequested();

        private void tabs_TabSelected(object sender, int tab)
        {
            if (tab == 0) LoginRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
