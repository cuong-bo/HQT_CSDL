namespace Hue_travel.Api.Routing;

/// <summary>The answer of a plan: the route, or the HTTP status and the sentence for the person when it cannot be made.</summary>
public sealed record RoutePlanOutcome(RouteResponse? Response, int ErrorStatus = 0, string? ErrorDetail = null)
{
    public bool Ok => Response != null;

    public static RoutePlanOutcome Fail(int status, string detail) => new(null, status, detail);
}

/// <summary>
/// Visits the stops from the start: in the cheapest order by default, or in the order given when optimize is false. This is the body of
/// POST /v1/route, moved here unchanged so that the assistant's route tool runs exactly the same planning (Held-Karp over the road times).
/// </summary>
public sealed class RoutePlanService(RouteRepository routes)
{
    // A free start further than this from the connected road network is refused.
    private const double MaxStartSnapMeters = 1500;

    /// <param name="countRouteAdds">True for a signed-in person's real plan: the stops' "added to a route" counters go up.</param>
    public async Task<RoutePlanOutcome> PlanAsync(RouteRequest request, bool countRouteAdds, CancellationToken ct)
    {
        string? start = request.StartId?.Trim();
        string[] stops = (request.StopIds ?? []).Select(s => s.Trim()).ToArray();
        bool freeStart = string.IsNullOrEmpty(start) && request.StartX is double && request.StartY is double;
        if (string.IsNullOrEmpty(start) && !freeStart)
            return RoutePlanOutcome.Fail(StatusCodes.Status400BadRequest, "Thiếu điểm xuất phát (startId hoặc startX/startY).");
        if (stops.Length is < 1 or > RouteOptimizer.MaxStops)
            return RoutePlanOutcome.Fail(StatusCodes.Status400BadRequest, $"stopIds cần từ 1 đến {RouteOptimizer.MaxStops} địa điểm.");
        if (stops.Distinct().Count() != stops.Length || (start != null && stops.Contains(start)))
            return RoutePlanOutcome.Fail(StatusCodes.Status400BadRequest, "Các địa điểm phải khác nhau và khác điểm xuất phát.");

        double startX = 0, startY = 0;
        if (freeStart)
        {
            double x = request.StartX!.Value, y = request.StartY!.Value;
            if (!double.IsFinite(x) || !double.IsFinite(y))
                return RoutePlanOutcome.Fail(StatusCodes.Status400BadRequest, "Tọa độ điểm xuất phát không hợp lệ.");
            double? away = await routes.DistanceToRoadAsync(x, y, ct);
            if (away == null || away.Value > MaxStartSnapMeters)
                return RoutePlanOutcome.Fail(StatusCodes.Status422UnprocessableEntity, "Điểm xuất phát quá xa đường đi; hãy chọn gần đường hơn.");
            startX = x;
            startY = y;
        }

        string[] all = freeStart ? stops : [start!, .. stops];
        HashSet<string> routable = await routes.RoutableIdsAsync(all, ct);
        string[] missing = all.Where(id => !routable.Contains(id)).ToArray();
        if (missing.Length > 0)
            return RoutePlanOutcome.Fail(StatusCodes.Status404NotFound, "Không tìm thấy địa điểm: " + string.Join(", ", missing));

        string[] order = stops;
        if (request.Optimize ?? true)
        {
            double[,] matrix;
            if (freeStart)
            {
                // Index 0 is the free start, 1..n the stops.
                double[,] among = await routes.TimeMatrixAsync(stops, ct);
                double[] fromStart = await routes.TimesFromPointAsync(startX, startY, stops, ct);
                matrix = new double[stops.Length + 1, stops.Length + 1];
                for (int i = 0; i < stops.Length; i++)
                {
                    matrix[0, i + 1] = fromStart[i];
                    matrix[i + 1, 0] = double.PositiveInfinity;
                    for (int j = 0; j < stops.Length; j++) matrix[i + 1, j + 1] = among[i, j];
                }
            }
            else
            {
                matrix = await routes.TimeMatrixAsync(all, ct);
            }
            IReadOnlyList<int>? best = RouteOptimizer.Order(matrix);
            if (best == null)
                return RoutePlanOutcome.Fail(StatusCodes.Status422UnprocessableEntity, "Có địa điểm không nối được bằng đường bộ với các điểm còn lại.");
            order = freeStart ? best.Select(i => stops[i - 1]).ToArray() : best.Select(i => all[i]).ToArray();
        }

        var legs = new List<RouteLeg>(order.Length);
        string previous = start ?? RouteEndpoints.StartLabel;
        foreach (string next in order)
        {
            legs.Add(freeStart && legs.Count == 0
                ? await routes.LegFromPointAsync(startX, startY, RouteEndpoints.StartLabel, next, ct)
                : await routes.LegAsync(previous, next, ct));
            previous = next;
        }
        // Counted for signed-in use of a real plan only; the app also asks for the order as listed, just to show the saving.
        if (countRouteAdds && (request.Optimize ?? true)) await routes.CountRouteAddsAsync(stops, ct);
        return new RoutePlanOutcome(new RouteResponse(order, legs, legs.Sum(l => l.Meters), legs.Sum(l => l.Seconds)));
    }
}
