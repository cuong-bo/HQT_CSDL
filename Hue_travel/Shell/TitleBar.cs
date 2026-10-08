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
        private const int WindowButtonWidth = 48; // enlarged for readability

        public TitleBar()
        {
            InitializeComponent();
            LayoutRightGroup();
        }

        /// <summary>
        /// Positions the right-hand group (auth button, window buttons)
        /// from the measured widths so the spacing matches the mockup's flex gaps. The
        /// controls are right-anchored, so this only needs to run once.
        /// </summary>
        private void LayoutRightGroup()
        {
            int right = Width - EdgePadding;
            btnClose.Left = right - WindowButtonWidth;
            btnMaximize.Left = btnClose.Left - WindowButtonWidth;
            btnMinimize.Left = btnMaximize.Left - WindowButtonWidth;

            btnAuth.Left = btnMinimize.Left - 10 - btnAuth.Width;
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
