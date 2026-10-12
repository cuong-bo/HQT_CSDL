using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Hue_travel.Shell
{
    public partial class TitleBar : UserControl
    {
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        public event EventHandler AuthClicked;
        public event EventHandler MinimizeClicked;
        public event EventHandler MaximizeClicked;
        public event EventHandler CloseClicked;

        private const int EdgePadding = 0;       // window buttons touch the edge

        public TitleBar()
        {
            InitializeComponent();
            LayoutRightGroup();
            Resize += (s, e) => LayoutRightGroup(); // widths change once the window is scaled to the screen DPI

            // Double-clicking the bar maximises / restores, like a normal window caption.
            EventHandler toggleMaximize = (s, e) => MaximizeClicked?.Invoke(this, EventArgs.Empty);
            panelRoot.DoubleClick += toggleMaximize;
            lblLogo.DoubleClick += toggleMaximize;
            picLogo.DoubleClick += toggleMaximize;
        }

        /// <summary>The text of the account button: the signed-in name, or the invitation to sign in when null.</summary>
        public void SetUser(string text)
        {
            string shown = string.IsNullOrWhiteSpace(text) ? "Đăng nhập / Đăng ký" : (text.Length > 22 ? text.Substring(0, 21) + "…" : text);
            btnAuth.AutoWidth = true;
            btnAuth.Text = shown;
            LayoutRightGroup();
        }

        /// <summary>Shows the "restore" icon while the window is maximised, "maximise" otherwise.</summary>
        public void SetMaximized(bool isMaximized)
        {
            btnMaximize.IconName = isMaximized ? "filter_none" : "crop_square";
        }

        /// <summary>
        /// Positions the right-hand group (auth button, window buttons)
        /// from the measured widths so the spacing matches the mockup's flex gaps. The
        /// controls are right-anchored, so this only needs to run once.
        /// </summary>
        private void LayoutRightGroup()
        {
            int right = Width - EdgePadding;
            btnClose.Left = right - btnClose.Width;
            btnMaximize.Left = btnClose.Left - btnMaximize.Width;
            btnMinimize.Left = btnMaximize.Left - btnMinimize.Width;

            btnAuth.Left = btnMinimize.Left - Hue_travel.Theme.Ui.S(10) - btnAuth.Width;
        }

        /// <summary>Lets the user drag the borderless MainForm by its title bar (FormBorderStyle.None has no built-in move handle).</summary>
        private void panelRoot_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            var form = FindForm();
            if (form == null) return;

            ReleaseCapture();
            SendMessage(form.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
        }

        private void btnAuth_Click(object sender, EventArgs e) => AuthClicked?.Invoke(this, EventArgs.Empty);

        private void btnMinimize_Click(object sender, EventArgs e) => MinimizeClicked?.Invoke(this, EventArgs.Empty);

        private void btnMaximize_Click(object sender, EventArgs e) => MaximizeClicked?.Invoke(this, EventArgs.Empty);

        private void btnClose_Click(object sender, EventArgs e) => CloseClicked?.Invoke(this, EventArgs.Empty);
    }
}
