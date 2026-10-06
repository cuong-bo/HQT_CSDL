using System.Drawing;

namespace Hue_travel.Theme
{
    /// <summary>
    /// The mockup specifies "Plus Jakarta Sans", which is not installed on a stock
    /// Windows machine. We substitute "Segoe UI" (full Vietnamese diacritic coverage,
    /// present on every supported Windows version) rather than referencing a font
    /// family that may fail to resolve at runtime.
    /// </summary>
    public static class ThemeFonts
    {
        public const string FamilyName = "Segoe UI";

        public static readonly Font HeadlineXl = new Font(FamilyName, 18F, FontStyle.Bold);
        public static readonly Font HeadlineLg = new Font(FamilyName, 13.5F, FontStyle.Bold);
        public static readonly Font HeadlineMd = new Font(FamilyName, 11.25F, FontStyle.Bold);
        public static readonly Font BodyLg = new Font(FamilyName, 10.5F, FontStyle.Regular);
        public static readonly Font BodyMd = new Font(FamilyName, 9.75F, FontStyle.Regular);
        public static readonly Font BodySm = new Font(FamilyName, 9F, FontStyle.Regular);
        public static readonly Font LabelMd = new Font(FamilyName, 9F, FontStyle.Bold);
        public static readonly Font LabelSm = new Font(FamilyName, 8.25F, FontStyle.Bold);
    }
}
