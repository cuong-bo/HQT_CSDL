using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Hue_travel.Theme
{
    /// <summary>
    /// The mockup's text input: h-8 slate-50 field with a leading icon, optional password
    /// reveal eye, slate-200 border that turns primary while focused.
    /// </summary>
    public class InputField : Control
    {
        private readonly TextBox textBox = new TextBox();
        private readonly IconButton leadingIcon = new IconButton();
        private readonly IconButton revealButton = new IconButton();
        private bool isRevealable;
        private bool isFocused;

        public InputField()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Size = new Size(240, 32);
            Font = ThemeFonts.Regular(12);

            textBox.BorderStyle = BorderStyle.None;
            textBox.Multiline = true;
            textBox.WordWrap = false;
            textBox.Height = 20;
            textBox.BackColor = ThemeColors.Slate50;
            textBox.ForeColor = ThemeColors.Slate800;
            textBox.Font = Font;
            textBox.GotFocus += (s, e) => { isFocused = true; Invalidate(); };
            textBox.LostFocus += (s, e) => { isFocused = false; Invalidate(); };
            textBox.TextChanged += (s, e) => OnTextChanged(EventArgs.Empty);

            leadingIcon.AutoWidth = false;
            leadingIcon.IconSize = 15;
            leadingIcon.IconColor = ThemeColors.Slate400;
            leadingIcon.FillColor = Color.Transparent;
            leadingIcon.HoverFillColor = Color.Transparent;
            leadingIcon.Cursor = Cursors.IBeam;
            leadingIcon.Size = new Size(16, 16);
            leadingIcon.Click += (s, e) => textBox.Focus();

            revealButton.AutoWidth = false;
            revealButton.IconName = "visibility";
            revealButton.IconSize = 15;
            revealButton.IconColor = ThemeColors.Slate400;
            revealButton.HoverIconColor = ThemeColors.Slate600;
            revealButton.FillColor = Color.Transparent;
            revealButton.HoverFillColor = Color.Transparent;
            revealButton.Size = new Size(16, 16);
            revealButton.Visible = false;
            revealButton.Click += (s, e) =>
            {
                textBox.UseSystemPasswordChar = !textBox.UseSystemPasswordChar;
                revealButton.IconName = textBox.UseSystemPasswordChar ? "visibility" : "visibility_off";
            };

            Controls.Add(textBox);
            Controls.Add(leadingIcon);
            Controls.Add(revealButton);
            LayoutChildren();
        }

        [Category("Appearance")]
        public string IconName { get { return leadingIcon.IconName; } set { leadingIcon.IconName = value; LayoutChildren(); } }

        [Category("Behavior")]
        public bool IsPassword
        {
            get { return textBox.UseSystemPasswordChar; }
            set
            {
                // Windows ignores password masking on multiline edit controls, so masked fields are single-line.
                textBox.UseSystemPasswordChar = value;
                textBox.Multiline = !value;
                if (!value) textBox.Height = 20;
                LayoutChildren();
            }
        }

        /// <summary>Shows the eye button that toggles password visibility.</summary>
        [Category("Behavior")]
        public bool Revealable { get { return isRevealable; } set { isRevealable = value; revealButton.Visible = value; LayoutChildren(); } }

        [Category("Appearance")]
        public bool MonospaceText { get { return textBox.Font.FontFamily.Name == "Consolas"; } set { textBox.Font = value ? ThemeFonts.Mono(12) : Font; } }

        [Browsable(true), EditorBrowsable(EditorBrowsableState.Always)]
        public override string Text { get { return textBox.Text; } set { textBox.Text = value; } }

        protected override void OnResize(EventArgs e) { base.OnResize(e); LayoutChildren(); }

        private void LayoutChildren()
        {
            bool hasIcon = !string.IsNullOrEmpty(leadingIcon.IconName);
            int left = hasIcon ? 32 : 10;
            int right = isRevealable ? 32 : 10;
            leadingIcon.Visible = hasIcon;
            leadingIcon.Location = new Point(10, (Height - leadingIcon.Height) / 2);
            revealButton.Location = new Point(Width - 10 - revealButton.Width, (Height - revealButton.Height) / 2);
            textBox.Location = new Point(left, (Height - textBox.Height) / 2);
            textBox.Width = Math.Max(10, Width - left - right);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = Ui.RoundedRect(new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f), 6))
            using (var fill = new SolidBrush(ThemeColors.Slate50))
            using (var pen = new Pen(isFocused ? ThemeColors.Primary : ThemeColors.Slate200, 1f))
            {
                g.FillPath(fill, path);
                g.DrawPath(pen, path);
            }
        }
    }
}
