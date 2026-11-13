using System;
using System.Collections.Generic;
using System.Linq;
using Hue_travel.Models;
using Hue_travel.Services.Hours;
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

        /// <summary>Seconds elapsed on arrival at each point of <see cref="Path"/> (same count), so the car can run at the real speed of each road; null when unknown.</summary>
        public IReadOnlyList<double> Times { get; set; }

        /// <summary>When the visitor is expected at the stop, in Vietnam time; null when no departure time was given.</summary>
        public DateTime? ArriveAt { get; set; }

        /// <summary>When the visit is expected to end (arrival plus the stay); null when no departure time was given.</summary>
        public DateTime? LeaveAt { get; set; }

        /// <summary>How long the visitor plans to stay at the stop, in minutes.</summary>
        public int StayMinutes { get; set; }

        /// <summary>The state of the stop's opening schedule at the arrival time (Unknown when no time was given).</summary>
        public OpenState OpenState { get; set; } = OpenState.Unknown;

        /// <summary>True when the visit, starting at <see cref="ArriveAt"/>, cannot finish inside one opening range.</summary>
        public bool HoursWarning { get; set; }
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

        /// <summary>When the trip starts, in Vietnam time; null for a plan that only shows travel time.</summary>
        public DateTime? DepartAt { get; set; }

        /// <summary>Total time spent at the stops, in minutes.</summary>
        public int TotalVisitMinutes { get; set; }

        /// <summary>How much time the order saves against the order as listed (0 when unknown).</summary>
        public double SavedSeconds { get; set; }

        /// <summary>Always false now: every route comes from the server along the real roads.</summary>
        public bool IsEstimate { get; set; }
    }

    /// <summary>Builds <see cref="TripPlan"/>s from the server's road route.</summary>
    public static class TripPlanner
    {
        private const double JoinToleranceMeters = 3;

        /// <summary>The server's road route for the stops (in the order it chose), with names from the places.</summary>
        public static TripPlan FromOnline(OnlineRoute route, string startName, MapXY startPoint, IEnumerable<Poi> places, OnlineRoute asListed)
        {
            return FromOnline(route, startName, startPoint, places, asListed, null, null);
        }

        /// <summary>
        /// As above, with a departure time (Vietnam clock) and how long the visitor stays at each place in <paramref name="places"/>.
        /// Every stop then gets its arrival and leaving time and whether its opening hours fit the visit.
        /// </summary>
        public static TripPlan FromOnline(OnlineRoute route, string startName, MapXY startPoint, IEnumerable<Poi> places, OnlineRoute asListed,
            DateTime? departAt, IReadOnlyList<int> stayMinutes)
        {
            List<Poi> placeList = (places ?? Enumerable.Empty<Poi>()).ToList();
            Dictionary<string, Poi> byId = placeList.GroupBy(p => p.Id).ToDictionary(g => g.Key, g => g.First());
            var stays = new Dictionary<string, int>();
            for (int i = 0; i < placeList.Count; i++)
            {
                Poi place = placeList[i];
                int stayed = stayMinutes != null && i < stayMinutes.Count ? stayMinutes[i] : DefaultStayMinutes(place.Category);
                stays[place.Id] = stayed;
            }

            var stops = new List<Poi>();
            var legs = new List<TripLeg>();
            string previousName = startName;
            MapXY previousPoint = startPoint;
            DateTime? clock = departAt;
            foreach (OnlineLeg leg in route.Legs)
            {
                Poi to;
                if (!byId.TryGetValue(leg.ToId, out to)) continue;
                stops.Add(to);
                var end = new MapXY(to.X, to.Y);
                IReadOnlyList<MapXY> path;
                IReadOnlyList<double> times;
                Joined(previousPoint, leg, end, out path, out times);

                int stayed = stays.ContainsKey(to.Id) ? stays[to.Id] : DefaultStayMinutes(to.Category);
                DateTime? arrive = null, leave = null;
                OpenState state = OpenState.Unknown;
                bool warning = false;
                if (clock.HasValue)
                {
                    clock = clock.Value.AddSeconds(leg.Seconds);
                    arrive = clock;
                    leave = arrive.Value.AddMinutes(stayed);
                    clock = leave;
                    OpenStatus status = to.Schedule.StatusAt(arrive.Value);
                    state = status.State;
                    warning = !to.Schedule.IsOpenForVisit(arrive.Value, TimeSpan.FromMinutes(stayed));
                }

                legs.Add(new TripLeg
                {
                    FromName = previousName,
                    ToName = to.Name,
                    Meters = leg.Meters,
                    Seconds = leg.Seconds,
                    Path = path,
                    Times = times,
                    ArriveAt = arrive,
                    LeaveAt = leave,
                    StayMinutes = stayed,
                    OpenState = state,
                    HoursWarning = warning,
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
                DepartAt = departAt,
                TotalVisitMinutes = legs.Sum(l => l.StayMinutes),
                SavedSeconds = asListed == null ? 0 : Math.Max(0, asListed.TotalSeconds - route.TotalSeconds),
                IsEstimate = false,
            };
        }

        private static int DefaultStayMinutes(PoiCategory category)
        {
            switch (category)
            {
                case PoiCategory.Heritage: return 60;
                case PoiCategory.Scenery: return 30;
                case PoiCategory.Food: return 45;
                case PoiCategory.Service: return 30;
                default: return 0;
            }
        }

        // The road line with the leg's own end points added, so the line reaches the markers; the times follow the same points.
        private static void Joined(MapXY from, OnlineLeg leg, MapXY to, out IReadOnlyList<MapXY> path, out IReadOnlyList<double> times)
        {
            IReadOnlyList<MapXY> road = leg.Path;
            var points = new List<MapXY>();
            if (road == null || road.Count == 0)
            {
                points.Add(from);
                points.Add(to);
                path = points;
                times = new[] { 0.0, leg.Seconds };
                return;
            }
            bool aligned = leg.Times != null && leg.Times.Count == road.Count;
            var elapsed = new List<double>();
            if (Distance(from, road[0]) > JoinToleranceMeters)
            {
                points.Add(from);
                elapsed.Add(0);
            }
            points.AddRange(road);
            if (aligned) elapsed.AddRange(leg.Times);
            if (Distance(road[road.Count - 1], to) > JoinToleranceMeters)
            {
                points.Add(to);
                if (aligned) elapsed.Add(leg.Times[leg.Times.Count - 1]);
            }
            path = points;
            times = aligned ? elapsed : null;
        }

        private static double Distance(MapXY a, MapXY b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
    }
}
