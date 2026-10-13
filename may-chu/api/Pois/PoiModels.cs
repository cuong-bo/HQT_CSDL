namespace Hue_travel.Api.Pois;

public sealed record PoiDto(
    string Id, string Group, string? Type, string Name, string? Address, string? OpeningHours,
    string? PriceText, int? PriceFrom, int? PriceTo, string? Phone, string? Website, string? Summary, string? Source,
    double Lon, double Lat, double X, double Y);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, long Total, int Page, int PageSize);

public sealed record PoiQuery(string[] Groups, string? Text, double[]? Bbox, int Page, int PageSize);
