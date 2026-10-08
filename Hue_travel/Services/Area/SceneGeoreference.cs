namespace Hue_travel.Services.Area
{
    /// <summary>
    /// Places the demo scene (the mockup's 1600x900 canvas) on the real Hue map in VN-2000 / UTM zone 48N, so map units
    /// are metres. The Hoang Thanh Hue pin is the anchor; real layers will carry their own coordinates.
    /// </summary>
    public static class SceneGeoreference
    {
        /// <summary>The Imperial City is about 600 m wide and spans roughly 300 canvas units.</summary>
        public const double MetresPerCanvasUnit = 2.0;

        private const double AnchorCanvasX = 620;
        private const double AnchorCanvasY = 320;

        // 16.4698 N, 107.5786 E converted to EPSG:3405 with MapWinGIS.
        private const double AnchorEasting = 775116.47;
        private const double AnchorNorthing = 1822770.60;

        /// <summary>Easting (m) of a mockup canvas x.</summary>
        public static double X(double canvasX) => AnchorEasting + (canvasX - AnchorCanvasX) * MetresPerCanvasUnit;

        /// <summary>Northing (m) of a mockup canvas y (canvas y grows downwards).</summary>
        public static double Y(double canvasY) => AnchorNorthing + (AnchorCanvasY - canvasY) * MetresPerCanvasUnit;
    }
}
