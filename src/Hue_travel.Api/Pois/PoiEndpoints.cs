using System.Globalization;

namespace Hue_travel.Api.Pois;

public static class PoiEndpoints
{
    private const int MaxPageSize = 100;
    private const int MaxQueryLength = 100;
    private static readonly HashSet<string> Groups = ["heritage", "scenery", "food", "stay", "service", "transport"];

    public static void MapPoiEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/pois", SearchAsync);
        app.MapGet("/v1/pois/{id}", GetAsync);
    }

    private static async Task<IResult> SearchAsync(
        PoiRepository pois, CancellationToken ct,
        string[]? group = null, string? q = null, string? bbox = null, int page = 1, int pageSize = 50)
    {
        string[] groups = group ?? [];
        if (groups.Any(g => !Groups.Contains(g)))
            return Results.Problem(detail: "group phải là một trong: " + string.Join(", ", Groups), statusCode: StatusCodes.Status400BadRequest);
        if (page < 1 || pageSize < 1 || pageSize > MaxPageSize)
            return Results.Problem(detail: $"page từ 1, pageSize từ 1 đến {MaxPageSize}.", statusCode: StatusCodes.Status400BadRequest);
        if (q is { Length: > MaxQueryLength })
            return Results.Problem(detail: "Từ khóa quá dài.", statusCode: StatusCodes.Status400BadRequest);

        double[]? box = null;
        if (!string.IsNullOrWhiteSpace(bbox) && !TryParseBbox(bbox, out box))
            return Results.Problem(detail: "bbox có dạng minX,minY,maxX,maxY (mét, VN-2000 UTM 48N).", statusCode: StatusCodes.Status400BadRequest);

        string? text = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        return Results.Ok(await pois.SearchAsync(new PoiQuery(groups, text, box, page, pageSize), ct));
    }

    private static async Task<IResult> GetAsync(string id, PoiRepository pois, CancellationToken ct)
    {
        PoiDto? poi = await pois.FindAsync(id, ct);
        if (poi == null) return Results.Problem(detail: "Không tìm thấy địa điểm.", statusCode: StatusCodes.Status404NotFound);
        await pois.CountViewAsync(id, ct);
        return Results.Ok(poi);
    }

    private static bool TryParseBbox(string text, out double[]? box)
    {
        box = null;
        string[] parts = text.Split(',');
        if (parts.Length != 4) return false;
        var values = new double[4];
        for (int i = 0; i < 4; i++)
        {
            if (!double.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out values[i]) || !double.IsFinite(values[i]))
                return false;
        }
        if (values[0] >= values[2] || values[1] >= values[3]) return false;
        box = values;
        return true;
    }
}
