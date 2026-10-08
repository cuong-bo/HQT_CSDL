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
            btnRegister.Click += (s, e) => Submit();
        }

        private void Submit()
        {
            txtFullName.HasError = !AuthValidation.IsFullName(txtFullName.Text);
            txtEmail.HasError = !AuthValidation.IsEmail(txtEmail.Text);
            txtPassword.HasError = !AuthValidation.IsStrongEnoughPassword(txtPassword.Text);
            if (txtFullName.HasError) { Notify("Vui lòng nhập họ và tên du khách", ToastKind.Error); return; }
            if (txtEmail.HasError) { Notify("Email chưa hợp lệ", ToastKind.Error); return; }
            if (txtPassword.HasError) { Notify("Mật khẩu cần tối thiểu " + AuthValidation.MinPasswordLength + " ký tự", ToastKind.Error); return; }
            Notify("Thông tin hợp lệ. Đăng ký sẽ hoạt động khi kết nối CSDL tài khoản", ToastKind.Info);
        }

        private void header_CloseClicked(object sender, EventArgs e) => RaiseCloseRequested();

        private void tabs_TabSelected(object sender, int tab)
        {
            if (tab == 0) LoginRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
