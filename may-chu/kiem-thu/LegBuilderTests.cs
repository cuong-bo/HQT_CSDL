using System.Text.Json;
using Hue_travel.Api.Routing;

namespace Hue_travel.Api.Tests;

public class LegBuilderTests
{
    private static PathPiece Piece(double seconds, params (double X, double Y)[] points) =>
        new(points.Zip(points.Skip(1), (a, b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y))).Sum(), seconds, points, points);

    [Fact]
    public void Build_GivesTheArrivalTimeAtEveryPoint_SoSlowRoadsTakeLonger()
    {
        // 100 m in 10 s (fast road), then 100 m in 50 s (slow road); the second piece starts where the first ends.
        RouteLeg leg = LegBuilder.Build("a", "b", [Piece(10, (0, 0), (100, 0)), Piece(50, (100, 0), (200, 0))]);

        Assert.Equal(new[] { 0.0, 10.0, 60.0 }, leg.Times);
        Assert.Equal(60, leg.Seconds);
        Assert.Equal(200, leg.Meters);
        Assert.Equal(3, leg.GeometryUtm!.Value.GetProperty("coordinates").GetArrayLength());
    }

    [Fact]
    public void Build_SpreadsAPiecesTimeOverItsPointsByDistance()
    {
        RouteLeg leg = LegBuilder.Build("a", "b", [Piece(20, (0, 0), (25, 0), (100, 0))]);

        Assert.Equal(new[] { 0.0, 5.0, 20.0 }, leg.Times);
    }

    [Fact]
    public void Build_KeepsTimesAndGeometryTheSameLength_WhenPointsRepeat()
    {
        RouteLeg leg = LegBuilder.Build("a", "b", [Piece(5, (0, 0), (0, 0), (10, 0)), Piece(5, (10, 0), (20, 0))]);

        Assert.Equal(leg.Times.Count, leg.Geometry!.Value.GetProperty("coordinates").GetArrayLength());
        Assert.Equal(leg.Times.Count, leg.GeometryUtm!.Value.GetProperty("coordinates").GetArrayLength());
        Assert.Equal(3, leg.Times.Count);
    }

    [Fact]
    public void Build_HasNoLine_WhenThereIsNoPath()
    {
        RouteLeg leg = LegBuilder.Build("a", "b", []);

        Assert.Null(leg.Geometry);
        Assert.Empty(leg.Times);
    }

    [Fact]
    public void Coordinates_ReadsPointsFromAnyLineKind()
    {
        using JsonDocument line = JsonDocument.Parse("{\"type\":\"LineString\",\"coordinates\":[[1,2],[3,4]]}");
        using JsonDocument point = JsonDocument.Parse("{\"type\":\"Point\",\"coordinates\":[5,6]}");
        using JsonDocument many = JsonDocument.Parse("{\"type\":\"MultiLineString\",\"coordinates\":[[[1,2],[3,4]],[[3,4],[5,6]]]}");

        Assert.Equal(2, LegBuilder.Coordinates(line.RootElement).Count);
        Assert.Equal((5.0, 6.0), LegBuilder.Coordinates(point.RootElement)[0]);
        Assert.Equal(4, LegBuilder.Coordinates(many.RootElement).Count);
    }
}
