using System.Drawing;
using System.Windows.Forms;
using Hue_travel.Theme;

namespace Hue_travel.Auth
{
    partial class AuthModal
    {
        private System.ComponentModel.IContainer components = null;
        private PanelHeader header;
        private AuthTabStrip tabs;
        private Label lblLoginId;
        private InputField txtLoginId;
        private Label lblLoginPassword;
        private InputField txtLoginPassword;
        private CheckBox chkRemember;
        private IconButton btnLogin;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        // Every property is assigned inline (no helper calls) so the WinForms Designer can render it.
        private void InitializeComponent()
        {
            this.header = new PanelHeader();
            this.tabs = new AuthTabStrip();
            this.lblLoginId = new Label();
            this.txtLoginId = new InputField();
            this.lblLoginPassword = new Label();
            this.txtLoginPassword = new InputField();
            this.chkRemember = new CheckBox();
            this.btnLogin = new IconButton();
            this.SuspendLayout();

            // w-[420px] card.
            this.BackColor = ThemeColors.SurfaceContainerLowest;
            this.Size = new Size(420, 316);

            this.header.IconName = "lock";
            this.header.Text = "Form Đăng Nhập & Đăng Ký Tài Khoản";
            this.header.Prominent = true;
            this.header.ShowMinimize = false;
            this.header.CloseClicked += new System.EventHandler(this.header_CloseClicked);

            this.tabs.ActiveTab = 0;
            this.tabs.Size = new Size(388, 30);
            this.tabs.Location = new Point(16, 48);
            this.tabs.TabSelected += new System.EventHandler<int>(this.tabs_TabSelected);

            this.lblLoginId.Text = "Email hoặc Số điện thoại:";
            this.lblLoginId.Font = ThemeFonts.Medium(12);
            this.lblLoginId.ForeColor = ThemeColors.Slate700;
            this.lblLoginId.AutoSize = true;
            this.lblLoginId.Location = new Point(16, 96);

            this.txtLoginId.IconName = "mail";
            this.txtLoginId.Text = "dukhach@huegis.vn";
            this.txtLoginId.Size = new Size(388, 32);
            this.txtLoginId.Location = new Point(16, 118);

            this.lblLoginPassword.Text = "Mật khẩu:";
            this.lblLoginPassword.Font = ThemeFonts.Medium(12);
            this.lblLoginPassword.ForeColor = ThemeColors.Slate700;
            this.lblLoginPassword.AutoSize = true;
            this.lblLoginPassword.Location = new Point(16, 162);


            this.txtLoginPassword.IconName = "lock";
            this.txtLoginPassword.IsPassword = true;
            this.txtLoginPassword.Text = "Huetravel2025";
            this.txtLoginPassword.Size = new Size(388, 32);
            this.txtLoginPassword.Location = new Point(16, 184);

            this.chkRemember.Text = "Ghi nhớ phiên đăng nhập";
            this.chkRemember.Checked = true;
            this.chkRemember.Font = ThemeFonts.Regular(11);
            this.chkRemember.ForeColor = ThemeColors.Slate600;
            this.chkRemember.AutoSize = true;
            this.chkRemember.Location = new Point(16, 228);

            this.btnLogin.AutoWidth = false;
            this.btnLogin.Text = "Đăng Nhập Ngay";
            this.btnLogin.IconName = "login";
            this.btnLogin.IconSize = 16;
            this.btnLogin.Font = ThemeFonts.SemiBold(12);
            this.btnLogin.ForeColor = Color.White;
            this.btnLogin.IconColor = Color.White;
            this.btnLogin.FillColor = ThemeColors.Primary;
            this.btnLogin.HoverFillColor = ThemeColors.PrimaryContainer;
            this.btnLogin.Radius = 6;
            this.btnLogin.Gap = 6;
            this.btnLogin.Size = new Size(388, 32);
            this.btnLogin.Location = new Point(16, 258);



            this.Controls.Add(this.btnLogin);
            this.Controls.Add(this.chkRemember);
            this.Controls.Add(this.txtLoginPassword);
            this.Controls.Add(this.lblLoginPassword);
            this.Controls.Add(this.txtLoginId);
            this.Controls.Add(this.lblLoginId);
            this.Controls.Add(this.tabs);
            this.Controls.Add(this.header);
            this.Name = "AuthModal";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
