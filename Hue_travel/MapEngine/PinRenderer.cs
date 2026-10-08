using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Hue_travel.Theme;

namespace Hue_travel.MapEngine
{
    /// <summary>How one map pin looks: caption pill, optional order badge, round marker with an icon.</summary>
    public sealed class PinSpec
    {
        public string Caption { get; set; }
        public string Icon { get; set; }
        public int? Order { get; set; }
        public Color Marker { get; set; }
        public int MarkerSize { get; set; }
        public int IconSize { get; set; }
        public Color LabelFill { get; set; }
        public Color LabelBorder { get; set; }
        public Color LabelText { get; set; }
        public Color BadgeFill { get; set; }
        public Color BadgeText { get; set; }
        public int LabelRadius { get; set; }
        public bool StatusDot { get; set; }
        public float CaptionPx { get; set; }
        public bool CaptionBold { get; set; }
    }

    /// <summary>
    /// Draws the mockup's pins (caption pill, circular icon marker and stem) with GDI+ so they can be
    /// used as MapWinGIS point pictures. The background is the map colour, keyed out as transparent.
    /// </summary>
    public static class PinRenderer
    {
        private const int LabelHeight = 24;
        private const int Gap = 4;
        private const int StemHeight = 8;

        public static Bitmap Render(PinSpec spec, Color background)
        {
            Font font = spec.CaptionBold ? ThemeFonts.Bold(spec.CaptionPx) : ThemeFonts.SemiBold(spec.CaptionPx);
            const TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.Left | TextFormatFlags.VerticalCenter;
            int textWidth = TextRenderer.MeasureText(spec.Caption, font, Size.Empty, flags).Width;
            int lead = (spec.Order.HasValue ? 22 : 0) + (spec.StatusDot ? 12 : 0);
            int labelWidth = textWidth + lead + 20;

            int width = Math.Max(labelWidth, spec.MarkerSize) + 6;
            int height = LabelHeight + Gap + spec.MarkerSize + StemHeight + 4;
            var bmp = new Bitmap(width, height);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(background);
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // caption pill
                var label = new RectangleF((width - labelWidth) / 2f, 1, labelWidth, LabelHeight);
                using (GraphicsPath path = Ui.RoundedRect(label, spec.LabelRadius))
                using (var fill = new SolidBrush(spec.LabelFill))
                {
                    g.FillPath(fill, path);
                    if (spec.LabelBorder != Color.Empty)
                    {
                        using (var pen = new Pen(spec.LabelBorder, 1f)) g.DrawPath(pen, path);
                    }
                }

                float x = label.X + 10;
                if (spec.StatusDot)
                {
                    using (var dot = new SolidBrush(ThemeColors.Amber400)) g.FillEllipse(dot, x, label.Y + (LabelHeight - 8) / 2f, 8, 8);
                    x += 12;
                }
                if (spec.Order.HasValue)
                {
                    float d = 18;
                    var badge = new RectangleF(x, label.Y + (LabelHeight - d) / 2f, d, d);
                    using (var fill = new SolidBrush(spec.BadgeFill)) g.FillEllipse(fill, badge);
                    TextRenderer.DrawText(g, spec.Order.Value.ToString(), ThemeFonts.Bold(11), Rectangle.Round(badge), spec.BadgeText,
                        TextFormatFlags.NoPadding | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
                    x += 22;
                }
                TextRenderer.DrawText(g, spec.Caption, font, new Rectangle((int)x, (int)label.Y, textWidth + 4, LabelHeight), spec.LabelText, flags);

                // stem, marker disc with white ring, icon
                float cx = width / 2f;
                float top = label.Bottom + Gap;
                using (var stem = new SolidBrush(spec.Marker))
                {
                    g.FillPolygon(stem, new[] { new PointF(cx - 4, top + spec.MarkerSize - 2), new PointF(cx + 4, top + spec.MarkerSize - 2), new PointF(cx, top + spec.MarkerSize + StemHeight) });
                }
                var disc = new RectangleF(cx - spec.MarkerSize / 2f, top, spec.MarkerSize, spec.MarkerSize);
                using (var fill = new SolidBrush(spec.Marker)) g.FillEllipse(fill, disc);
                using (var ring = new Pen(Color.White, 2f))
                {
                    disc.Inflate(-1f, -1f);
                    g.DrawEllipse(ring, disc);
                }
                g.DrawImage(IconFont.Get(spec.Icon, spec.IconSize, Color.White), cx - spec.IconSize / 2f, top + (spec.MarkerSize - spec.IconSize) / 2f, spec.IconSize, spec.IconSize);
            }
            return bmp;
        }

        /// <summary>The GDI picture object MapWinGIS wants for <c>Image.Picture</c> (an stdole IPictureDisp).</summary>
        public static object ToPictureDisp(Bitmap bitmap) => PictureConverter.Convert(bitmap);

        private sealed class PictureConverter : AxHost
        {
            private PictureConverter() : base("02BF25D5-8C17-4B23-BC80-D3488ABDDC6B") { }

            public static object Convert(Image image) => GetIPictureDispFromPicture(image);
        }
    }
}
