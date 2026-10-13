using System;
using System.Collections.Generic;
using System.Linq;
using Hue_travel.Models;
using Hue_travel.Services.Online;

namespace Hue_travel.Services.Routing
{
    /// <summary>One stretch of a trip: from the previous place (or the start) to the next stop.</summary>
    public sealed class TripLeg
    {
        public string FromName { get; set; }
        public string ToName { get; set; }
        public double Meters { get; set; }
        public double Seconds { get; set; }

        /// <summary>The line to drive, in map metres (VN-2000 / UTM 48N), from the leg's start to its stop.</summary>
        public IReadOnlyList<MapXY> Path { get; set; }
    }

    /// <summary>A trip ready to show: the start, the stops in visiting order and the legs between them.</summary>
    public sealed class TripPlan
    {
        public string StartName { get; set; }
        public MapXY StartPoint { get; set; }
        public IReadOnlyList<Poi> Stops { get; set; }
        public IReadOnlyList<TripLeg> Legs { get; set; }
        public double TotalMeters { get; set; }
        public double TotalSeconds { get; set; }

        /// <summary>How much time the order saves against the order as listed (0 when unknown).</summary>
        public double SavedSeconds { get; set; }

        /// <summary>True when the figures are estimates (straight lines, no road data from the server).</summary>
        public bool IsEstimate { get; set; }
    }

    /// <summary>Builds <see cref="TripPlan"/>s: from the server's road route, or an estimate when the server is not available.</summary>
    public static class TripPlanner
    {
        private const double JoinToleranceMeters = 3;

        /// <summary>The server's road route for the stops (in the order it chose), with names from the places.</summary>
        public static TripPlan FromOnline(OnlineRoute route, string startName, MapXY startPoint, IEnumerable<Poi> places, OnlineRoute asListed)
        {
            Dictionary<string, Poi> byId = places.GroupBy(p => p.Id).ToDictionary(g => g.Key, g => g.First());
            var stops = new List<Poi>();
            var legs = new List<TripLeg>();
            string previousName = startName;
            MapXY previousPoint = startPoint;
            foreach (OnlineLeg leg in route.Legs)
            {
                Poi to;
                if (!byId.TryGetValue(leg.ToId, out to)) continue;
                stops.Add(to);
                var end = new MapXY(to.X, to.Y);
                legs.Add(new TripLeg
                {
                    FromName = previousName,
                    ToName = to.Name,
                    Meters = leg.Meters,
                    Seconds = leg.Seconds,
                    Path = Joined(previousPoint, leg.Path, end),
                });
                previousName = to.Name;
                previousPoint = end;
            }
            return new TripPlan
            {
                StartName = startName,
                StartPoint = startPoint,
                Stops = stops,
                Legs = legs,
                TotalMeters = route.TotalMeters,
                TotalSeconds = route.TotalSeconds,
                SavedSeconds = asListed == null ? 0 : Math.Max(0, asListed.TotalSeconds - route.TotalSeconds),
                IsEstimate = false,
            };
        }

        /// <summary>
        /// An estimate without the server: straight lines scaled by the road detour factor at the average city speed.
        /// With <paramref name="optimize"/> the stops are put in the shortest order, otherwise kept as listed.
        /// </summary>
        public static TripPlan Estimate(string startName, double startLongitude, double startLatitude, MapXY startPoint, IReadOnlyList<Poi> stops, bool optimize)
        {
            var geo = stops.Select(p => new GeoPoint(p.Longitude, p.Latitude)).ToList();
            var start = new GeoPoint(startLongitude, startLatitude);
            int[] listed = Enumerable.Range(0, stops.Count).ToArray();
            int[] order = optimize ? RoutePlanner.Plan(start, geo).Order.ToArray() : listed;

            double listedKm = LengthKm(start, geo, listed);
            double orderKm = LengthKm(start, geo, order);
            var ordered = order.Select(i => stops[i]).ToList();
            var legs = new List<TripLeg>();
            string previousName = startName;
            MapXY previousPoint = startPoint;
            GeoPoint previousGeo = start;
            foreach (int i in order)
            {
                double km = GeoMath.Between(previousGeo, geo[i]) * RoutePlanner.RoadDetourFactor;
                var end = new MapXY(stops[i].X, stops[i].Y);
                legs.Add(new TripLeg
                {
                    FromName = previousName,
                    ToName = stops[i].Name,
                    Meters = km * 1000,
                    Seconds = km / RoutePlanner.AverageSpeedKmh * 3600,
                    Path = new[] { previousPoint, end },
                });
                previousName = stops[i].Name;
                previousPoint = end;
                previousGeo = geo[i];
            }
            return new TripPlan
            {
                StartName = startName,
                StartPoint = startPoint,
                Stops = ordered,
                Legs = legs,
                TotalMeters = orderKm * 1000,
                TotalSeconds = orderKm / RoutePlanner.AverageSpeedKmh * 3600,
                SavedSeconds = Math.Max(0, (listedKm - orderKm) / RoutePlanner.AverageSpeedKmh * 3600),
                IsEstimate = true,
            };
        }

        private static double LengthKm(GeoPoint start, IReadOnlyList<GeoPoint> stops, int[] order)
        {
            double km = 0;
            GeoPoint previous = start;
            foreach (int i in order)
            {
                km += GeoMath.Between(previous, stops[i]) * RoutePlanner.RoadDetourFactor;
                previous = stops[i];
            }
            return km;
        }

        // The road line with the leg's own end points added, so the line reaches the markers.
        private static IReadOnlyList<MapXY> Joined(MapXY from, IReadOnlyList<MapXY> road, MapXY to)
        {
            var points = new List<MapXY>();
            if (road == null || road.Count == 0)
            {
                points.Add(from);
                points.Add(to);
                return points;
            }
            if (Distance(from, road[0]) > JoinToleranceMeters) points.Add(from);
            points.AddRange(road);
            if (Distance(road[road.Count - 1], to) > JoinToleranceMeters) points.Add(to);
            return points;
        }

        private static double Distance(MapXY a, MapXY b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
    }
}
