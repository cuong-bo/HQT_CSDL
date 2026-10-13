using System.Text.Json;

namespace Hue_travel.Api.Routing;

// The start is either a place (StartId) or a free point in VN-2000 / UTM 48N metres (StartX, StartY), e.g. one picked on the map.
public sealed record RouteRequest(string? StartId, string[]? StopIds, bool? Optimize, double? StartX = null, double? StartY = null);

// Geometry is GeoJSON in degrees; GeometryUtm is the same line in VN-2000 / UTM 48N metres (the map's own units).
public sealed record RouteLeg(string FromId, string ToId, double Meters, double Seconds, JsonElement? Geometry, JsonElement? GeometryUtm);

public sealed record RouteResponse(IReadOnlyList<string> Order, IReadOnlyList<RouteLeg> Legs, double TotalMeters, double TotalSeconds);
