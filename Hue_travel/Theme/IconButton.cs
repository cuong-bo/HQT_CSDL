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

        private bool isHovered;
        private bool isPressed;
        private float hoverBlend;
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

        private void StepHover()
        {
            float target = isHovered ? 1f : 0f;
            hoverBlend += hoverBlend < target ? HoverStep : -HoverStep;
            if ((hoverBlend >= target && target == 1f) || (hoverBlend <= target && target == 0f))
            {
                hoverBlend = target;
                hoverTimer.Stop();
            }
            Invalidate();
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

        public int ContentWidth
        {
            get
            {
                int width = horizontalPadding * 2;
                bool hasText = !string.IsNullOrEmpty(Text);
                if (iconName.Length > 0) width += iconSize + (hasText ? gap : 0);
                else if (HasDot) width += DotSize + (hasText ? gap : 0);
                if (hasText) width += TextRenderer.MeasureText(Text, Font, Size.Empty, MeasureFlags).Width;
                if (trailingIconName.Length > 0) width += trailingIconSize + (hasText || iconName.Length > 0 ? gap : 0);
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
            int blockHeight = iconSize + (textHeight > 0 ? gap : 0) + textHeight;
            int y = System.Math.Max(0, (Height - blockHeight) / 2);

            g.DrawImage(IconFont.Get(iconName, iconSize, icon), (Width - iconSize) / 2, y, iconSize, iconSize);
            if (textHeight > 0)
            {
                TextRenderer.DrawText(g, Text, Font, new Rectangle(0, y + iconSize + gap, Width, textHeight + 2), text, flags);
            }
        }

        private const int DotSize = 8;

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
        protected override void OnMouseDown(MouseEventArgs e) { isPressed = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { isPressed = false; Invalidate(); base.OnMouseUp(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var bounds = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);

            Color fill = Ui.Lerp(FillColor, HoverFillColor, hoverBlend);
            if (isPressed) fill = ControlPaint.Dark(fill, 0.03f);
            if (fill.A > 0)
            {
                using (var path = Ui.RoundedRect(bounds, Radius))
                using (var brush = new SolidBrush(fill))
                {
                    g.FillPath(brush, path);
                }
            }
            if (BorderColor.A > 0 && !BorderColor.IsEmpty)
            {
                using (var path = Ui.RoundedRect(bounds, Radius))
                using (var pen = new Pen(BorderColor, 1f))
                {
                    g.DrawPath(pen, path);
                }
            }

            if (Stacked)
            {
                PaintStacked(g);
                return;
            }

            bool hasText = !string.IsNullOrEmpty(Text);
            int textWidth = hasText ? TextRenderer.MeasureText(Text, Font, Size.Empty, MeasureFlags).Width : 0;
            int total = textWidth;
            if (iconName.Length > 0) total += iconSize + (hasText ? gap : 0);
            else if (HasDot) total += DotSize + (hasText ? gap : 0);
            if (trailingIconName.Length > 0) total += trailingIconSize + (hasText || iconName.Length > 0 ? gap : 0);

            Color icon = Ui.Lerp(IconColor, HoverIconColor.IsEmpty ? IconColor : HoverIconColor, hoverBlend);
            Color text = Ui.Lerp(ForeColor, HoverTextColor.IsEmpty ? ForeColor : HoverTextColor, hoverBlend);

            int x = (Width - total) / 2;
            if (iconName.Length > 0)
            {
                g.DrawImage(IconFont.Get(iconName, iconSize, icon), x, (Height - iconSize) / 2, iconSize, iconSize);
                x += iconSize + (hasText ? gap : 0);
            }
            else if (HasDot)
            {
                using (var brush = new SolidBrush(DotColor))
                {
                    g.FillEllipse(brush, x, (Height - DotSize) / 2f, DotSize, DotSize);
                }
                x += DotSize + (hasText ? gap : 0);
            }
            if (hasText)
            {
                var textBounds = new Rectangle(x, 0, textWidth + 2, Height);
                TextRenderer.DrawText(g, Text, Font, textBounds, text,
                    TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.SingleLine);
                x += textWidth + gap;
            }
            if (trailingIconName.Length > 0)
            {
                g.DrawImage(IconFont.Get(trailingIconName, trailingIconSize, TrailingIconColor), x, (Height - trailingIconSize) / 2, trailingIconSize, trailingIconSize);
            }
        }
    }
}
