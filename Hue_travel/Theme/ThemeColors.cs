using System.Drawing;

namespace Hue_travel.Theme
{
    /// <summary>Color tokens transcribed from GiaoDien/code.html's tailwind.config (ground truth over DESIGN.md's front-matter).</summary>
    public static class ThemeColors
    {
        public static readonly Color Primary = ColorTranslator.FromHtml("#0284C7");
        public static readonly Color PrimaryContainer = ColorTranslator.FromHtml("#0369A1");
        public static readonly Color OnPrimary = ColorTranslator.FromHtml("#FFFFFF");

        public static readonly Color Surface = ColorTranslator.FromHtml("#F8FAFC");
        public static readonly Color Background = ColorTranslator.FromHtml("#F0F9FF");
        public static readonly Color SurfaceContainerLowest = ColorTranslator.FromHtml("#FFFFFF");
        public static readonly Color SurfaceContainerLow = ColorTranslator.FromHtml("#F0F7FF");
        public static readonly Color SurfaceContainer = ColorTranslator.FromHtml("#E0F2FE");
        public static readonly Color SurfaceContainerHigh = ColorTranslator.FromHtml("#BAE6FD");

        public static readonly Color OnSurface = ColorTranslator.FromHtml("#0F172A");
        public static readonly Color OnSurfaceVariant = ColorTranslator.FromHtml("#334155");

        public static readonly Color Secondary = ColorTranslator.FromHtml("#64748B");
        public static readonly Color Tertiary = ColorTranslator.FromHtml("#B45309");

        public static readonly Color Outline = ColorTranslator.FromHtml("#94A3B8");
        public static readonly Color OutlineVariant = ColorTranslator.FromHtml("#CBD5E1");

        public static readonly Color Error = ColorTranslator.FromHtml("#DC2626");

        public static readonly Color CategoryHeritage = ColorTranslator.FromHtml("#D97706");
        public static readonly Color CategoryFood = ColorTranslator.FromHtml("#EA580C");
        public static readonly Color CategoryStay = ColorTranslator.FromHtml("#2563EB");
        public static readonly Color CategoryRiver = ColorTranslator.FromHtml("#0891B2");

        public static readonly Color StatusOnline = ColorTranslator.FromHtml("#10B981");
        public static readonly Color BadgeAmber = ColorTranslator.FromHtml("#F59E0B");
    }
}
