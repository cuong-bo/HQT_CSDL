using System.Drawing;
using System.Windows.Forms;
using Hue_travel.Theme;

namespace Hue_travel.Auth
{
    partial class RegisterPanel
    {
        private System.ComponentModel.IContainer components = null;
        private PanelHeader header;
        private AuthTabStrip tabs;
        private Label lblFullName;
        private InputField txtFullName;
        private Label lblEmail;
        private InputField txtEmail;
        private Label lblPassword;
        private InputField txtPassword;
        private RoundedPanel boxTip;
        private IconButton iconTip;
        private RichTextBlock textTip;
        private IconButton btnRegister;

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
            this.lblFullName = new Label();
            this.txtFullName = new InputField();
            this.lblEmail = new Label();
            this.txtEmail = new InputField();
            this.lblPassword = new Label();
            this.txtPassword = new InputField();
            this.boxTip = new RoundedPanel();
            this.iconTip = new IconButton();
            this.textTip = new RichTextBlock();
            this.btnRegister = new IconButton();
            this.SuspendLayout();
            this.boxTip.SuspendLayout();

            // w-[420px] card, register tab active.
            this.BackColor = ThemeColors.SurfaceContainerLowest;
            this.Size = new Size(420, 396);

            this.header.IconName = "lock";
            this.header.Text = "Form Đăng Nhập & Đăng Ký Tài Khoản";
            this.header.Prominent = true;
            this.header.ShowMinimize = false;
            this.header.CloseClicked += new System.EventHandler(this.header_CloseClicked);

            this.tabs.ActiveTab = 1;
            this.tabs.Size = new Size(388, 30);
            this.tabs.Location = new Point(16, 48);
            this.tabs.TabSelected += new System.EventHandler<int>(this.tabs_TabSelected);

            this.lblFullName.Text = "Họ và tên du khách:";
            this.lblFullName.Font = ThemeFonts.Medium(12);
            this.lblFullName.ForeColor = ThemeColors.Slate700;
            this.lblFullName.AutoSize = true;
            this.lblFullName.Location = new Point(16, 96);
            this.txtFullName.Text = "";
            this.txtFullName.Size = new Size(388, 32);
            this.txtFullName.Location = new Point(16, 118);

            this.lblEmail.Text = "Tên đăng nhập (chữ, số, _ hoặc .):";
            this.lblEmail.Font = ThemeFonts.Medium(12);
            this.lblEmail.ForeColor = ThemeColors.Slate700;
            this.lblEmail.AutoSize = true;
            this.lblEmail.Location = new Point(16, 158);
            this.txtEmail.Text = "";
            this.txtEmail.Size = new Size(388, 32);
            this.txtEmail.Location = new Point(16, 180);

            this.lblPassword.Text = "Mật khẩu mới (tối thiểu 8 ký tự):";
            this.lblPassword.Font = ThemeFonts.Medium(12);
            this.lblPassword.ForeColor = ThemeColors.Slate700;
            this.lblPassword.AutoSize = true;
            this.lblPassword.Location = new Point(16, 220);
            this.txtPassword.IsPassword = true;
            this.txtPassword.Text = "";
            this.txtPassword.Size = new Size(388, 32);
            this.txtPassword.Location = new Point(16, 242);

            // Benefit hint: sky-50 / sky-100.
            this.boxTip.FillColor = ThemeColors.Sky50;
            this.boxTip.BorderColor = ThemeColors.Sky100;
            this.boxTip.Radius = 4;
            this.boxTip.Size = new Size(388, 58);
            this.boxTip.Location = new Point(16, 284);
            this.iconTip.AutoWidth = false;
            this.iconTip.IconName = "bookmark_add";
            this.iconTip.IconSize = 14;
            this.iconTip.IconColor = ThemeColors.Primary;
            this.iconTip.FillColor = Color.Transparent;
            this.iconTip.HoverFillColor = Color.Transparent;
            this.iconTip.Cursor = Cursors.Default;
            this.iconTip.Size = new Size(14, 14);
            this.iconTip.Location = new Point(8, 8);
            this.textTip.Text = "Đăng ký giúp bạn lưu lại các điểm đến yêu thích và lộ trình cá nhân hóa đồng bộ trên thiết bị.";
            this.textTip.Font = ThemeFonts.Regular(11);
            this.textTip.ForeColor = ThemeColors.Sky800;
            this.textTip.LineSpacing = 1.4f;
            this.textTip.Size = new Size(350, 46);
            this.textTip.Location = new Point(28, 6);
            this.boxTip.Controls.Add(this.iconTip);
            this.boxTip.Controls.Add(this.textTip);

            this.btnRegister.AutoWidth = false;
            this.btnRegister.Text = "Tạo Tài Khoản Du Khách";
            this.btnRegister.IconName = "person_add";
            this.btnRegister.IconSize = 16;
            this.btnRegister.Font = ThemeFonts.SemiBold(12);
            this.btnRegister.ForeColor = Color.White;
            this.btnRegister.IconColor = Color.White;
            this.btnRegister.FillColor = ThemeColors.Primary;
            this.btnRegister.HoverFillColor = ThemeColors.PrimaryContainer;
            this.btnRegister.Radius = 6;
            this.btnRegister.Gap = 6;
            this.btnRegister.Size = new Size(388, 32);
            this.btnRegister.Location = new Point(16, 352);

            this.Controls.Add(this.btnRegister);
            this.Controls.Add(this.boxTip);
            this.Controls.Add(this.txtPassword);
            this.Controls.Add(this.lblPassword);
            this.Controls.Add(this.txtEmail);
            this.Controls.Add(this.lblEmail);
            this.Controls.Add(this.txtFullName);
            this.Controls.Add(this.lblFullName);
            this.Controls.Add(this.tabs);
            this.Controls.Add(this.header);
            this.Name = "RegisterPanel";
            this.boxTip.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
