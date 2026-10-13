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
        public static readonly Color CategoryScenery = ColorTranslator.FromHtml("#0891B2");

        // "Hue Travel" brand purple, used by the start-up sign-in screen (the logo and the card in the mockup).
        public static readonly Color BrandPurple = ColorTranslator.FromHtml("#5B2A86");
        public static readonly Color BrandPurpleDark = ColorTranslator.FromHtml("#44206A");
        public static readonly Color BrandPurpleLight = ColorTranslator.FromHtml("#F3ECF9");

        public static readonly Color StatusOnline = ColorTranslator.FromHtml("#10B981");
        public static readonly Color BadgeAmber = ColorTranslator.FromHtml("#F59E0B");

        // Tailwind palette entries referenced directly by the mockup's class names.
        public static readonly Color Slate50 = ColorTranslator.FromHtml("#F8FAFC");
        public static readonly Color Slate100 = ColorTranslator.FromHtml("#F1F5F9");
        public static readonly Color Slate200 = ColorTranslator.FromHtml("#E2E8F0");
        public static readonly Color Slate300 = ColorTranslator.FromHtml("#CBD5E1");
        public static readonly Color Slate400 = ColorTranslator.FromHtml("#94A3B8");
        public static readonly Color Slate500 = ColorTranslator.FromHtml("#64748B");
        public static readonly Color Slate600 = ColorTranslator.FromHtml("#475569");
        public static readonly Color Slate700 = ColorTranslator.FromHtml("#334155");
        public static readonly Color Slate800 = ColorTranslator.FromHtml("#1E293B");
        public static readonly Color Slate900 = ColorTranslator.FromHtml("#0F172A");

        public static readonly Color Sky50 = ColorTranslator.FromHtml("#F0F9FF");
        public static readonly Color Sky100 = ColorTranslator.FromHtml("#E0F2FE");
        public static readonly Color Sky200 = ColorTranslator.FromHtml("#BAE6FD");
        public static readonly Color Sky300 = ColorTranslator.FromHtml("#7DD3FC");
        public static readonly Color Sky400 = ColorTranslator.FromHtml("#38BDF8");
        public static readonly Color Sky500 = ColorTranslator.FromHtml("#0EA5E9");
        public static readonly Color Sky600 = ColorTranslator.FromHtml("#0284C7");
        public static readonly Color Sky700 = ColorTranslator.FromHtml("#0369A1");
        public static readonly Color Sky800 = ColorTranslator.FromHtml("#075985");
        public static readonly Color Sky900 = ColorTranslator.FromHtml("#0C4A6E");

        public static readonly Color Amber50 = ColorTranslator.FromHtml("#FFFBEB");
        public static readonly Color Amber100 = ColorTranslator.FromHtml("#FEF3C7");
        public static readonly Color Amber400 = ColorTranslator.FromHtml("#FBBF24");
        public static readonly Color Amber500 = ColorTranslator.FromHtml("#F59E0B");
        public static readonly Color Amber600 = ColorTranslator.FromHtml("#D97706");
        public static readonly Color Amber700 = ColorTranslator.FromHtml("#B45309");
        public static readonly Color Amber900 = ColorTranslator.FromHtml("#78350F");

        public static readonly Color Orange600 = ColorTranslator.FromHtml("#EA580C");
        public static readonly Color Blue600 = ColorTranslator.FromHtml("#2563EB");
        public static readonly Color Cyan400 = ColorTranslator.FromHtml("#22D3EE");
        public static readonly Color Cyan600 = ColorTranslator.FromHtml("#0891B2");
        public static readonly Color Emerald500 = ColorTranslator.FromHtml("#10B981");
        public static readonly Color Emerald700 = ColorTranslator.FromHtml("#047857");
        public static readonly Color Red500 = ColorTranslator.FromHtml("#EF4444");
    }
}
