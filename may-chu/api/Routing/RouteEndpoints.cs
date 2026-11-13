using System.Security.Claims;

namespace Hue_travel.Api.Routing;

public static class RouteEndpoints
{
    public static void MapRouteEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/route", PlanAsync).RequireRateLimiting("heavy");
    }

    /// <summary>Visits the stops from the start: in the cheapest order by default, or in the order given when optimize is false.</summary>
    private static async Task<IResult> PlanAsync(RouteRequest request, RoutePlanService plans, ClaimsPrincipal user, CancellationToken ct)
    {
        RoutePlanOutcome outcome = await plans.PlanAsync(request, user.Identity?.IsAuthenticated == true, ct);
        return outcome.Response != null
            ? Results.Ok(outcome.Response)
            : Results.Problem(detail: outcome.ErrorDetail, statusCode: outcome.ErrorStatus);
    }
}
