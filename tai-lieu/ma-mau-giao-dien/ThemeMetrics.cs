namespace Hue_travel.Theme
{
    /// <summary>
    /// Sizes shared by every control, in 96-DPI design pixels (pass them through <c>Ui.S</c> before drawing).
    /// A child's corner radius is its parent's radius minus the padding between them.
    /// </summary>
    public static class ThemeMetrics
    {
        public const int RadiusXs = 6;       // tags, small cells
        public const int RadiusSm = 10;      // buttons, input fields, list rows
        public const int RadiusMd = 14;      // small cards, thumbnails
        public const int RadiusLg = 20;      // floating panels, the place card
        public const int RadiusXl = 28;      // the main window, big dialogs
        public const int RadiusFull = 9999;  // pills: chips, the search box, badges

        public const int SpaceXs = 4;
        public const int SpaceSm = 8;
        public const int SpaceMd = 12;
        public const int SpaceLg = 16;
        public const int SpaceXl = 24;
        public const int SpaceXxl = 32;
    }
}
