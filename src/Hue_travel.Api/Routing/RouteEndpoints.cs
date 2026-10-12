namespace Hue_travel.Api.Routing;

public static class RouteEndpoints
{
    public static void MapRouteEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/route", PlanAsync);
    }

    /// <summary>Visits the stops from the start: in the cheapest order by default, or in the order given when optimize is false.</summary>
    private static async Task<IResult> PlanAsync(RouteRequest request, RouteRepository routes, CancellationToken ct)
    {
        string? start = request.StartId?.Trim();
        string[] stops = (request.StopIds ?? []).Select(s => s.Trim()).ToArray();
        if (string.IsNullOrEmpty(start))
            return Results.Problem(detail: "Thiếu startId.", statusCode: StatusCodes.Status400BadRequest);
        if (stops.Length is < 1 or > RouteOptimizer.MaxStops)
            return Results.Problem(detail: $"stopIds cần từ 1 đến {RouteOptimizer.MaxStops} địa điểm.", statusCode: StatusCodes.Status400BadRequest);
        if (stops.Distinct().Count() != stops.Length || stops.Contains(start))
            return Results.Problem(detail: "Các địa điểm phải khác nhau và khác điểm xuất phát.", statusCode: StatusCodes.Status400BadRequest);

        string[] all = [start, .. stops];
        HashSet<string> routable = await routes.RoutableIdsAsync(all, ct);
        string[] missing = all.Where(id => !routable.Contains(id)).ToArray();
        if (missing.Length > 0)
            return Results.Problem(detail: "Không tìm thấy địa điểm: " + string.Join(", ", missing), statusCode: StatusCodes.Status404NotFound);

        string[] order = stops;
        if (request.Optimize ?? true)
        {
            double[,] matrix = await routes.TimeMatrixAsync(all, ct);
            IReadOnlyList<int>? best = RouteOptimizer.Order(matrix);
            if (best == null)
                return Results.Problem(detail: "Có địa điểm không nối được bằng đường bộ với các điểm còn lại.", statusCode: StatusCodes.Status422UnprocessableEntity);
            order = best.Select(i => all[i]).ToArray();
        }

        var legs = new List<RouteLeg>(order.Length);
        string previous = start;
        foreach (string next in order)
        {
            legs.Add(await routes.LegAsync(previous, next, ct));
            previous = next;
        }
        await routes.CountRouteAddsAsync(stops, ct);
        return Results.Ok(new RouteResponse(order, legs, legs.Sum(l => l.Meters), legs.Sum(l => l.Seconds)));
    }
}
