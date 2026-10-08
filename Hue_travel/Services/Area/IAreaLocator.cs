namespace Hue_travel.Services.Area
{
    /// <summary>Names the area under a map position. A PostGIS-backed version will replace the demo one.</summary>
    public interface IAreaLocator
    {
        /// <summary>Display name for the area containing the point (degrees).</summary>
        string Locate(double longitude, double latitude);
    }
}
