using System.Globalization;
using System.Text.Json;
using Hue_travel.Api.Pois;
using Hue_travel.Api.Routing;
using Hue_travel.Api.Weather;

namespace Hue_travel.Api.Chat;

/// <summary>
/// Read-only tools the model may call. Every fact in a reply comes from one of these; the model never queries the database or
/// calculates a road route itself. <c>seen</c> is the whitelist of place ids used when the reply is cleaned.
/// </summary>
public sealed class ChatTools(PoiRepository pois, RoutePlanService routes, WeatherService weather)
{
    private static readonly HashSet<string> Groups = ["heritage", "scenery", "food", "stay", "service", "transport"];

    public IReadOnlyList<ChatToolDefinition> Definitions { get; } =
    [
        new("search_places", "Tìm địa điểm ở Huế theo tên (không cần dấu) và/hoặc nhóm. Trả tối đa 8 địa điểm gọn.",
            Schema("""
            {"type":"object","properties":{"query":{"type":"string","description":"Tên hoặc từ khóa, có dấu hay không dấu đều được"},"group":{"type":"string","enum":["heritage","scenery","food","stay","service","transport"]},"limit":{"type":"integer","minimum":1,"maximum":8}}}
            """)),
        new("get_place", "Lấy thông tin đầy đủ (địa chỉ, giờ mở cửa, giá, điện thoại, website, mô tả) của một địa điểm theo mã.",
            Schema("""
            {"type":"object","properties":{"id":{"type":"string","description":"Mã địa điểm, ví dụ DL021"}},"required":["id"]}
            """)),
        new("plan_route", "Tính lộ trình đường bộ thật qua các điểm đến, có thể tự sắp thứ tự. Dùng khi người dùng hỏi thời gian, khoảng cách hoặc thứ tự đi.",
            Schema("""
            {"type":"object","properties":{"startId":{"type":"string","description":"Mã điểm bắt đầu"},"stopIds":{"type":"array","items":{"type":"string"},"minItems":1,"maxItems":12},"optimize":{"type":"boolean","description":"Tự sắp thứ tự tối ưu, mặc định true"}},"required":["startId","stopIds"]}
            """)),
        new("get_weather", "Thời tiết hiện tại ở Huế (nhiệt độ, mưa, gió, UV).", Schema("""{"type":"object","properties":{}}""")),
        new("list_tours", "Các tour gợi ý có sẵn trong ứng dụng: tên, thời lượng và các điểm dừng.", Schema("""{"type":"object","properties":{}}""")),
    ];

    public async Task<string> RunAsync(ChatToolCall call, Dictionary<string, ChatPlaceRef> seen,
        Action<ChatRouteInfo> onRoute, CancellationToken ct)
    {
        try
        {
            using JsonDocument arguments = JsonDocument.Parse(string.IsNullOrWhiteSpace(call.Arguments) ? "{}" : call.Arguments);
            JsonElement root = arguments.RootElement;
            switch (call.Name)
            {
                case "search_places": return await SearchPlacesAsync(root, seen, ct);
                case "get_place": return await GetPlaceAsync(root, seen, ct);
                case "plan_route": return await PlanRouteAsync(root, seen, onRoute, ct);
                case "get_weather": return await GetWeatherAsync(ct);
                case "list_tours": return ListTours(seen);
                default: return Error("Công cụ không tồn tại.");
            }
        }
        catch (JsonException)
        {
            return Error("Tham số công cụ không phải JSON hợp lệ.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Error("Không lấy được dữ liệu cho công cụ này.");
        }
    }

    private async Task<string> SearchPlacesAsync(JsonElement args, Dictionary<string, ChatPlaceRef> seen, CancellationToken ct)
    {
        string? query = Text(args, "query");
        string? group = Text(args, "group");
        if (group != null && !Groups.Contains(group)) return Error("Nhóm địa điểm không hợp lệ.");
        int limit = Number(args, "limit", 6);
        limit = Math.Clamp(limit, 1, 8);

        string[] groups = group == null ? [] : [group];
        PagedResult<PoiDto> page = await pois.SearchAsync(new PoiQuery(groups, query, null, 1, limit), ct);
        var items = new List<object>();
        foreach (PoiDto poi in page.Items)
        {
            seen[poi.Id] = new ChatPlaceRef(poi.Id, poi.Name, poi.Group);
            items.Add(Compact(poi));
        }
        return JsonSerializer.Serialize(new { places = items });
    }

    private async Task<string> GetPlaceAsync(JsonElement args, Dictionary<string, ChatPlaceRef> seen, CancellationToken ct)
    {
        string? id = Text(args, "id");
        if (string.IsNullOrWhiteSpace(id)) return Error("Thiếu mã địa điểm.");
        PoiDto? poi = await pois.FindAsync(id.Trim(), ct);
        if (poi == null) return Error("Không tìm thấy địa điểm.");
        seen[poi.Id] = new ChatPlaceRef(poi.Id, poi.Name, poi.Group);
        return JsonSerializer.Serialize(new
        {
            id = poi.Id, name = poi.Name, group = poi.Group, type = poi.Type, address = poi.Address,
            openingHours = poi.OpeningHours, priceText = poi.PriceText, phone = poi.Phone, website = poi.Website,
            summary = Truncate(poi.Summary, 400),
        });
    }

    private async Task<string> PlanRouteAsync(JsonElement args, Dictionary<string, ChatPlaceRef> seen,
        Action<ChatRouteInfo> onRoute, CancellationToken ct)
    {
        string? startId = Text(args, "startId");
        if (string.IsNullOrWhiteSpace(startId)) return Error("Thiếu startId.");
        if (!args.TryGetProperty("stopIds", out JsonElement stopValue) || stopValue.ValueKind != JsonValueKind.Array)
            return Error("Thiếu danh sách stopIds.");
        var stopIds = new List<string>();
        foreach (JsonElement item in stopValue.EnumerateArray())
        {
            string? id = item.ValueKind == JsonValueKind.String ? item.GetString() : null;
            if (!string.IsNullOrWhiteSpace(id)) stopIds.Add(id!.Trim());
        }
        if (stopIds.Count is < 1 or > RouteOptimizer.MaxStops) return Error($"Cần từ 1 đến {RouteOptimizer.MaxStops} điểm đến.");

        bool optimize = !args.TryGetProperty("optimize", out JsonElement opt) || opt.ValueKind != JsonValueKind.False;
        RoutePlanOutcome outcome = await routes.PlanAsync(
            new RouteRequest(startId.Trim(), stopIds.ToArray(), optimize), false, ct);
        if (outcome.Response == null) return Error(outcome.ErrorDetail ?? "Không tìm được đường đi.");

        string[] ids = outcome.Response.Order.Concat([startId.Trim()]).Distinct().ToArray();
        Dictionary<string, string> names = await pois.NamesAsync(ids, ct);
        var order = outcome.Response.Order.Select(id => new ChatPlaceRef(id, names.GetValueOrDefault(id, id), null)).ToList();
        foreach (ChatPlaceRef place in order) seen[place.Id] = place;
        string start = startId.Trim();
        if (names.TryGetValue(start, out string? startName)) seen[start] = new ChatPlaceRef(start, startName, null);

        var legs = outcome.Response.Legs.Select(leg => new
        {
            from = names.GetValueOrDefault(leg.FromId, leg.FromId),
            to = names.GetValueOrDefault(leg.ToId, leg.ToId),
            km = Math.Round(leg.Meters / 1000.0, 1),
            minutes = Math.Round(leg.Seconds / 60.0),
        });
        onRoute(new ChatRouteInfo(order, order, outcome.Response.TotalMeters, outcome.Response.TotalSeconds));
        return JsonSerializer.Serialize(new
        {
            order = order.Select(p => new { id = p.Id, name = p.Name }),
            legs,
            totalKm = Math.Round(outcome.Response.TotalMeters / 1000.0, 1),
            totalMinutes = Math.Round(outcome.Response.TotalSeconds / 60.0),
        });
    }

    private async Task<string> GetWeatherAsync(CancellationToken ct)
    {
        WeatherDto? current = await weather.GetAsync(ct);
        if (current == null) return Error("Chưa lấy được thời tiết.");
        return JsonSerializer.Serialize(new
        {
            temperature = current.Temperature,
            apparent = current.ApparentTemperature,
            condition = WeatherText.Describe(current.Code),
            isDay = current.IsDay,
            precipitationMm = current.Precipitation,
            windKmh = current.WindSpeed,
            uv = current.UvIndex,
            note = WeatherText.Advice(current),
        });
    }

    private static string ListTours(Dictionary<string, ChatPlaceRef> seen)
    {
        var tours = new List<object>();
        foreach (ChatTour tour in ChatKnowledge.Tours)
        {
            var stops = new List<object>();
            foreach (ChatTourStop stop in tour.Stops)
            {
                seen[stop.Id] = new ChatPlaceRef(stop.Id, stop.Name, null);
                stops.Add(new { id = stop.Id, name = stop.Name });
            }
            tours.Add(new { title = tour.Title, badge = tour.Badge, duration = tour.Duration, stops });
        }
        return JsonSerializer.Serialize(new { tours });
    }

    private static object Compact(PoiDto poi) => new
    {
        id = poi.Id, name = poi.Name, group = poi.Group, type = poi.Type, address = poi.Address,
        openingHours = poi.OpeningHours, priceText = poi.PriceText, summary = Truncate(poi.Summary, 160),
    };

    private static string Error(string message) => JsonSerializer.Serialize(new { error = message });

    private static JsonElement Schema(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private static string? Text(JsonElement args, string name) =>
        args.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? (value.GetString() ?? "").Trim() : null;

    private static int Number(JsonElement args, string name, int fallback) =>
        args.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int number)
            ? number : fallback;

    private static string? Truncate(string? text, int max) =>
        string.IsNullOrWhiteSpace(text) ? null : text.Length <= max ? text : text[..max] + "…";
}

/// <summary>The small WMO-code wording used by the chat tool (the desktop has its own, larger describer).</summary>
internal static class WeatherText
{
    public static string Describe(int code) => code switch
    {
        0 => "Trời quang",
        1 or 2 => "Ít mây",
        3 => "Nhiều mây",
        45 or 48 => "Sương mù",
        >= 51 and <= 57 => "Mưa phùn",
        >= 61 and <= 67 => "Mưa",
        >= 71 and <= 77 => "Mưa tuyết",
        >= 80 and <= 82 => "Mưa rào",
        >= 85 and <= 86 => "Mưa tuyết rào",
        >= 95 => "Dông",
        _ => "Không rõ",
    };

    public static string? Advice(WeatherDto weather)
    {
        var notes = new List<string>();
        if (weather.Precipitation > 0 || weather.Code is >= 51 and <= 67 or >= 80 and <= 82 or >= 95)
            notes.Add("mang áo mưa");
        if (weather.UvIndex >= 6 && weather.IsDay) notes.Add("nắng gắt, nhớ mũ và nước uống");
        if (weather.WindSpeed >= 35) notes.Add("gió mạnh");
        if (weather.ApparentTemperature >= 33) notes.Add("trời oi nóng");
        if (weather.ApparentTemperature <= 18) notes.Add("trời lạnh, mang áo khoác");
        return notes.Count == 0 ? null : string.Join("; ", notes);
    }
}
