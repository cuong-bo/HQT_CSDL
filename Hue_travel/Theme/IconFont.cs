using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;

namespace Hue_travel.Theme
{
    /// <summary>
    /// Renders Material Symbols Outlined glyphs (the icon set used by GiaoDien/code.html)
    /// to bitmaps. Safe to call from InitializeComponent(): it never throws, and returns
    /// an empty bitmap if the embedded font is unavailable (design-time).
    /// </summary>
    public static class IconFont
    {
        // name -> codepoint, a subset of the official Material Symbols codepoints file.
        private static readonly Dictionary<string, string> Glyphs = new Dictionary<string, string>
        {
            { "category", "" }, { "alt_route", "" }, { "search", "" },
            { "menu_book", "" }, { "zoom_in", "" }, { "zoom_out", "" },
            { "travel_explore", "" }, { "straighten", "" }, { "layers", "" },
            { "expand_more", "" }, { "expand_less", "" }, { "map", "" },
            { "satellite_alt", "" }, { "dark_mode", "" }, { "apps", "" },
            { "account_balance", "" }, { "restaurant", "" }, { "hotel", "" },
            { "kayaking", "" }, { "account_circle", "" }, { "minimize", "" },
            { "crop_square", "" }, { "filter_none", "" }, { "close", "" },
            { "lightbulb", "" }, { "directions", "" }, { "save_alt", "" },
            { "print", "" }, { "tune", "" }, { "pin_drop", "" },
            { "add", "" }, { "remove", "" }, { "campaign", "" },
            { "route", "" }, { "check_box", "" }, { "check", "" },
            { "call", "" }, { "download", "" }, { "center_focus_strong", "" },
            { "lock", "" }, { "mail", "" }, { "login", "" },
            { "bookmark_add", "" }, { "person_add", "" }, { "lock_reset", "" },
            { "verified_user", "" }, { "pin", "" }, { "refresh", "" },
            { "visibility", "" }, { "visibility_off", "" }, { "arrow_back", "" },
            { "support_agent", "" }, { "explore", "" }, { "wb_sunny", "" },
            { "navigation", "" }, { "pan_tool", "" }, { "train", "" },
            { "temple_buddhist", "" }, { "place", "" }, { "check_circle", "" },
            { "star", "" }, { "open_in_new", "" },
        };

        private static readonly Dictionary<string, Bitmap> Cache = new Dictionary<string, Bitmap>();

        public static bool Contains(string name) => Glyphs.ContainsKey(name);

        /// <summary>Returns a shared, cached bitmap. Callers must not dispose it.</summary>
        public static Bitmap Get(string name, int px, Color color)
        {
            string key = name + "|" + px + "|" + color.ToArgb();
            lock (Cache)
            {
                Bitmap cached;
                if (Cache.TryGetValue(key, out cached)) return cached;
                Bitmap created = Render(name, px, color);
                Cache[key] = created;
                return created;
            }
        }

        // The usual window "minimise" mark: a short horizontal bar centred in the icon box.
        private static void DrawDash(Bitmap bmp, int px, Color color)
        {
            int width = Math.Max(4, (int)Math.Round(px * 0.6));
            int thickness = Math.Max(1, (int)Math.Round(px / 10.0));
            using (Graphics g = Graphics.FromImage(bmp))
            using (var brush = new SolidBrush(color))
            {
                g.FillRectangle(brush, (px - width) / 2, (px - thickness) / 2, width, thickness);
            }
        }

        // "Measure area": a four-cornered outline with a dot on every corner (no matching glyph in the font subset).
        private static void DrawArea(Bitmap bmp, int px, Color color)
        {
            float s = px / 16f;
            PointF[] corners =
            {
                new PointF(3f * s, 4f * s), new PointF(13f * s, 3f * s), new PointF(12f * s, 13f * s), new PointF(2.5f * s, 11.5f * s),
            };
            using (Graphics g = Graphics.FromImage(bmp))
            using (var pen = new Pen(color, Math.Max(1f, 1.3f * s)) { LineJoin = System.Drawing.Drawing2D.LineJoin.Round })
            using (var fill = new SolidBrush(Color.FromArgb(48, color)))
            using (var dot = new SolidBrush(color))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.FillPolygon(fill, corners);
                g.DrawPolygon(pen, corners);
                float r = 1.7f * s;
                foreach (PointF c in corners) g.FillEllipse(dot, c.X - r, c.Y - r, 2 * r, 2 * r);
            }
        }

        // "Landscape": two mountains and a sun.
        private static void DrawLandscape(Bitmap bmp, int px, Color color)
        {
            float s = px / 16f;
            PointF[] ridge =
            {
                new PointF(0.5f * s, 13.5f * s), new PointF(5.5f * s, 5.5f * s), new PointF(8.5f * s, 10f * s),
                new PointF(10.5f * s, 7.5f * s), new PointF(15.5f * s, 13.5f * s),
            };
            using (Graphics g = Graphics.FromImage(bmp))
            using (var fill = new SolidBrush(color))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.FillPolygon(fill, ridge);
                g.FillEllipse(fill, 11f * s, 1.5f * s, 3.2f * s, 3.2f * s);
            }
        }

        private static Bitmap Render(string name, int px, Color color)
        {
            if (px <= 0) px = 1;
            var bmp = new Bitmap(px, px, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            if (name == "minimize")
            {
                DrawDash(bmp, px, color);
                return bmp;
            }
            if (name == "measure_area")
            {
                DrawArea(bmp, px, color);
                return bmp;
            }
            if (name == "landscape")
            {
                DrawLandscape(bmp, px, color);
                return bmp;
            }

            string glyph;
            FontFamily family = FontLoader.IconFamily;
            if (family == null || !Glyphs.TryGetValue(name, out glyph)) return bmp;

            try
            {
            using (var g = Graphics.FromImage(bmp))
            using (var font = new Font(family, px, FontStyle.Regular, GraphicsUnit.Pixel))
            using (var brush = new SolidBrush(color))
            using (var format = new StringFormat())
            {
                // Note: StringFormat.GenericTypographic makes these PUA glyphs disappear, so a plain format is used.
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                g.DrawString(glyph, font, brush, new RectangleF(0, 0, px, px), format);
            }
            }
            catch (ArgumentException)
            {
                // Font lacks the regular style: keep the blank bitmap rather than crash the designer.
            }
            return bmp;
        }
    }
}
