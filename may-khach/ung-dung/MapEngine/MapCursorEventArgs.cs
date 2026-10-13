using System;

namespace Hue_travel.MapEngine
{
    /// <summary>The mouse pointer moved over the map: its position in VN-2000 UTM metres and in degrees.</summary>
    public sealed class MapCursorEventArgs : EventArgs
    {
        public MapCursorEventArgs(double easting, double northing, double longitude, double latitude)
        {
            Easting = easting;
            Northing = northing;
            Longitude = longitude;
            Latitude = latitude;
        }

        public double Easting { get; }

        public double Northing { get; }

        public double Longitude { get; }

        public double Latitude { get; }
    }
}
