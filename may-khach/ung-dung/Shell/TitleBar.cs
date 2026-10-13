using System;
using System.Drawing;
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

        public event EventHandler MinimizeClicked;
        public event EventHandler MaximizeClicked;
        public event EventHandler CloseClicked;

        /// <summary>Raised as the text of the search box changes (also when it is cleared).</summary>
        public event EventHandler<string> SearchTextChanged;

        /// <summary>Raised when Enter is pressed in the search box.</summary>
        public event EventHandler<string> SearchSubmitted;


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

            LayoutSearch();
        }

        // The search box is centred in the bar and shrinks before it would touch the logo or the window buttons.
        private void LayoutSearch()
        {
            int gap = Hue_travel.Theme.Ui.S(24);
            int free = btnMinimize.Left - gap - (lblLogo.Right + gap);
            int width = System.Math.Max(Hue_travel.Theme.Ui.S(200), System.Math.Min(Hue_travel.Theme.Ui.S(560), free));
            int height = Hue_travel.Theme.Ui.S(38);
            int left = System.Math.Max(lblLogo.Right + gap, (Width - width) / 2);
            if (left + width > btnMinimize.Left - gap) left = btnMinimize.Left - gap - width;
            boxSearch.SetBounds(left, (Height - 1 - height) / 2, width, height);
            boxSearch.Radius = 19;

            int pad = Hue_travel.Theme.Ui.S(14);
            iconSearch.Location = new System.Drawing.Point(pad, (height - iconSearch.Height) / 2);
            int clearSize = Hue_travel.Theme.Ui.S(22);
            btnClearSearch.SetBounds(width - pad - clearSize + Hue_travel.Theme.Ui.S(4), (height - clearSize) / 2, clearSize, clearSize);
            int textLeft = iconSearch.Right + Hue_travel.Theme.Ui.S(10);
            int textRight = width - pad - clearSize - Hue_travel.Theme.Ui.S(6);
            // Tall enough for the whole line, or the tails of g, y, p are cut off.
            txtSearch.AutoSize = false;
            int textHeight = txtSearch.Font.Height + Hue_travel.Theme.Ui.S(4);
            txtSearch.SetBounds(textLeft, (height - textHeight) / 2, System.Math.Max(Hue_travel.Theme.Ui.S(40), textRight - textLeft), textHeight);
            lblSearchHint.Bounds = txtSearch.Bounds;
        }

        // Clicking the hint text puts the caret in the box, like clicking the box itself.
        private void lblSearchHint_Click(object sender, EventArgs e) => txtSearch.Focus();

        /// <summary>Empties the search box without raising a search for the empty text.</summary>
        public void ClearSearch() => txtSearch.Clear();

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            btnClearSearch.Visible = txtSearch.TextLength > 0;
            lblSearchHint.Visible = txtSearch.TextLength == 0;
            SearchTextChanged?.Invoke(this, txtSearch.Text);
        }

        /// <summary>Raised for the up (-1) and down (+1) keys, to move through the results shown under the box.</summary>
        public event EventHandler<int> SearchMoved;

        /// <summary>Raised when the caret leaves the box (the results under it are then dismissed).</summary>
        public event EventHandler SearchFocusLost;

        /// <summary>Raised when the caret comes back into a box that already has text (the results show again).</summary>
        public event EventHandler SearchFocusGained;

        /// <summary>Where the search box is, in the title bar's coordinates (the bar sits at the top-left of the window).</summary>
        public Rectangle SearchBoxBounds => boxSearch.Bounds;

        /// <summary>The text in the search box.</summary>
        public string SearchText => txtSearch.Text;

        private void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                SearchSubmitted?.Invoke(this, txtSearch.Text);
            }
            else if (e.KeyCode == Keys.Down || e.KeyCode == Keys.Up)
            {
                e.SuppressKeyPress = true;
                SearchMoved?.Invoke(this, e.KeyCode == Keys.Down ? 1 : -1);
            }
            else if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
                txtSearch.Clear();
            }
        }

        // Web-style focus: the pill turns white with a blue ring while it has the caret.
        private void txtSearch_FocusChanged(object sender, EventArgs e)
        {
            bool focused = txtSearch.Focused;
            if (focused && txtSearch.TextLength > 0) SearchFocusGained?.Invoke(this, EventArgs.Empty);
            else if (!focused) SearchFocusLost?.Invoke(this, EventArgs.Empty);
            Color fill = focused ? Hue_travel.Theme.ThemeColors.SurfaceContainerLowest : Hue_travel.Theme.ThemeColors.Slate100;
            boxSearch.FillColor = fill;
            boxSearch.BorderColor = focused ? Hue_travel.Theme.ThemeColors.Primary : Hue_travel.Theme.ThemeColors.Slate200;
            txtSearch.BackColor = fill;
            boxSearch.Invalidate();
        }

        private void btnClearSearch_Click(object sender, EventArgs e)
        {
            txtSearch.Clear();
            txtSearch.Focus();
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

        private void btnMinimize_Click(object sender, EventArgs e) => MinimizeClicked?.Invoke(this, EventArgs.Empty);

        private void btnMaximize_Click(object sender, EventArgs e) => MaximizeClicked?.Invoke(this, EventArgs.Empty);

        private void btnClose_Click(object sender, EventArgs e) => CloseClicked?.Invoke(this, EventArgs.Empty);
    }
}
