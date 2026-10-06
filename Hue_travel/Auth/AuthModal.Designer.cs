using Guna.UI2.WinForms;
using Hue_travel.Theme;

namespace Hue_travel.Auth
{
    partial class AuthModal
    {
        private System.ComponentModel.IContainer components = null;
        private Guna2Panel panelBackdrop;
        private Guna2Panel panelCard;
        private Guna2Panel panelHeader;
        private System.Windows.Forms.Label lblTitle;
        private Guna2Button btnClose;
        private Guna2TextBox txtEmail;
        private Guna2TextBox txtPassword;
        private Guna2Button btnSubmit;
        private System.Windows.Forms.LinkLabel lnkForgotPassword;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.panelBackdrop = new Guna2Panel();
            this.panelCard = new Guna2Panel();
            this.panelHeader = new Guna2Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.btnClose = new Guna2Button();
            this.txtEmail = new Guna2TextBox();
            this.txtPassword = new Guna2TextBox();
            this.btnSubmit = new Guna2Button();
            this.lnkForgotPassword = new System.Windows.Forms.LinkLabel();
            this.SuspendLayout();

            this.panelBackdrop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelBackdrop.FillColor = System.Drawing.Color.FromArgb(15, 23, 42);
            this.panelBackdrop.Click += new System.EventHandler(this.panelBackdrop_Click);

            this.panelCard.Size = new System.Drawing.Size(420, 360);
            this.panelCard.FillColor = ThemeColors.SurfaceContainerLowest;
            this.panelCard.BorderRadius = ThemeMetrics.RadiusLg;
            this.panelCard.Anchor = System.Windows.Forms.AnchorStyles.None;

            this.panelHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelHeader.Height = 36;
            this.panelHeader.FillColor = ThemeColors.Primary;

            this.lblTitle.Text = "Đăng Nhập — Cố Đô GIS";
            this.lblTitle.Font = ThemeFonts.LabelMd;
            this.lblTitle.ForeColor = ThemeColors.OnPrimary;
            this.lblTitle.AutoSize = true;
            this.lblTitle.Location = new System.Drawing.Point(12, 10);

            this.btnClose.Text = "✕";
            this.btnClose.Size = new System.Drawing.Size(24, 24);
            this.btnClose.Location = new System.Drawing.Point(384, 6);
            this.btnClose.FillColor = ThemeColors.Primary;
            this.btnClose.ForeColor = ThemeColors.OnPrimary;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);

            this.panelHeader.Controls.Add(this.lblTitle);
            this.panelHeader.Controls.Add(this.btnClose);

            this.txtEmail.PlaceholderText = "Email đã đăng ký";
            this.txtEmail.Location = new System.Drawing.Point(24, 64);
            this.txtEmail.Size = new System.Drawing.Size(372, 32);

            this.txtPassword.PlaceholderText = "Mật khẩu";
            this.txtPassword.PasswordChar = '•';
            this.txtPassword.Location = new System.Drawing.Point(24, 108);
            this.txtPassword.Size = new System.Drawing.Size(372, 32);

            this.lnkForgotPassword.Text = "Quên mật khẩu?";
            this.lnkForgotPassword.AutoSize = true;
            this.lnkForgotPassword.Location = new System.Drawing.Point(24, 148);

            this.btnSubmit.Text = "Đăng Nhập";
            this.btnSubmit.Location = new System.Drawing.Point(24, 180);
            this.btnSubmit.Size = new System.Drawing.Size(372, 36);
            this.btnSubmit.FillColor = ThemeColors.Primary;
            this.btnSubmit.ForeColor = ThemeColors.OnPrimary;
            this.btnSubmit.Font = ThemeFonts.LabelMd;

            this.panelCard.Controls.Add(this.btnSubmit);
            this.panelCard.Controls.Add(this.lnkForgotPassword);
            this.panelCard.Controls.Add(this.txtPassword);
            this.panelCard.Controls.Add(this.txtEmail);
            this.panelCard.Controls.Add(this.panelHeader);

            this.panelBackdrop.Controls.Add(this.panelCard);
            this.Controls.Add(this.panelBackdrop);
            this.Name = "AuthModal";
            this.Size = new System.Drawing.Size(1000, 700);
            this.ResumeLayout(false);
        }
    }
}
