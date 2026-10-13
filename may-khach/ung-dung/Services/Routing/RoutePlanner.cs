using System;
using System.Collections.Generic;
using System.Linq;

namespace Hue_travel.Services.Routing
{
    /// <summary>
    /// Orders sightseeing stops for the shortest trip. Distances are straight lines scaled by a road detour factor
    /// until the street network (pgRouting) is available, so every figure is an estimate.
    /// </summary>
    public static class RoutePlanner
    {
        /// <summary>Streets are longer than the straight line between two places.</summary>
        public const double RoadDetourFactor = 1.3;

        /// <summary>Average speed in city traffic (motorbike or car), km/h.</summary>
        public const double AverageSpeedKmh = 25;

        // Up to this many stops every order is tried (8! = 40,320), so the answer is exactly the shortest one.
        private const int ExactSearchLimit = 8;

        /// <summary>Best order for visiting all <paramref name="stops"/>, beginning at <paramref name="start"/> when one is given.</summary>
        public static RoutePlan Plan(GeoPoint? start, IReadOnlyList<GeoPoint> stops)
        {
            if (stops == null || stops.Count == 0)
                return new RoutePlan(new int[0], new double[0], 0, 0, 0);

            int[] listed = Enumerable.Range(0, stops.Count).ToArray();
            int[] best = stops.Count <= ExactSearchLimit ? ShortestOrder(start, stops) : NearestNeighbourOrder(start, stops);

            double[] legs = Legs(start, stops, best);
            double totalKm = legs.Sum();
            double listedKm = Legs(start, stops, listed).Sum();
            int travelMinutes = ToMinutes(totalKm);
            int savedMinutes = Math.Max(0, (int)Math.Round((listedKm - totalKm) / AverageSpeedKmh * 60));
            return new RoutePlan(best, legs, totalKm, travelMinutes, savedMinutes);
        }

        private static int ToMinutes(double km) => (int)Math.Round(km / AverageSpeedKmh * 60);

        private static double Km(GeoPoint from, GeoPoint to) => GeoMath.Between(from, to) * RoadDetourFactor;

        private static double[] Legs(GeoPoint? start, IReadOnlyList<GeoPoint> stops, int[] order)
        {
            var legs = new List<double>();
            if (start.HasValue) legs.Add(Km(start.Value, stops[order[0]]));
            for (int i = 1; i < order.Length; i++) legs.Add(Km(stops[order[i - 1]], stops[order[i]]));
            return legs.ToArray();
        }

        // Depth-first over every order, dropping a branch as soon as it is already longer than the best found.
        private static int[] ShortestOrder(GeoPoint? start, IReadOnlyList<GeoPoint> stops)
        {
            int n = stops.Count;
            int[] bestOrder = Enumerable.Range(0, n).ToArray();
            double bestCost = double.MaxValue;
            int[] order = new int[n];
            bool[] used = new bool[n];

            void Visit(int depth, double cost)
            {
                if (cost >= bestCost) return;
                if (depth == n)
                {
                    bestCost = cost;
                    Array.Copy(order, bestOrder, n);
                    return;
                }
                for (int i = 0; i < n; i++)
                {
                    if (used[i]) continue;
                    double leg = depth > 0 ? Km(stops[order[depth - 1]], stops[i]) : (start.HasValue ? Km(start.Value, stops[i]) : 0);
                    used[i] = true;
                    order[depth] = i;
                    Visit(depth + 1, cost + leg);
                    used[i] = false;
                }
            }

            Visit(0, 0);
            return bestOrder;
        }

        // Greedy fallback for long lists: always go to the closest stop not visited yet.
        private static int[] NearestNeighbourOrder(GeoPoint? start, IReadOnlyList<GeoPoint> stops)
        {
            var remaining = Enumerable.Range(0, stops.Count).ToList();
            var order = new List<int>();
            GeoPoint? current = start;
            while (remaining.Count > 0)
            {
                int next = current.HasValue
                    ? remaining.OrderBy(i => Km(current.Value, stops[i])).First()
                    : remaining[0];
                order.Add(next);
                remaining.Remove(next);
                current = stops[next];
            }
            return order.ToArray();
        }
    }
}
