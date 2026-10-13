using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Hue_travel.Theme;

namespace Hue_travel.Welcome
{
    /// <summary>
    /// A text field of the sign-in screen: white rounded box with a leading icon, a hint that stays until something is
    /// typed, an optional eye button for passwords, and a purple ring while it has the caret (red after a failed check).
    /// All sizes are in device pixels already scaled by the caller (<see cref="Ui.S(int)"/>).
    /// </summary>
    public sealed class AuthField : Control
    {
        private readonly TextBox textBox = new TextBox();
        private readonly Label hintLabel = new Label();
        private readonly IconButton revealButton = new IconButton();
        private string iconName = "";
        private bool isPassword;
        private bool isFocused;
        private bool hasError;

        /// <summary>Raised when Enter is pressed inside the field.</summary>
        public event EventHandler EnterPressed;

        public AuthField()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.ContainerControl, true);
            BackColor = Color.Transparent;
            TabStop = false;   // the text box inside is the stop, not this frame
            Font = ThemeFonts.Regular(15);

            textBox.BorderStyle = BorderStyle.None;
            textBox.AutoSize = false;
            textBox.BackColor = Color.White;
            textBox.ForeColor = ThemeColors.Slate800;
            textBox.Font = Font;
            textBox.GotFocus += (s, e) => { isFocused = true; Invalidate(); };
            textBox.LostFocus += (s, e) => { isFocused = false; Invalidate(); };
            textBox.TextChanged += (s, e) =>
            {
                hasError = false;
                hintLabel.Visible = textBox.TextLength == 0;
                Invalidate();
                OnTextChanged(EventArgs.Empty);
            };
            textBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode != Keys.Enter) return;
                e.SuppressKeyPress = true;
                EnterPressed?.Invoke(this, EventArgs.Empty);
            };

            // The hint is a label, not the TextBox's cue banner: the banner loses Vietnamese marks (no font fallback).
            hintLabel.AutoSize = false;
            hintLabel.UseMnemonic = false;
            hintLabel.BackColor = Color.White;
            hintLabel.ForeColor = ThemeColors.Slate400;
            hintLabel.Font = Font;
            hintLabel.TextAlign = ContentAlignment.MiddleLeft;
            hintLabel.Cursor = Cursors.IBeam;
            hintLabel.Click += (s, e) => textBox.Focus();

            revealButton.AutoWidth = false;
            revealButton.IconName = "visibility";
            revealButton.IconSize = 20;
            revealButton.IconColor = ThemeColors.Slate500;
            revealButton.HoverIconColor = ThemeColors.BrandPurple;
            revealButton.FillColor = Color.Transparent;
            revealButton.HoverFillColor = ThemeColors.BrandPurpleLight;
            revealButton.Radius = 14;
            revealButton.Visible = false;
            revealButton.TabStop = false;   // Tab goes from field to field, not onto the eye
            revealButton.Click += (s, e) =>
            {
                textBox.UseSystemPasswordChar = !textBox.UseSystemPasswordChar;
                revealButton.IconName = textBox.UseSystemPasswordChar ? "visibility" : "visibility_off";
                textBox.Focus();
            };

            Controls.Add(hintLabel);
            Controls.Add(textBox);
            Controls.Add(revealButton);
        }

        public string IconName { get { return iconName; } set { iconName = value ?? ""; LayoutChildren(); Invalidate(); } }

        public string Hint { get { return hintLabel.Text; } set { hintLabel.Text = value ?? ""; } }

        public bool IsPassword
        {
            get { return isPassword; }
            set
            {
                isPassword = value;
                textBox.UseSystemPasswordChar = value;
                revealButton.Visible = value;
                LayoutChildren();
            }
        }

        public override string Text { get { return textBox.Text; } set { textBox.Text = value ?? string.Empty; } }

        /// <summary>Draws the border in red until the text is edited again.</summary>
        public bool HasError { get { return hasError; } set { hasError = value; Invalidate(); } }

        public new bool Enabled
        {
            get { return base.Enabled; }
            set { base.Enabled = value; textBox.Enabled = value; Invalidate(); }
        }

        public void FocusInput() => textBox.Focus();

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            textBox.Font = Font;
            hintLabel.Font = Font;
            LayoutChildren();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutChildren();
        }

        private void LayoutChildren()
        {
            if (textBox == null || Width <= 0) return;
            int left = Ui.S(iconName.Length > 0 ? 52 : 18);
            int right = Ui.S(isPassword ? 54 : 18);
            int textHeight = textBox.Font.Height + Ui.S(8);   // tall enough for the tails of g, y, p
            textBox.SetBounds(left, (Height - textHeight) / 2, Math.Max(Ui.S(40), Width - left - right), textHeight);
            hintLabel.SetBounds(textBox.Left, textBox.Top, textBox.Width, textBox.Height);
            int eye = Ui.S(28);
            revealButton.SetBounds(Width - Ui.S(14) - eye, (Height - eye) / 2, eye, eye);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var bounds = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            Color border = hasError ? ThemeColors.Red500 : isFocused ? ThemeColors.BrandPurple : ThemeColors.Slate300;
            using (GraphicsPath path = Ui.RoundedRect(bounds, Ui.S(12)))
            using (var fill = new SolidBrush(Enabled ? Color.White : ThemeColors.Slate100))
            using (var pen = new Pen(border, isFocused || hasError ? 1.6f : 1f))
            {
                g.FillPath(fill, path);
                g.DrawPath(pen, path);
            }
            if (iconName.Length > 0)
            {
                int px = Ui.S(22);
                g.DrawImage(IconFont.Get(iconName, px, isFocused ? ThemeColors.BrandPurple : ThemeColors.Slate500), Ui.S(16), (Height - px) / 2, px, px);
            }
        }
    }
}
