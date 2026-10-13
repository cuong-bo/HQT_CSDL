using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Hue_travel.Theme
{
    /// <summary>
    /// Owner-drawn button: optional leading icon, text, optional trailing icon, rounded
    /// fill, 1px border and a hover colour — the button vocabulary used throughout
    /// GiaoDien/code.html. Width follows the content when <see cref="AutoWidth"/> is set.
    /// </summary>
    public class IconButton : Control
    {
        private const TextFormatFlags MeasureFlags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;

        private string iconName = "";
        private string trailingIconName = "";
        private int iconSize = 16;
        private int trailingIconSize = 14;
        private int gap = 6;
        private int horizontalPadding = 10;
        private bool autoWidth = true;
        private const float HoverStep = 0.22f; // about 70 ms from rest to hover at 15 ms per tick
        private const float PressInStep = 0.5f;   // pressed look is complete after two ticks (about 30 ms)
        private const float PressOutStep = 0.12f; // and fades back in about 120 ms
        private const int MinPressedMs = 110;     // a quick tap still shows the pressed look long enough to be seen
        private const float RippleStep = 0.055f;  // the ripple spreads over roughly 270 ms
        private const float PressDarkening = 0.14f;

        private bool isHovered;
        private bool isPressed;
        private float hoverBlend;
        private float pressBlend;
        private int pressedAtTick;
        private float rippleProgress = -1f;       // -1: no ripple running
        private Point rippleOrigin;
        private readonly Timer hoverTimer = new Timer { Interval = 15 };

        public IconButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Font = ThemeFonts.SemiBold(12);
            ForeColor = ThemeColors.Slate700;
            FillColor = Color.White;
            HoverFillColor = ThemeColors.Slate100;
            IconColor = ThemeColors.Primary;
            TrailingIconColor = ThemeColors.Slate400;
            Radius = 6;
            Size = new Size(100, 28);
            hoverTimer.Tick += (s, e) => StepHover();
        }

        // One timer drives the hover fade, the press-in / press-out and the ripple; it stops when all three are at rest.
        private void StepHover()
        {
            float hoverTarget = isHovered ? 1f : 0f;
            hoverBlend = Approach(hoverBlend, hoverTarget, HoverStep);

            bool holdPressed = isPressed || unchecked(System.Environment.TickCount - pressedAtTick) < MinPressedMs;
            pressBlend = holdPressed ? Approach(pressBlend, 1f, PressInStep) : Approach(pressBlend, 0f, PressOutStep);

            if (rippleProgress >= 0f)
            {
                rippleProgress += RippleStep;
                if (rippleProgress >= 1f) rippleProgress = -1f;
            }

            if (hoverBlend == hoverTarget && pressBlend == (holdPressed ? 1f : 0f) && rippleProgress < 0f && !(holdPressed && !isPressed))
            {
                hoverTimer.Stop();
            }
            Invalidate();
        }

        private static float Approach(float value, float target, float step)
        {
            if (value < target) return System.Math.Min(target, value + step);
            return System.Math.Max(target, value - step);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) hoverTimer.Dispose();
            base.Dispose(disposing);
        }

        [Category("Appearance")] public Color FillColor { get; set; }
        [Category("Appearance")] public Color HoverFillColor { get; set; }
        [Category("Appearance")] public Color BorderColor { get; set; }
        [Category("Appearance")] public Color IconColor { get; set; }
        [Category("Appearance")] public Color TrailingIconColor { get; set; }
        [Category("Appearance")] public int Radius { get; set; }
        /// <summary>When set (and no icon is), draws an 8px status dot before the text.</summary>
        [Category("Appearance")] public Color DotColor { get; set; }
        /// <summary>Icon colour while hovered; Color.Empty keeps <see cref="IconColor"/>.</summary>
        [Category("Appearance")] public Color HoverIconColor { get; set; }
        /// <summary>Text colour while hovered; Color.Empty keeps <see cref="Control.ForeColor"/>.</summary>
        [Category("Appearance")] public Color HoverTextColor { get; set; }

        [Category("Appearance")]
        public string IconName { get { return iconName; } set { iconName = value ?? ""; Relayout(); } }

        [Category("Appearance")]
        public string TrailingIconName { get { return trailingIconName; } set { trailingIconName = value ?? ""; Relayout(); } }

        [Category("Appearance")]
        public int IconSize { get { return iconSize; } set { iconSize = value; Relayout(); } }

        [Category("Appearance")]
        public int TrailingIconSize { get { return trailingIconSize; } set { trailingIconSize = value; Relayout(); } }

        [Category("Layout")]
        public int Gap { get { return gap; } set { gap = value; Relayout(); } }

        [Category("Layout")]
        public int HorizontalPadding { get { return horizontalPadding; } set { horizontalPadding = value; Relayout(); } }

        [Category("Layout")]
        public bool AutoWidth { get { return autoWidth; } set { autoWidth = value; Relayout(); } }

        [Browsable(true), EditorBrowsable(EditorBrowsableState.Always)]
        public override string Text { get { return base.Text; } set { base.Text = value; Relayout(); } }

        public override Font Font { get { return base.Font; } set { base.Font = value; Relayout(); } }

        // Design-pixel settings converted to device pixels (the app is DPI aware).
        private int IconPx => Ui.S(iconSize);
        private int TrailingPx => Ui.S(trailingIconSize);
        private int PadPx => Ui.S(horizontalPadding);
        private int GapPx => Ui.S(gap);
        private int DotPx => Ui.S(DotSizeLogical);
        private int RadiusPx => Ui.S(Radius);

        public int ContentWidth
        {
            get
            {
                int width = PadPx * 2;
                bool hasText = !string.IsNullOrEmpty(Text);
                if (iconName.Length > 0) width += IconPx + (hasText ? GapPx : 0);
                else if (HasDot) width += DotPx + (hasText ? GapPx : 0);
                if (hasText) width += TextRenderer.MeasureText(Text, Font, Size.Empty, MeasureFlags).Width;
                if (trailingIconName.Length > 0) width += TrailingPx + (hasText || iconName.Length > 0 ? GapPx : 0);
                return width;
            }
        }

        /// <summary>Icon above a centred, word-wrapped caption (the category rail layout).</summary>
        [Category("Layout")] public bool Stacked { get; set; }

        private void PaintStacked(Graphics g)
        {
            Color icon = Ui.Lerp(IconColor, HoverIconColor.IsEmpty ? IconColor : HoverIconColor, hoverBlend);
            Color text = Ui.Lerp(ForeColor, HoverTextColor.IsEmpty ? ForeColor : HoverTextColor, hoverBlend);
            const TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.WordBreak | TextFormatFlags.HorizontalCenter | TextFormatFlags.Top;

            int textHeight = string.IsNullOrEmpty(Text) ? 0 : TextRenderer.MeasureText(Text, Font, new Size(Width, int.MaxValue), flags).Height;
            int blockHeight = IconPx + (textHeight > 0 ? GapPx : 0) + textHeight;
            int y = System.Math.Max(0, (Height - blockHeight) / 2);

            g.DrawImage(IconFont.Get(iconName, IconPx, icon), (Width - IconPx) / 2, y, IconPx, IconPx);
            if (textHeight > 0)
            {
                TextRenderer.DrawText(g, Text, Font, new Rectangle(0, y + IconPx + GapPx, Width, textHeight + 2), text, flags);
            }
        }

        private const int DotSizeLogical = 8;

        private bool HasDot => iconName.Length == 0 && !DotColor.IsEmpty && DotColor.A > 0;

        protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
        {
            // Content decides the width, whatever order the designer assigns Size/Text in.
            if (autoWidth && (specified & BoundsSpecified.Width) != 0 && !string.IsNullOrEmpty(Text + iconName))
            {
                width = ContentWidth;
            }
            base.SetBoundsCore(x, y, width, height, specified);
        }

        private void Relayout()
        {
            if (autoWidth) Width = ContentWidth;
            Invalidate();
        }

        protected override void OnMouseEnter(System.EventArgs e) { isHovered = true; hoverTimer.Start(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(System.EventArgs e) { isHovered = false; isPressed = false; hoverTimer.Start(); base.OnMouseLeave(e); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isPressed = true;
                pressedAtTick = System.Environment.TickCount;
                rippleOrigin = e.Location;
                rippleProgress = 0f;
                hoverTimer.Start();
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            isPressed = false;
            hoverTimer.Start();
            base.OnMouseUp(e);
        }

        /// <summary>
        /// True: the button sits 1px above the page with a soft edge shadow and sinks into it when pressed
        /// (used on the filled call-to-action buttons).
        /// </summary>
        [Category("Appearance")] public bool Elevated { get; set; }

        /// <summary>The fill while pressed: a darker shade of what the button shows at rest or hovered (a ghost button gets a visible tint).</summary>
        private Color PressedFill(Color shown)
        {
            Color basis = shown.A < 20 ? (HoverFillColor.A < 20 ? ThemeColors.Slate200 : HoverFillColor) : shown;
            Color dark = Ui.Lerp(basis, Color.Black, PressDarkening);
            return Color.FromArgb(basis.A, dark.R, dark.G, dark.B);
        }

        // Blend that keeps the target colour's channels when the start is fully transparent (a ghost button fading in).
        private static Color Mix(Color from, Color to, float t)
        {
            if (from.A == 0) from = Color.FromArgb(0, to.R, to.G, to.B);
            if (to.A == 0) to = Color.FromArgb(0, from.R, from.G, from.B);
            return Ui.Lerp(from, to, t);
        }

        private bool FillIsDark(Color fill) => fill.A > 128 && (fill.R * 299 + fill.G * 587 + fill.B * 114) / 1000 < 150;

        private void PaintRipple(Graphics g, GraphicsPath clip, Color fill)
        {
            if (rippleProgress < 0f) return;
            float ease = 1f - (1f - rippleProgress) * (1f - rippleProgress);
            float reach = (float)System.Math.Sqrt(Width * Width + Height * Height);
            float radius = reach * ease;
            int alpha = (int)(FillIsDark(fill) ? 95 : 55) * (int)(100 * (1f - rippleProgress)) / 100;
            Color tint = FillIsDark(fill) ? Color.White : ThemeColors.Sky700;
            var state = g.Save();
            g.SetClip(clip, CombineMode.Intersect);
            using (var brush = new SolidBrush(Color.FromArgb(System.Math.Max(0, alpha), tint)))
            {
                g.FillEllipse(brush, rippleOrigin.X - radius, rippleOrigin.Y - radius, radius * 2, radius * 2);
            }
            g.Restore(state);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // An elevated button leaves one pixel at the bottom for its edge shadow, which closes as it is pressed.
            int lift = Elevated ? 1 : 0;
            float shadowGap = lift * (1f - pressBlend);
            var bounds = new RectangleF(0.5f, 0.5f + (lift - shadowGap), Width - 1f, Height - 1f - lift);

            Color shown = Mix(FillColor, HoverFillColor, hoverBlend);
            Color fill = pressBlend > 0f ? Mix(shown, PressedFill(shown), pressBlend) : shown;

            if (Elevated && shadowGap > 0.01f && fill.A > 200)
            {
                using (var path = Ui.RoundedRect(new RectangleF(bounds.X, bounds.Y + shadowGap, bounds.Width, bounds.Height), RadiusPx))
                using (var brush = new SolidBrush(Color.FromArgb((int)(55 * shadowGap), 15, 23, 42)))
                {
                    g.FillPath(brush, path);
                }
            }
            using (var path = Ui.RoundedRect(bounds, RadiusPx))
            {
                if (fill.A > 0)
                {
                    using (var brush = new SolidBrush(fill)) g.FillPath(brush, path);
                }
                PaintRipple(g, path, fill);
                if (BorderColor.A > 0 && !BorderColor.IsEmpty)
                {
                    Color border = pressBlend > 0f ? Mix(BorderColor, Ui.Lerp(BorderColor, Color.Black, 0.18f), pressBlend) : BorderColor;
                    using (var pen = new Pen(border, 1f)) g.DrawPath(pen, path);
                }
            }

            // The content sinks one pixel while pressed.
            int sink = (int)System.Math.Round(Ui.S(1) * pressBlend);
            if (sink != 0) g.TranslateTransform(0, sink);

            if (Stacked)
            {
                PaintStacked(g);
                return;
            }

            bool hasText = !string.IsNullOrEmpty(Text);
            int textWidth = hasText ? TextRenderer.MeasureText(Text, Font, Size.Empty, MeasureFlags).Width : 0;
            int total = textWidth;
            if (iconName.Length > 0) total += IconPx + (hasText ? GapPx : 0);
            else if (HasDot) total += DotPx + (hasText ? GapPx : 0);
            if (trailingIconName.Length > 0) total += TrailingPx + (hasText || iconName.Length > 0 ? GapPx : 0);

            Color icon = Ui.Lerp(IconColor, HoverIconColor.IsEmpty ? IconColor : HoverIconColor, hoverBlend);
            Color text = Ui.Lerp(ForeColor, HoverTextColor.IsEmpty ? ForeColor : HoverTextColor, hoverBlend);

            int x = (Width - total) / 2;
            if (iconName.Length > 0)
            {
                g.DrawImage(IconFont.Get(iconName, IconPx, icon), x, (Height - IconPx) / 2, IconPx, IconPx);
                x += IconPx + (hasText ? GapPx : 0);
            }
            else if (HasDot)
            {
                using (var brush = new SolidBrush(DotColor))
                {
                    g.FillEllipse(brush, x, (Height - DotPx) / 2f, DotPx, DotPx);
                }
                x += DotPx + (hasText ? GapPx : 0);
            }
            if (hasText)
            {
                var textBounds = new Rectangle(x, 0, textWidth + 2, Height);
                TextRenderer.DrawText(g, Text, Font, textBounds, text,
                    TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.SingleLine);
                x += textWidth + GapPx;
            }
            if (trailingIconName.Length > 0)
            {
                g.DrawImage(IconFont.Get(trailingIconName, TrailingPx, TrailingIconColor), x, (Height - TrailingPx) / 2, TrailingPx, TrailingPx);
            }
        }
    }
}
