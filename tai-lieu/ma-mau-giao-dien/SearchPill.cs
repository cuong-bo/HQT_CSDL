using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Hue_travel.Theme;

namespace Hue_travel.Shell
{
    /// <summary>
    /// The search box that floats at the top left of the map (it used to live in the title bar). A pill: search icon, the text box
    /// with a hint label, and a clear button. Same events and members as the search part of the old TitleBar, so MainForm only has
    /// to change the name it subscribes to. Put it in MapHost and give it <see cref="OverlayCard.Attach"/> for the shadow.
    /// </summary>
    public sealed class SearchPill : Control
    {
        private readonly IconButton iconSearch = new IconButton();
        private readonly TextBox txtSearch = new TextBox();
        private readonly Label lblHint = new Label();
        private readonly IconButton btnClear = new IconButton();

        /// <summary>Raised as the text changes (also when it is cleared).</summary>
        public event EventHandler<string> SearchTextChanged;

        /// <summary>Raised when Enter is pressed.</summary>
        public event EventHandler<string> SearchSubmitted;

        /// <summary>Raised for the up (-1) and down (+1) keys, to move through the results shown under the box.</summary>
        public event EventHandler<int> SearchMoved;

        /// <summary>Raised when the caret leaves the box (the results under it are then dismissed).</summary>
        public event EventHandler SearchFocusLost;

        /// <summary>Raised when the caret comes back into a box that already has text (the results show again).</summary>
        public event EventHandler SearchFocusGained;

        public SearchPill()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Color.White;
            Size = new Size(Ui.S(420), Ui.S(48));

            iconSearch.AutoWidth = false;
            iconSearch.IconName = "search";
            iconSearch.IconSize = 20;
            iconSearch.IconColor = ThemeColors.Brand;
            iconSearch.FillColor = Color.Transparent;
            iconSearch.HoverFillColor = Color.Transparent;
            iconSearch.Cursor = Cursors.IBeam;
            iconSearch.Click += (s, e) => txtSearch.Focus();

            txtSearch.BorderStyle = BorderStyle.None;
            txtSearch.BackColor = Color.White;
            txtSearch.ForeColor = ThemeColors.Ink;
            txtSearch.Font = ThemeFonts.Regular(14);
            txtSearch.TextChanged += (s, e) =>
            {
                btnClear.Visible = txtSearch.TextLength > 0;
                lblHint.Visible = txtSearch.TextLength == 0;
                SearchTextChanged?.Invoke(this, txtSearch.Text);
            };
            txtSearch.Enter += (s, e) => FocusChanged();
            txtSearch.Leave += (s, e) => FocusChanged();
            txtSearch.KeyDown += TxtKeyDown;

            // The hint is a label, not the TextBox's own cue banner: the banner is drawn without font fallback and loses Vietnamese marks.
            lblHint.Text = "Tìm điểm đến ở Huế: di tích, quán ăn, khách sạn…";
            lblHint.Font = ThemeFonts.Regular(14);
            lblHint.ForeColor = ThemeColors.Slate400;
            lblHint.BackColor = Color.Transparent;
            lblHint.UseMnemonic = false;
            lblHint.AutoSize = false;
            lblHint.TextAlign = ContentAlignment.MiddleLeft;
            lblHint.Cursor = Cursors.IBeam;
            lblHint.Click += (s, e) => txtSearch.Focus();

            btnClear.AutoWidth = false;
            btnClear.IconName = "close";
            btnClear.IconSize = 14;
            btnClear.IconColor = ThemeColors.Slate500;
            btnClear.FillColor = Color.Transparent;
            btnClear.HoverFillColor = ThemeColors.Slate100;
            btnClear.Radius = ThemeMetrics.RadiusFull;
            btnClear.Visible = false;
            btnClear.Click += (s, e) => { txtSearch.Clear(); txtSearch.Focus(); };

            Controls.Add(lblHint);
            Controls.Add(iconSearch);
            Controls.Add(txtSearch);
            Controls.Add(btnClear);
            LayoutChildren();
        }

        /// <summary>The text in the search box.</summary>
        public string SearchText => txtSearch.Text;

        /// <summary>Empties the box without raising a search for the empty text.</summary>
        public void ClearSearch() => txtSearch.Clear();

        private void LayoutChildren()
        {
            int pad = Ui.S(16), icon = Ui.S(20), clear = Ui.S(26);
            iconSearch.SetBounds(pad, (Height - icon) / 2, icon, icon);
            btnClear.SetBounds(Width - pad - clear + Ui.S(4), (Height - clear) / 2, clear, clear);
            int textLeft = iconSearch.Right + Ui.S(10), textRight = btnClear.Left - Ui.S(6);
            txtSearch.AutoSize = false;
            int textHeight = txtSearch.Font.Height + Ui.S(4);   // tall enough for the whole line, or the tails of g, y, p are cut off
            txtSearch.SetBounds(textLeft, (Height - textHeight) / 2, Math.Max(Ui.S(40), textRight - textLeft), textHeight);
            lblHint.Bounds = txtSearch.Bounds;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutChildren();
        }

        private void TxtKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; SearchSubmitted?.Invoke(this, txtSearch.Text); }
            else if (e.KeyCode == Keys.Down || e.KeyCode == Keys.Up) { e.SuppressKeyPress = true; SearchMoved?.Invoke(this, e.KeyCode == Keys.Down ? 1 : -1); }
            else if (e.KeyCode == Keys.Escape) { e.SuppressKeyPress = true; txtSearch.Clear(); }
        }

        // Web-style focus: the pill gets a purple ring while it has the caret.
        private void FocusChanged()
        {
            bool focused = txtSearch.Focused;
            if (focused && txtSearch.TextLength > 0) SearchFocusGained?.Invoke(this, EventArgs.Empty);
            else if (!focused) SearchFocusLost?.Invoke(this, EventArgs.Empty);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);
            if (!txtSearch.Focused) return;
            float ring = Math.Max(2f, Ui.S(2f));
            using (GraphicsPath path = Ui.RoundedRect(new RectangleF(ring / 2, ring / 2, Width - ring, Height - ring), Height / 2f))
            using (var pen = new Pen(ThemeColors.Brand, ring))
            {
                g.DrawPath(pen, path);
            }
        }
    }
}
