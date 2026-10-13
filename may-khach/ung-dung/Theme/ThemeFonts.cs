using System;
using System.Collections.Generic;
using System.Drawing;

namespace Hue_travel.Theme
{
    /// <summary>
    /// Plus Jakarta Sans (the mockup's font) loaded from embedded resources via
    /// <see cref="FontLoader"/>, with Segoe UI as the fallback if loading fails.
    /// Sizes are given in CSS pixels as in GiaoDien/code.html and converted to points.
    /// </summary>
    public static class ThemeFonts
    {
        public const string FamilyName = "Plus Jakarta Sans";
        private const string FallbackFamily = "Segoe UI";
        private const string MonoFamily = "Consolas";

        private static readonly Dictionary<string, Font> Cache = new Dictionary<string, Font>();

        public static Font Regular(float px) => Make(FamilyName, px, FontStyle.Regular);
        public static Font Medium(float px) => Make(FamilyName + " Medium", px, FontStyle.Regular);
        public static Font SemiBold(float px) => Make(FamilyName + " SemiBold", px, FontStyle.Regular);
        public static Font Bold(float px) => Make(FamilyName, px, FontStyle.Bold);
        public static Font Mono(float px) => Make(MonoFamily, px, FontStyle.Regular);
        public static Font MonoBold(float px) => Make(MonoFamily, px, FontStyle.Bold);

        // Presets kept from the shell pass; now backed by Plus Jakarta Sans.
        public static readonly Font HeadlineXl = Bold(24);
        public static readonly Font HeadlineLg = Bold(18);
        public static readonly Font HeadlineMd = Bold(15);
        public static readonly Font BodyLg = Regular(14);
        public static readonly Font BodyMd = Regular(13);
        public static readonly Font BodySm = Regular(12);
        public static readonly Font LabelMd = Bold(12);
        public static readonly Font LabelSm = Bold(11);

        private static Font Make(string family, float px, FontStyle style)
        {
            string key = family + "|" + px + "|" + (int)style;
            lock (Cache)
            {
                Font cached;
                if (Cache.TryGetValue(key, out cached)) return cached;

                float points = px * 72f / 96f;
                FontFamily embedded = family == MonoFamily ? null : FontLoader.Find(family);
                Font font;
                try
                {
                    font = embedded != null && embedded.IsStyleAvailable(style)
                        ? new Font(embedded, points, style, GraphicsUnit.Point)
                        : new Font(family == MonoFamily ? MonoFamily : FallbackFamily, points, style, GraphicsUnit.Point);
                }
                catch (ArgumentException)
                {
                    font = new Font(FallbackFamily, points, FontStyle.Regular, GraphicsUnit.Point);
                }
                Cache[key] = font;
                return font;
            }
        }
    }
}
