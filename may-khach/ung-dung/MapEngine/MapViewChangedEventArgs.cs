using System;

namespace Hue_travel.MapEngine
{
    /// <summary>The visible part of the map changed.</summary>
    public sealed class MapViewChangedEventArgs : EventArgs
    {
        public MapViewChangedEventArgs(double centerX, double centerY, double centerLongitude, double centerLatitude, double scaleDenominator)
        {
            CenterX = centerX;
            CenterY = centerY;
            CenterLongitude = centerLongitude;
            CenterLatitude = centerLatitude;
            ScaleDenominator = scaleDenominator;
        }

        /// <summary>Centre of the view in map units (VN-2000 UTM metres).</summary>
        public double CenterX { get; }

        public double CenterY { get; }

        /// <summary>Centre of the view in degrees.</summary>
        public double CenterLongitude { get; }

        public double CenterLatitude { get; }

        /// <summary>N in "1 : N"; NaN when it cannot be computed.</summary>
        public double ScaleDenominator { get; }
    }
}
