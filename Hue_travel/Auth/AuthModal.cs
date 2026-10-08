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
            btnLogin.Click += (s, e) => Submit();
        }

        private void Submit()
        {
            txtLoginId.HasError = !AuthValidation.IsEmailOrPhone(txtLoginId.Text);
            txtLoginPassword.HasError = !AuthValidation.IsStrongEnoughPassword(txtLoginPassword.Text);
            if (txtLoginId.HasError)
            {
                Notify("Email hoặc số điện thoại chưa hợp lệ", ToastKind.Error);
                return;
            }
            if (txtLoginPassword.HasError)
            {
                Notify("Mật khẩu cần tối thiểu " + AuthValidation.MinPasswordLength + " ký tự", ToastKind.Error);
                return;
            }
            Notify("Thông tin hợp lệ. Chức năng đăng nhập sẽ hoạt động khi kết nối CSDL tài khoản", ToastKind.Info);
        }

        private void header_CloseClicked(object sender, EventArgs e) => RaiseCloseRequested();

        private void tabs_TabSelected(object sender, int tab)
        {
            if (tab == 1) RegisterRequested?.Invoke(this, EventArgs.Empty);
        }

        private void lblForgot_Click(object sender, EventArgs e) => ForgotPasswordRequested?.Invoke(this, EventArgs.Empty);
    }
}
