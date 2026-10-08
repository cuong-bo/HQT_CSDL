using System;

namespace Hue_travel.MapEngine
{
    /// <summary>The visible part of the map changed.</summary>
    public sealed class MapViewChangedEventArgs : EventArgs
    {
        public MapViewChangedEventArgs(double centerX, double centerY, double scaleDenominator)
        {
            CenterX = centerX;
            CenterY = centerY;
            ScaleDenominator = scaleDenominator;
        }

        /// <summary>Centre of the view in map units.</summary>
        public double CenterX { get; }

        public double CenterY { get; }

        /// <summary>N in "1 : N"; NaN when it cannot be computed.</summary>
        public double ScaleDenominator { get; }
    }
}
