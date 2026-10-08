using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Hue_travel.Theme;

namespace Hue_travel.Panels.SearchPoi
{
    /// <summary>
    /// A search result: 48px photo, name, star rating badge, address, distance line and the
    /// "Bản đồ" / "+ Lộ trình" mini buttons. Text is the POI name.
    /// </summary>
    public class PoiResultCard : Control
    {
        private const TextFormatFlags MeasureFlags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;
        private const TextFormatFlags LeftFlags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine;

        private static Bitmap cachedPhoto;

        private readonly IconButton mapButton = new IconButton();
        private readonly IconButton routeButton = new IconButton();
        private bool isHovered;
        private bool isTinted;

        public event EventHandler ShowOnMapClicked;
        public event EventHandler AddToRouteClicked;

        public PoiResultCard()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Size = new Size(360, 76);
            Address = "";
            Distance = "";
            Rating = "";

            mapButton.Text = "Bản đồ";
            mapButton.IconName = "pin_drop";
            mapButton.IconSize = 11;
            mapButton.Font = ThemeFonts.Regular(10);
            mapButton.ForeColor = ThemeColors.Slate700;
            mapButton.IconColor = ThemeColors.Primary;
            mapButton.FillColor = Color.White;
            mapButton.HoverFillColor = ThemeColors.Slate100;
            mapButton.BorderColor = ThemeColors.Slate200;
            mapButton.Radius = 4;
            mapButton.HorizontalPadding = 6;
            mapButton.Gap = 2;
            mapButton.Height = 20;
            mapButton.Click += (s, e) => ShowOnMapClicked?.Invoke(this, EventArgs.Empty);

            routeButton.Text = "Lộ trình";
            routeButton.IconName = "add";
            routeButton.IconSize = 11;
            routeButton.Font = ThemeFonts.Medium(10);
            routeButton.ForeColor = Color.White;
            routeButton.IconColor = Color.White;
            routeButton.FillColor = ThemeColors.Primary;
            routeButton.HoverFillColor = ThemeColors.PrimaryContainer;
            routeButton.Radius = 4;
            routeButton.HorizontalPadding = 6;
            routeButton.Gap = 2;
            routeButton.Height = 20;
            routeButton.Click += (s, e) => AddToRouteClicked?.Invoke(this, EventArgs.Empty);

            Controls.Add(mapButton);
            Controls.Add(routeButton);
            LayoutButtons();
        }

        [Category("Appearance")] public string Address { get; set; }
        [Category("Appearance")] public string Distance { get; set; }
        [Category("Appearance")] public string Rating { get; set; }
        /// <summary>First result in the mockup is drawn on slate-50 instead of white.</summary>
        [Category("Appearance")] public bool Tinted { get { return isTinted; } set { isTinted = value; Invalidate(); } }

        [Browsable(true), EditorBrowsable(EditorBrowsableState.Always)]
        public override string Text { get { return base.Text; } set { base.Text = value; Invalidate(); } }

        // Control.Scale grows children after OnResize ran, so lay out again once scaling is done.
        protected override void OnLayout(LayoutEventArgs e) { base.OnLayout(e); LayoutButtons(); }

        protected override void OnResize(EventArgs e) { base.OnResize(e); LayoutButtons(); }
        protected override void OnMouseEnter(EventArgs e) { isHovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { isHovered = false; Invalidate(); base.OnMouseLeave(e); }

        private void LayoutButtons()
        {
            int top = Height - Ui.S(8) - routeButton.Height;
            routeButton.Location = new Point(Width - Ui.S(8) - routeButton.Width, top);
            mapButton.Location = new Point(routeButton.Left - Ui.S(4) - mapButton.Width, top);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color fill = isHovered ? Color.FromArgb(0xF3, 0xF9, 0xFE) : (isTinted ? ThemeColors.Slate50 : Color.White);
            using (GraphicsPath card = Ui.RoundedRect(new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f), Ui.S(8)))
            using (var brush = new SolidBrush(fill))
            using (var pen = new Pen(isHovered ? ThemeColors.Sky200 : ThemeColors.Slate200, 1f))
            {
                g.FillPath(brush, card);
                g.DrawPath(pen, card);
            }

            // 48px thumbnail (object-cover, rounded, 1px border)
            var thumb = new Rectangle(Ui.S(9), Ui.S(9), Ui.S(48), Ui.S(48));
            var state = g.Save();
            using (GraphicsPath clip = Ui.RoundedRect(thumb, Ui.S(4)))
            {
                g.SetClip(clip, CombineMode.Intersect);
                if (cachedPhoto == null) cachedPhoto = AppImages.PoiPhoto(thumb.Width, thumb.Height, 0);
                g.DrawImage(cachedPhoto, thumb);
            }
            g.Restore(state);
            using (GraphicsPath outline = Ui.RoundedRect(new RectangleF(thumb.X - 0.5f, thumb.Y - 0.5f, thumb.Width, thumb.Height), Ui.S(4)))
            using (var pen = new Pen(ThemeColors.Slate200, 1f))
            {
                g.DrawPath(pen, outline);
            }

            // rating badge (amber)
            int right = Width - Ui.S(10);
            int badgeWidth = 0;
            if (!string.IsNullOrEmpty(Rating))
            {
                badgeWidth = TextRenderer.MeasureText(Rating, ThemeFonts.Bold(10), Size.Empty, MeasureFlags).Width + Ui.S(8);
                var badge = new Rectangle(right - badgeWidth, Ui.S(9), badgeWidth, Ui.S(16));
                using (GraphicsPath path = Ui.RoundedRect(badge, Ui.S(3)))
                using (var brush = new SolidBrush(ThemeColors.Amber50))
                {
                    g.FillPath(brush, path);
                }
                TextRenderer.DrawText(g, Rating, ThemeFonts.Bold(10), badge, ThemeColors.Amber600,
                    TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.SingleLine);
            }

            int textLeft = Ui.S(66);
            TextRenderer.DrawText(g, Text, ThemeFonts.Bold(12), new Rectangle(textLeft, Ui.S(8), Math.Max(0, right - badgeWidth - Ui.S(6) - textLeft), Ui.S(18)), ThemeColors.Slate800, LeftFlags);
            TextRenderer.DrawText(g, Address, ThemeFonts.Regular(11), new Rectangle(textLeft, Ui.S(26), right - textLeft, Ui.S(16)), ThemeColors.Slate500, LeftFlags);
            TextRenderer.DrawText(g, Distance, ThemeFonts.SemiBold(10), new Rectangle(textLeft, Height - Ui.S(8) - mapButton.Height, Math.Max(0, mapButton.Left - textLeft - Ui.S(4)), mapButton.Height), ThemeColors.Emerald700, LeftFlags);
        }
    }
}
