namespace Hue_travel.Services.Area
{
    /// <summary>Names the area under a map position. A PostGIS-backed version will replace the demo one.</summary>
    public interface IAreaLocator
    {
        /// <summary>Display name for the area containing the map point (x, y in map units).</summary>
        string Locate(double x, double y);
    }
}
