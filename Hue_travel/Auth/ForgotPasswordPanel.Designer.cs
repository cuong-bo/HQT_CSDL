using System.Drawing;
using System.Windows.Forms;
using Hue_travel.Theme;

namespace Hue_travel.Auth
{
    partial class ForgotPasswordPanel
    {
        private System.ComponentModel.IContainer components = null;
        private PanelHeader header;
        private RoundedPanel boxInfo;
        private IconButton iconInfo;
        private RichTextBlock textInfo;
        private Label lblEmail;
        private InputField txtEmail;
        private Label lblOtp;
        private InputField txtOtp;
        private IconButton btnResend;
        private Label lblNewPassword;
        private InputField txtNewPassword;
        private Label lblConfirmPassword;
        private InputField txtConfirmPassword;
        private IconButton btnConfirm;
        private IconButton btnBack;
        private Panel dividerFooter;
        private IconButton captionHelp;
        private Label lblHotline;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        // Every property is assigned inline (no helper calls) so the WinForms Designer can render it.
        private void InitializeComponent()
        {
            this.header = new PanelHeader();
            this.boxInfo = new RoundedPanel();
            this.iconInfo = new IconButton();
            this.textInfo = new RichTextBlock();
            this.lblEmail = new Label();
            this.txtEmail = new InputField();
            this.lblOtp = new Label();
            this.txtOtp = new InputField();
            this.btnResend = new IconButton();
            this.lblNewPassword = new Label();
            this.txtNewPassword = new InputField();
            this.lblConfirmPassword = new Label();
            this.txtConfirmPassword = new InputField();
            this.btnConfirm = new IconButton();
            this.btnBack = new IconButton();
            this.dividerFooter = new Panel();
            this.captionHelp = new IconButton();
            this.lblHotline = new Label();
            this.SuspendLayout();
            this.boxInfo.SuspendLayout();

            // w-[440px] card.
            this.BackColor = ThemeColors.SurfaceContainerLowest;
            this.Size = new Size(440, 508);

            this.header.IconName = "lock_reset";
            this.header.Text = "Khôi Phục Mật Khẩu - Cố Đô GIS";
            this.header.Prominent = true;
            this.header.ShowMinimize = false;
            this.header.CloseClicked += new System.EventHandler(this.header_CloseClicked);

            // Info box: sky-50 / sky-100 with verified_user icon.
            this.boxInfo.FillColor = ThemeColors.Sky50;
            this.boxInfo.BorderColor = ThemeColors.Sky100;
            this.boxInfo.Size = new Size(408, 60);
            this.boxInfo.Location = new Point(16, 48);
            this.iconInfo.AutoWidth = false;
            this.iconInfo.IconName = "verified_user";
            this.iconInfo.IconSize = 18;
            this.iconInfo.IconColor = ThemeColors.Primary;
            this.iconInfo.FillColor = Color.Transparent;
            this.iconInfo.HoverFillColor = Color.Transparent;
            this.iconInfo.Cursor = Cursors.Default;
            this.iconInfo.Size = new Size(18, 18);
            this.iconInfo.Location = new Point(10, 10);
            this.textInfo.Text = "Nhập địa chỉ email, mã OTP nhận được và thiết lập mật khẩu mới để hoàn tất khôi phục quyền truy cập tài khoản Cố Đô GIS.";
            this.textInfo.Font = ThemeFonts.Regular(11);
            this.textInfo.ForeColor = ThemeColors.Sky900;
            this.textInfo.LineSpacing = 1.5f;
            this.textInfo.Size = new Size(368, 44);
            this.textInfo.Location = new Point(34, 8);
            this.boxInfo.Controls.Add(this.iconInfo);
            this.boxInfo.Controls.Add(this.textInfo);

            this.lblEmail.Text = "Email đã đăng ký:";
            this.lblEmail.Font = ThemeFonts.Medium(12);
            this.lblEmail.ForeColor = ThemeColors.Slate700;
            this.lblEmail.AutoSize = true;
            this.lblEmail.Location = new Point(16, 122);
            this.txtEmail.IconName = "mail";
            this.txtEmail.Text = "dukhach@huegis.vn";
            this.txtEmail.Size = new Size(408, 32);
            this.txtEmail.Location = new Point(16, 144);

            this.lblOtp.Text = "Mã xác thực OTP (6 chữ số):";
            this.lblOtp.Font = ThemeFonts.Medium(12);
            this.lblOtp.ForeColor = ThemeColors.Slate700;
            this.lblOtp.AutoSize = true;
            this.lblOtp.Location = new Point(16, 186);
            this.txtOtp.IconName = "pin";
            this.txtOtp.MonospaceText = true;
            this.txtOtp.Text = "839201";
            this.txtOtp.Size = new Size(286, 32);
            this.txtOtp.Location = new Point(16, 208);

            this.btnResend.AutoWidth = false;
            this.btnResend.Text = "Gửi lại mã (59s)";
            this.btnResend.IconName = "refresh";
            this.btnResend.IconSize = 13;
            this.btnResend.Font = ThemeFonts.SemiBold(11);
            this.btnResend.ForeColor = ThemeColors.Primary;
            this.btnResend.IconColor = ThemeColors.Primary;
            this.btnResend.FillColor = ThemeColors.Sky50;
            this.btnResend.HoverFillColor = ThemeColors.Sky100;
            this.btnResend.BorderColor = ThemeColors.Sky200;
            this.btnResend.Radius = 6;
            this.btnResend.Gap = 4;
            this.btnResend.Size = new Size(116, 32);
            this.btnResend.Location = new Point(308, 208);

            this.lblNewPassword.Text = "Mật khẩu mới:";
            this.lblNewPassword.Font = ThemeFonts.Medium(12);
            this.lblNewPassword.ForeColor = ThemeColors.Slate700;
            this.lblNewPassword.AutoSize = true;
            this.lblNewPassword.Location = new Point(16, 250);
            this.txtNewPassword.IconName = "lock";
            this.txtNewPassword.IsPassword = true;
            this.txtNewPassword.Revealable = true;
            this.txtNewPassword.Text = "HueNewPass2025";
            this.txtNewPassword.Size = new Size(408, 32);
            this.txtNewPassword.Location = new Point(16, 272);

            this.lblConfirmPassword.Text = "Xác nhận mật khẩu mới:";
            this.lblConfirmPassword.Font = ThemeFonts.Medium(12);
            this.lblConfirmPassword.ForeColor = ThemeColors.Slate700;
            this.lblConfirmPassword.AutoSize = true;
            this.lblConfirmPassword.Location = new Point(16, 314);
            this.txtConfirmPassword.IconName = "lock_reset";
            this.txtConfirmPassword.IsPassword = true;
            this.txtConfirmPassword.Revealable = true;
            this.txtConfirmPassword.Text = "HueNewPass2025";
            this.txtConfirmPassword.Size = new Size(408, 32);
            this.txtConfirmPassword.Location = new Point(16, 336);

            this.btnConfirm.AutoWidth = false;
            this.btnConfirm.Text = "Xác Nhận Đổi Mật Khẩu";
            this.btnConfirm.IconName = "check_circle";
            this.btnConfirm.IconSize = 16;
            this.btnConfirm.Font = ThemeFonts.SemiBold(12);
            this.btnConfirm.ForeColor = Color.White;
            this.btnConfirm.IconColor = Color.White;
            this.btnConfirm.FillColor = ThemeColors.Primary;
            this.btnConfirm.HoverFillColor = ThemeColors.PrimaryContainer;
            this.btnConfirm.Radius = 6;
            this.btnConfirm.Gap = 6;
            this.btnConfirm.Size = new Size(408, 32);
            this.btnConfirm.Location = new Point(16, 384);

            this.btnBack.AutoWidth = false;
            this.btnBack.Text = "Quay lại Đăng nhập";
            this.btnBack.IconName = "arrow_back";
            this.btnBack.IconSize = 14;
            this.btnBack.Font = ThemeFonts.Medium(12);
            this.btnBack.ForeColor = ThemeColors.Slate600;
            this.btnBack.IconColor = ThemeColors.Slate600;
            this.btnBack.HoverIconColor = ThemeColors.Primary;
            this.btnBack.HoverTextColor = ThemeColors.Primary;
            this.btnBack.FillColor = Color.Transparent;
            this.btnBack.HoverFillColor = Color.Transparent;
            this.btnBack.Gap = 4;
            this.btnBack.Size = new Size(408, 26);
            this.btnBack.Location = new Point(16, 424);
            this.btnBack.Click += new System.EventHandler(this.btnBack_Click);

            // Footer: border-t slate-100, support hint + hotline.
            this.dividerFooter.BackColor = ThemeColors.Slate100;
            this.dividerFooter.Size = new Size(408, 1);
            this.dividerFooter.Location = new Point(16, 462);

            this.captionHelp.AutoWidth = false;
            this.captionHelp.Text = "Cần trợ giúp trực tiếp?";
            this.captionHelp.IconName = "support_agent";
            this.captionHelp.IconSize = 14;
            this.captionHelp.Font = ThemeFonts.Regular(11);
            this.captionHelp.ForeColor = ThemeColors.Slate500;
            this.captionHelp.IconColor = ThemeColors.Amber600;
            this.captionHelp.FillColor = Color.Transparent;
            this.captionHelp.HoverFillColor = Color.Transparent;
            this.captionHelp.HorizontalPadding = 0;
            this.captionHelp.Gap = 4;
            this.captionHelp.Cursor = Cursors.Default;
            this.captionHelp.Size = new Size(200, 24);
            this.captionHelp.Location = new Point(16, 472);

            this.lblHotline.Text = "0234.3828288";
            this.lblHotline.Font = ThemeFonts.Bold(11);
            this.lblHotline.ForeColor = ThemeColors.Amber700;
            this.lblHotline.AutoSize = false;
            this.lblHotline.TextAlign = ContentAlignment.MiddleRight;
            this.lblHotline.Size = new Size(120, 24);
            this.lblHotline.Location = new Point(304, 472);

            this.Controls.Add(this.lblHotline);
            this.Controls.Add(this.captionHelp);
            this.Controls.Add(this.dividerFooter);
            this.Controls.Add(this.btnBack);
            this.Controls.Add(this.btnConfirm);
            this.Controls.Add(this.txtConfirmPassword);
            this.Controls.Add(this.lblConfirmPassword);
            this.Controls.Add(this.txtNewPassword);
            this.Controls.Add(this.lblNewPassword);
            this.Controls.Add(this.btnResend);
            this.Controls.Add(this.txtOtp);
            this.Controls.Add(this.lblOtp);
            this.Controls.Add(this.txtEmail);
            this.Controls.Add(this.lblEmail);
            this.Controls.Add(this.boxInfo);
            this.Controls.Add(this.header);
            this.Name = "ForgotPasswordPanel";
            this.boxInfo.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
