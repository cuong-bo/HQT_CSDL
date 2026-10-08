using System;

namespace Hue_travel.Services.Area
{
    /// <summary>Stand-in locator for the demo scene: the nearest landmark within reach names the area (landmarks given in canvas coordinates).</summary>
    public sealed class DemoAreaLocator : IAreaLocator
    {
        public const string DefaultArea = "Trung tâm Cố đô Huế & Bờ sông Hương";

        private const double ReachInMetres = 150;

        private static readonly Landmark[] Landmarks =
        {
            new Landmark("Khu vực Hoàng Thành Huế", 620, 320),
            new Landmark("Khu vực chùa Thiên Mụ & sông Hương", 300, 480),
            new Landmark("Khu vực Ga Huế", 430, 670),
            new Landmark("Khu ẩm thực phố Huế", 890, 560),
        };

        public string Locate(double x, double y)
        {
            string best = DefaultArea;
            double bestDistance = ReachInMetres;
            foreach (Landmark landmark in Landmarks)
            {
                double distance = Math.Sqrt(Math.Pow(x - landmark.X, 2) + Math.Pow(y - landmark.Y, 2));
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                best = landmark.Name;
            }
            return best;
        }

        private sealed class Landmark
        {
            public Landmark(string name, double x, double y)
            {
                Name = name;
                X = SceneGeoreference.X(x);
                Y = SceneGeoreference.Y(y);
            }

            public string Name { get; }
            public double X { get; }
            public double Y { get; }
        }
    }
}
