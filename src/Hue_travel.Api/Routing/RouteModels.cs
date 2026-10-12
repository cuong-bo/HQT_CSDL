using System.Text.Json;

namespace Hue_travel.Api.Routing;

public sealed record RouteRequest(string? StartId, string[]? StopIds, bool? Optimize);

public sealed record RouteLeg(string FromId, string ToId, double Meters, double Seconds, JsonElement? Geometry);

public sealed record RouteResponse(IReadOnlyList<string> Order, IReadOnlyList<RouteLeg> Legs, double TotalMeters, double TotalSeconds);
