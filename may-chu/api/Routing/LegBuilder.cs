using System.Text.Json;

namespace Hue_travel.Api.Routing;

/// <summary>One road piece of a leg: its length and time, and its line in VN-2000 / UTM 48N metres and in degrees (same points, same order).</summary>
public sealed record PathPiece(double Meters, double Seconds, IReadOnlyList<(double X, double Y)> Utm, IReadOnlyList<(double X, double Y)> Degrees);

/// <summary>
/// Joins the pieces of a path into one leg. Besides the line it gives <see cref="RouteLeg.Times"/>: the seconds elapsed on
/// arrival at each point of the line, so the app can move a car along it at the real, changing speed of each road.
/// </summary>
public static class LegBuilder
{
    public static RouteLeg Build(string fromId, string toId, IReadOnlyList<PathPiece> pieces)
    {
        var utm = new List<(double X, double Y)>();
        var degrees = new List<(double X, double Y)>();
        var times = new List<double>();
        double elapsed = 0, meters = 0;

        foreach (PathPiece piece in pieces)
        {
            double length = Length(piece.Utm);
            double walked = 0;
            for (int i = 0; i < piece.Utm.Count; i++)
            {
                if (i > 0) walked += Distance(piece.Utm[i - 1], piece.Utm[i]);
                var point = piece.Utm[i];
                if (utm.Count > 0 && utm[^1] == point) continue;   // the join between two pieces, or a repeated point
                utm.Add(point);
                degrees.Add(piece.Degrees[Math.Min(i, piece.Degrees.Count - 1)]);
                times.Add(Math.Round(elapsed + (length > 0 ? piece.Seconds * walked / length : 0), 1));
            }
            elapsed += piece.Seconds;
            meters += piece.Meters;
        }

        bool drawable = utm.Count >= 2;
        return new RouteLeg(fromId, toId, meters, elapsed,
            drawable ? LineString(degrees) : null,
            drawable ? LineString(utm) : null,
            drawable ? times : []);
    }

    /// <summary>The points of a GeoJSON LineString, MultiLineString or Point (a one-point piece).</summary>
    public static IReadOnlyList<(double X, double Y)> Coordinates(JsonElement geometry)
    {
        var points = new List<(double X, double Y)>();
        JsonElement coordinates = geometry.GetProperty("coordinates");
        switch (geometry.GetProperty("type").GetString())
        {
            case "Point": points.Add(Read(coordinates)); break;
            case "LineString": foreach (JsonElement c in coordinates.EnumerateArray()) points.Add(Read(c)); break;
            case "MultiLineString":
                foreach (JsonElement line in coordinates.EnumerateArray())
                    foreach (JsonElement c in line.EnumerateArray()) points.Add(Read(c));
                break;
        }
        return points;
    }

    private static (double X, double Y) Read(JsonElement c) => (c[0].GetDouble(), c[1].GetDouble());

    private static JsonElement LineString(List<(double X, double Y)> points) =>
        JsonSerializer.SerializeToElement(new { type = "LineString", coordinates = points.Select(p => new[] { p.X, p.Y }) });

    private static double Length(IReadOnlyList<(double X, double Y)> points)
    {
        double sum = 0;
        for (int i = 1; i < points.Count; i++) sum += Distance(points[i - 1], points[i]);
        return sum;
    }

    private static double Distance((double X, double Y) a, (double X, double Y) b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
}
