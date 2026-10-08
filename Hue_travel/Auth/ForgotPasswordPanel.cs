using System;
using Hue_travel.Theme;

namespace Hue_travel.Auth
{
    /// <summary>UI-only "forgot password" card (email, OTP, new password). The back link asks the host to return to the login form.</summary>
    public partial class ForgotPasswordPanel : FloatingPanelForm
    {
        public event EventHandler BackToLoginRequested;

        private const int ResendSeconds = 59;

        private readonly System.Windows.Forms.Timer countdown = new System.Windows.Forms.Timer { Interval = 1000 };
        private int secondsLeft;

        public ForgotPasswordPanel()
        {
            InitializeComponent();
            countdown.Tick += (s, e) => TickCountdown();
            Disposed += (s, e) => countdown.Dispose();
            btnResend.Click += (s, e) => Resend();
            btnConfirm.Click += (s, e) => Submit();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible && !DesignMode) StartCountdown();
            else countdown.Stop();
        }

        private void StartCountdown()
        {
            secondsLeft = ResendSeconds;
            ShowCountdown();
            countdown.Start();
        }

        private void TickCountdown()
        {
            if (secondsLeft > 0) secondsLeft--;
            ShowCountdown();
            if (secondsLeft == 0) countdown.Stop();
        }

        private void ShowCountdown()
        {
            btnResend.Text = secondsLeft > 0 ? "Gửi lại mã (" + secondsLeft + "s)" : "Gửi lại mã";
        }

        private void Resend()
        {
            if (secondsLeft > 0)
            {
                Notify("Vui lòng chờ " + secondsLeft + " giây để gửi lại mã", ToastKind.Info);
                return;
            }
            Notify("Đã gửi lại mã OTP (giao diện mẫu, chưa gửi email thật)", ToastKind.Info);
            StartCountdown();
        }

        private void Submit()
        {
            txtEmail.HasError = !AuthValidation.IsEmail(txtEmail.Text);
            txtOtp.HasError = !AuthValidation.IsOtp(txtOtp.Text);
            txtNewPassword.HasError = !AuthValidation.IsStrongEnoughPassword(txtNewPassword.Text);
            txtConfirmPassword.HasError = txtConfirmPassword.Text != txtNewPassword.Text;
            if (txtEmail.HasError) { Notify("Email chưa hợp lệ", ToastKind.Error); return; }
            if (txtOtp.HasError) { Notify("Mã OTP gồm " + AuthValidation.OtpLength + " chữ số", ToastKind.Error); return; }
            if (txtNewPassword.HasError) { Notify("Mật khẩu mới cần tối thiểu " + AuthValidation.MinPasswordLength + " ký tự", ToastKind.Error); return; }
            if (txtConfirmPassword.HasError) { Notify("Mật khẩu xác nhận chưa khớp", ToastKind.Error); return; }
            Notify("Thông tin hợp lệ. Đổi mật khẩu sẽ hoạt động khi kết nối CSDL tài khoản", ToastKind.Info);
        }

        private void header_CloseClicked(object sender, EventArgs e) => RaiseCloseRequested();

        private void btnBack_Click(object sender, EventArgs e) => BackToLoginRequested?.Invoke(this, EventArgs.Empty);
    }
}
