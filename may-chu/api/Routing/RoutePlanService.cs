namespace Hue_travel.Api.Routing;

public sealed record RoutePlanOutcome(RouteResponse? Response, int ErrorStatus = 0, string? ErrorDetail = null);

/// <summary>
/// The route planner behind both the HTTP endpoint and the chat tool. It validates the request, orders the stops (including
/// opening hours when a departure time is given), and builds the legs. The endpoint only turns an outcome into a result.
/// </summary>
public sealed class RoutePlanService(RouteRepository routes)
{
    // A free start further than this from the connected road network is refused.
    private const double MaxStartSnapMeters = 1500;

    public const string StartLabel = "start";

    public async Task<RoutePlanOutcome> PlanAsync(RouteRequest request, bool countRouteAdds, CancellationToken ct)
    {
        string? start = request.StartId?.Trim();
        string[] stops = (request.StopIds ?? []).Select(s => s.Trim()).ToArray();
        bool freeStart = string.IsNullOrEmpty(start) && request.StartX is double && request.StartY is double;
        if (string.IsNullOrEmpty(start) && !freeStart)
            return Bad(StatusCodes.Status400BadRequest, "Thiếu điểm xuất phát (startId hoặc startX/startY).");
        if (stops.Length is < 1 or > RouteOptimizer.MaxStops)
            return Bad(StatusCodes.Status400BadRequest, $"stopIds cần từ 1 đến {RouteOptimizer.MaxStops} địa điểm.");
        if (stops.Distinct().Count() != stops.Length || (start != null && stops.Contains(start)))
            return Bad(StatusCodes.Status400BadRequest, "Các địa điểm phải khác nhau và khác điểm xuất phát.");

        double startX = 0, startY = 0;
        if (freeStart)
        {
            double x = request.StartX!.Value, y = request.StartY!.Value;
            if (!double.IsFinite(x) || !double.IsFinite(y))
                return Bad(StatusCodes.Status400BadRequest, "Tọa độ điểm xuất phát không hợp lệ.");
            double? away = await routes.DistanceToRoadAsync(x, y, ct);
            if (away == null || away.Value > MaxStartSnapMeters)
                return Bad(StatusCodes.Status422UnprocessableEntity, "Điểm xuất phát quá xa đường đi; hãy chọn gần đường hơn.");
            startX = x;
            startY = y;
        }

        string[] all = freeStart ? stops : [start!, .. stops];
        HashSet<string> routable = await routes.RoutableIdsAsync(all, ct);
        string[] missing = all.Where(id => !routable.Contains(id)).ToArray();
        if (missing.Length > 0)
            return Bad(StatusCodes.Status404NotFound, "Không tìm thấy địa điểm: " + string.Join(", ", missing));

        int[] stays = request.StayMinutes ?? new int[stops.Length];
        if (stays.Length != stops.Length)
            return Bad(StatusCodes.Status400BadRequest, "stayMinutes phải có đúng một giá trị cho mỗi điểm đến.");
        if (stays.Any(minutes => minutes is < 0 or > 24 * 60))
            return Bad(StatusCodes.Status400BadRequest, "stayMinutes phải từ 0 đến 1440 phút.");

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

            IReadOnlyList<int>? best;
            if (request.DepartAt is { } departAt)
            {
                // An order that cannot visit a stop before it closes is not useful; the app still shows warnings for the
                // order it was given, so a distance-only fallback is better than refusing the whole trip.
                Dictionary<string, PoiOpeningHours> info = await routes.OpeningHoursAsync(stops, ct);
                var schedules = stops.Select(id =>
                {
                    return info.TryGetValue(id, out PoiOpeningHours? found) && found != null
                        ? OpeningHours.Parse(found.Text, found.Group)
                        : OpeningHours.Parse(null, null);
                }).ToList();
                best = RouteOptimizer.Order(matrix, departAt, schedules, stays);
            }
            else
            {
                best = null;
            }
            best ??= RouteOptimizer.Order(matrix);
            if (best == null)
                return Bad(StatusCodes.Status422UnprocessableEntity, "Có địa điểm không nối được bằng đường bộ với các điểm còn lại.");
            order = freeStart ? best.Select(i => stops[i - 1]).ToArray() : best.Select(i => all[i]).ToArray();
        }

        var legs = new List<RouteLeg>(order.Length);
        string previous = start ?? StartLabel;
        foreach (string next in order)
        {
            legs.Add(freeStart && legs.Count == 0
                ? await routes.LegFromPointAsync(startX, startY, StartLabel, next, ct)
                : await routes.LegAsync(previous, next, ct));
            previous = next;
        }

        // Counted for signed-in use of a real plan only; the app also asks for the order as listed, just to show the saving.
        if (countRouteAdds && (request.Optimize ?? true)) await routes.CountRouteAddsAsync(stops, ct);

        return new RoutePlanOutcome(new RouteResponse(order, legs, legs.Sum(l => l.Meters), legs.Sum(l => l.Seconds)));
    }

    private static RoutePlanOutcome Bad(int status, string detail) => new(null, status, detail);
}
