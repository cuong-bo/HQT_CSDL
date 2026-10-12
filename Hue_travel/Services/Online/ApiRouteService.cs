using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hue_travel.Services.Online
{
    /// <summary>A point in the map's own units: metres in VN-2000 / UTM zone 48N.</summary>
    public struct MapXY
    {
        public MapXY(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double X { get; }

        public double Y { get; }
    }

    public sealed class OnlineLeg
    {
        public string FromId { get; set; }
        public string ToId { get; set; }
        public double Meters { get; set; }
        public double Seconds { get; set; }
        public IReadOnlyList<MapXY> Path { get; set; }
    }

    /// <summary>A route computed by the server along the real roads.</summary>
    public sealed class OnlineRoute
    {
        public IReadOnlyList<string> Order { get; set; }
        public IReadOnlyList<OnlineLeg> Legs { get; set; }
        public double TotalMeters { get; set; }
        public double TotalSeconds { get; set; }
    }

    public sealed class ApiResult<T>
    {
        public T Value { get; set; }
        public string Error { get; set; }
        public bool Ok => Error == null;
    }

    public static class RouteJson
    {
        public static OnlineRoute Parse(IDictionary<string, object> d)
        {
            IList<object> legs = JsonHelper.List(d, "legs");
            IList<object> order = JsonHelper.List(d, "order");
            if (legs == null || order == null) return null;
            return new OnlineRoute
            {
                Order = order.Select(o => Convert.ToString(o)).ToList(),
                Legs = legs.OfType<IDictionary<string, object>>().Select(ParseLeg).ToList(),
                TotalMeters = JsonHelper.Dbl(d, "totalMeters"),
                TotalSeconds = JsonHelper.Dbl(d, "totalSeconds"),
            };
        }

        private static OnlineLeg ParseLeg(IDictionary<string, object> d) => new OnlineLeg
        {
            FromId = JsonHelper.Str(d, "fromId"),
            ToId = JsonHelper.Str(d, "toId"),
            Meters = JsonHelper.Dbl(d, "meters"),
            Seconds = JsonHelper.Dbl(d, "seconds"),
            Path = ParsePath(JsonHelper.Obj(d, "geometryUtm")),
        };

        /// <summary>The points of a GeoJSON LineString (or the parts of a MultiLineString joined end to end).</summary>
        public static IReadOnlyList<MapXY> ParsePath(IDictionary<string, object> geometry)
        {
            var points = new List<MapXY>();
            IList<object> coordinates = JsonHelper.List(geometry, "coordinates");
            if (coordinates == null) return points;
            bool multi = JsonHelper.Str(geometry, "type") == "MultiLineString";
            foreach (object part in multi ? coordinates : new List<object> { coordinates })
            {
                IList<object> line = JsonHelper.AsList(part);
                if (line == null) continue;
                foreach (object pair in line)
                {
                    IList<object> xy = JsonHelper.AsList(pair);
                    if (xy != null && xy.Count >= 2) points.Add(new MapXY(Convert.ToDouble(xy[0]), Convert.ToDouble(xy[1])));
                }
            }
            return points;
        }
    }

    public sealed class ApiRouteService
    {
        private readonly ApiClient api;

        public ApiRouteService(ApiClient api)
        {
            this.api = api;
        }

        public async Task<ApiResult<OnlineRoute>> PlanAsync(string startId, IList<string> stopIds, bool optimize, CancellationToken ct)
        {
            var body = new Dictionary<string, object> { { "startId", startId }, { "stopIds", stopIds.ToArray() }, { "optimize", optimize } };
            ApiResponse r = await api.PostAsync("/v1/route", body, ct).ConfigureAwait(true);
            if (!r.IsSuccess) return new ApiResult<OnlineRoute> { Error = r.Error };
            OnlineRoute route = RouteJson.Parse(r.Object);
            return route == null ? new ApiResult<OnlineRoute> { Error = "Máy chủ trả về tuyến đường không hợp lệ." } : new ApiResult<OnlineRoute> { Value = route };
        }
    }
}
