using System.Collections.Generic;

namespace Hue_travel.Services.Area
{
    /// <summary>
    /// Where the demo points of interest are (mockup canvas coordinates, see <see cref="SceneGeoreference"/>).
    /// "Show on map" zooms to these; real positions will come from the PostGIS POI layer.
    /// </summary>
    public static class DemoPoiLocations
    {
        private static readonly Dictionary<string, double[]> Canvas = new Dictionary<string, double[]>
        {
            { "ga-hue", new[] { 430.0, 670.0 } },
            { "dai-noi", new[] { 620.0, 320.0 } },
            { "thien-mu", new[] { 300.0, 480.0 } },
            { "bun-bo-mu-roi", new[] { 890.0, 560.0 } },
        };

        /// <summary>Easting / northing (VN-2000 UTM metres) of a demo POI; false when the POI has no known position.</summary>
        public static bool TryGet(string poiId, out double easting, out double northing)
        {
            easting = 0;
            northing = 0;
            double[] xy;
            if (poiId == null || !Canvas.TryGetValue(poiId, out xy)) return false;
            easting = SceneGeoreference.X(xy[0]);
            northing = SceneGeoreference.Y(xy[1]);
            return true;
        }
    }
}
