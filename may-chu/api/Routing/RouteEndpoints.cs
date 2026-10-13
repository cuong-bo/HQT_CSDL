using System.Security.Claims;

namespace Hue_travel.Api.Routing;

public static class RouteEndpoints
{
    /// <summary>Id given to a free start (picked on the map) in the legs of the answer.</summary>
    public const string StartLabel = "start";

    // A free start further than this from the connected road network is refused.
    private const double MaxStartSnapMeters = 1500;

    public static void MapRouteEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/route", PlanAsync).RequireRateLimiting("heavy");
    }

    /// <summary>Visits the stops from the start: in the cheapest order by default, or in the order given when optimize is false.</summary>
    private static async Task<IResult> PlanAsync(RouteRequest request, RouteRepository routes, ClaimsPrincipal user, CancellationToken ct)
    {
        string? start = request.StartId?.Trim();
        string[] stops = (request.StopIds ?? []).Select(s => s.Trim()).ToArray();
        bool freeStart = string.IsNullOrEmpty(start) && request.StartX is double && request.StartY is double;
        if (string.IsNullOrEmpty(start) && !freeStart)
            return Results.Problem(detail: "Thiếu điểm xuất phát (startId hoặc startX/startY).", statusCode: StatusCodes.Status400BadRequest);
        if (stops.Length is < 1 or > RouteOptimizer.MaxStops)
            return Results.Problem(detail: $"stopIds cần từ 1 đến {RouteOptimizer.MaxStops} địa điểm.", statusCode: StatusCodes.Status400BadRequest);
        if (stops.Distinct().Count() != stops.Length || (start != null && stops.Contains(start)))
            return Results.Problem(detail: "Các địa điểm phải khác nhau và khác điểm xuất phát.", statusCode: StatusCodes.Status400BadRequest);

        double startX = 0, startY = 0;
        if (freeStart)
        {
            double x = request.StartX!.Value, y = request.StartY!.Value;
            if (!double.IsFinite(x) || !double.IsFinite(y))
                return Results.Problem(detail: "Tọa độ điểm xuất phát không hợp lệ.", statusCode: StatusCodes.Status400BadRequest);
            double? away = await routes.DistanceToRoadAsync(x, y, ct);
            if (away == null || away.Value > MaxStartSnapMeters)
                return Results.Problem(detail: "Điểm xuất phát quá xa đường đi; hãy chọn gần đường hơn.", statusCode: StatusCodes.Status422UnprocessableEntity);
            startX = x;
            startY = y;
        }

        string[] all = freeStart ? stops : [start!, .. stops];
        HashSet<string> routable = await routes.RoutableIdsAsync(all, ct);
        string[] missing = all.Where(id => !routable.Contains(id)).ToArray();
        if (missing.Length > 0)
            return Results.Problem(detail: "Không tìm thấy địa điểm: " + string.Join(", ", missing), statusCode: StatusCodes.Status404NotFound);

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
                return Results.Problem(detail: "Có địa điểm không nối được bằng đường bộ với các điểm còn lại.", statusCode: StatusCodes.Status422UnprocessableEntity);
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
        if (user.Identity?.IsAuthenticated == true && (request.Optimize ?? true)) await routes.CountRouteAddsAsync(stops, ct);
        return Results.Ok(new RouteResponse(order, legs, legs.Sum(l => l.Meters), legs.Sum(l => l.Seconds)));
    }
}
