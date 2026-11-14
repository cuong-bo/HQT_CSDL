using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Hue_travel.Api.Pois;
using Hue_travel.Api.Routing;

namespace Hue_travel.Api.Chat;

/// <summary>What one chat turn collects while the tools run: the places they returned (the only ones the reply may point at) and the route they planned.</summary>
public sealed class ChatToolContext
{
    public Dictionary<string, PlaceRef> Seen { get; } = new(StringComparer.Ordinal);

    public ChatRoute? LastRoute { get; set; }
}

/// <summary>
/// The tools the model may call. They only READ (places, a route plan, the weather, the suggested tours); nothing the model says can change
/// data. Results are kept small because every token is paid for, and Vietnamese is not escaped to \uXXXX for the same reason.
/// </summary>
public sealed class ChatTools(IChatData data, ILogger<ChatTools> log)
{
    private static readonly HashSet<string> Groups = ["heritage", "scenery", "food", "stay", "service", "transport"];
    private static readonly JsonSerializerOptions Compact = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    private const int MaxSearch = 8;

    public JsonArray Definitions { get; } = BuildDefinitions();

    public async Task<string> RunAsync(ModelToolCall call, ChatToolContext ctx, CancellationToken ct)
    {
        try
        {
            JsonNode args;
            try { args = JsonNode.Parse(call.ArgumentsJson) ?? new JsonObject(); }
            catch (JsonException) { return Error("Tham số không phải JSON hợp lệ.").ToJsonString(Compact); }

            JsonNode result = call.Name switch
            {
                "search_places" => await SearchAsync(args, ctx, ct),
                "get_place" => await GetPlaceAsync(args, ctx, ct),
                "plan_route" => await PlanRouteAsync(args, ctx, ct),
                "get_weather" => await WeatherAsync(ct),
                "list_tours" => await ToursAsync(ctx, ct),
                _ => Error("Không có công cụ tên " + call.Name),
            };
            return result.ToJsonString(Compact);
        }
        catch (OperationCanceledException)
        {
            throw;   // the person left: not a tool failure
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "The assistant's tool {Tool} failed", call.Name);
            return Error("Công cụ gặp lỗi. Hãy trả lời mà không dùng dữ liệu này và nói rõ là chưa tra được.").ToJsonString(Compact);
        }
    }

    // ------------------------------------------------------------------ the tools

    private async Task<JsonNode> SearchAsync(JsonNode args, ChatToolContext ctx, CancellationToken ct)
    {
        string? query = Text(args, "query");
        string? group = Text(args, "group");
        if (group != null && !Groups.Contains(group)) return Error("group phải là một trong: " + string.Join(", ", Groups));
        int limit = Math.Clamp(Number(args, "limit") ?? 5, 1, MaxSearch);
        if (query is { Length: > 100 }) return Error("Từ khóa quá dài.");

        IReadOnlyList<PoiDto> found = await data.SearchPlacesAsync(query, group, limit, ct);
        var items = new JsonArray();
        foreach (PoiDto poi in found.Take(limit)) items.Add(Compact160(poi, ctx));
        return new JsonObject { ["count"] = items.Count, ["places"] = items, ["note"] = items.Count == 0 ? "Không tìm thấy địa điểm nào khớp." : null };
    }

    private async Task<JsonNode> GetPlaceAsync(JsonNode args, ChatToolContext ctx, CancellationToken ct)
    {
        string? id = Text(args, "id");
        if (!ChatText.IsPlaceId(id)) return Error("id không hợp lệ.");
        PoiDto? poi = await data.GetPlaceAsync(id!, ct);
        if (poi == null) return Error("Không tìm thấy địa điểm " + id);
        Remember(ctx, poi);
        var node = new JsonObject
        {
            ["id"] = poi.Id, ["name"] = poi.Name, ["group"] = poi.Group, ["type"] = poi.Type, ["address"] = poi.Address,
            ["openingHours"] = poi.OpeningHours, ["priceText"] = poi.PriceText, ["priceFrom"] = poi.PriceFrom, ["priceTo"] = poi.PriceTo,
            ["phone"] = poi.Phone, ["website"] = poi.Website, ["summary"] = Cut(poi.Summary, 600),
        };
        return node;
    }

    private async Task<JsonNode> PlanRouteAsync(JsonNode args, ChatToolContext ctx, CancellationToken ct)
    {
        string? start = Text(args, "startId");
        if (!ChatText.IsPlaceId(start)) return Error("startId không hợp lệ.");
        if (args["stopIds"] is not JsonArray list) return Error("Thiếu stopIds (danh sách mã địa điểm).");
        var stops = new List<string>();
        foreach (JsonNode? item in list)
        {
            string? stop = item is JsonValue v && v.TryGetValue(out string? s) ? s : null;
            if (!ChatText.IsPlaceId(stop)) return Error("Có mã địa điểm không hợp lệ trong stopIds.");
            stops.Add(stop!);
        }
        if (stops.Count is < 1 or > RouteOptimizer.MaxStops) return Error($"stopIds cần từ 1 đến {RouteOptimizer.MaxStops} địa điểm.");
        if (stops.Distinct(StringComparer.Ordinal).Count() != stops.Count || stops.Contains(start!, StringComparer.Ordinal))
            return Error("Các địa điểm phải khác nhau và khác điểm xuất phát.");
        bool optimize = args["optimize"] is JsonValue o && o.TryGetValue(out bool b) ? b : true;

        RoutePlanOutcome outcome = await data.PlanRouteAsync(start!, stops.ToArray(), optimize, ct);
        if (!outcome.Ok) return Error(outcome.ErrorDetail ?? "Không lập được lộ trình.");
        RouteResponse route = outcome.Response!;

        var order = new List<string> { start! };
        order.AddRange(route.Order);
        var named = new List<RouteStopRef>();
        foreach (string id in order) named.Add(new RouteStopRef(id, await NameOfAsync(id, ctx, ct)));
        ctx.LastRoute = new ChatRoute(order, route.TotalMeters, route.TotalSeconds, named);

        var legs = new JsonArray();
        foreach (RouteLeg leg in route.Legs)
        {
            string from = leg.FromId == RouteEndpoints.StartLabel ? start! : leg.FromId;
            legs.Add(new JsonObject
            {
                ["from"] = await NameOfAsync(from, ctx, ct), ["to"] = await NameOfAsync(leg.ToId, ctx, ct),
                ["km"] = Math.Round(leg.Meters / 1000, 1), ["minutes"] = (int)Math.Round(leg.Seconds / 60),
            });
        }
        var stopNodes = new JsonArray();
        foreach (RouteStopRef r in named) stopNodes.Add(new JsonObject { ["id"] = r.Id, ["name"] = r.Name });
        return new JsonObject
        {
            ["order"] = stopNodes, ["legs"] = legs,
            ["totalKm"] = Math.Round(route.TotalMeters / 1000, 1), ["totalMinutes"] = (int)Math.Round(route.TotalSeconds / 60),
        };
    }

    private async Task<JsonNode> WeatherAsync(CancellationToken ct)
    {
        var w = await data.GetWeatherAsync(ct);
        if (w == null) return Error("Chưa lấy được thời tiết lúc này.");
        return new JsonObject
        {
            ["temperatureC"] = Math.Round(w.Temperature, 1), ["feelsLikeC"] = Math.Round(w.ApparentTemperature, 1),
            ["condition"] = Describe(w.Code), ["isDaytime"] = w.IsDay, ["rainMm"] = Math.Round(w.Precipitation, 1),
            ["windKmh"] = Math.Round(w.WindSpeed, 0), ["uvIndex"] = Math.Round(w.UvIndex, 0),
        };
    }

    private async Task<JsonNode> ToursAsync(ChatToolContext ctx, CancellationToken ct)
    {
        var tours = new JsonArray();
        foreach (ChatTour tour in ChatKnowledge.Tours)
        {
            var places = new JsonArray();
            foreach (string id in tour.PlaceIds)
            {
                PoiDto? poi = await data.GetPlaceAsync(id, ct);
                if (poi == null) continue;   // a place that no longer exists drops out of the tour
                Remember(ctx, poi);
                places.Add(new JsonObject { ["id"] = poi.Id, ["name"] = poi.Name, ["openingHours"] = poi.OpeningHours });
            }
            if (places.Count >= 2) tours.Add(new JsonObject { ["title"] = tour.Title, ["duration"] = tour.Duration, ["places"] = places });
        }
        return new JsonObject { ["tours"] = tours };
    }

    // ------------------------------------------------------------------ helpers

    private static JsonObject Compact160(PoiDto poi, ChatToolContext ctx)
    {
        Remember(ctx, poi);
        return new JsonObject
        {
            ["id"] = poi.Id, ["name"] = poi.Name, ["group"] = poi.Group, ["type"] = poi.Type, ["address"] = poi.Address,
            ["openingHours"] = poi.OpeningHours, ["priceText"] = poi.PriceText, ["summary"] = Cut(poi.Summary, 160),
        };
    }

    private static void Remember(ChatToolContext ctx, PoiDto poi) => ctx.Seen[poi.Id] = new PlaceRef(poi.Id, poi.Name, poi.Group);

    private async Task<string> NameOfAsync(string id, ChatToolContext ctx, CancellationToken ct)
    {
        if (ctx.Seen.TryGetValue(id, out PlaceRef? known)) return known.Name;
        PoiDto? poi = await data.GetPlaceAsync(id, ct);
        if (poi == null) return id;
        Remember(ctx, poi);
        return poi.Name;
    }

    private static JsonObject Error(string message) => new() { ["error"] = message };

    private static string? Cut(string? text, int max)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        text = text.Trim();
        return text.Length <= max ? text : text[..(max - 1)].TrimEnd() + "…";
    }

    private static string? Text(JsonNode args, string name) =>
        args[name] is JsonValue v && v.TryGetValue(out string? s) && !string.IsNullOrWhiteSpace(s) ? s.Trim() : null;

    private static int? Number(JsonNode args, string name)
    {
        if (args[name] is not JsonValue v) return null;
        if (v.TryGetValue(out int i)) return i;
        if (v.TryGetValue(out double d) && double.IsFinite(d)) return (int)Math.Round(d);
        return null;
    }

    /// <summary>The weather code (WMO) in words the assistant can repeat.</summary>
    public static string Describe(int code) => code switch
    {
        0 => "trời quang",
        1 or 2 => "ít mây",
        3 => "nhiều mây",
        45 or 48 => "sương mù",
        >= 51 and <= 57 => "mưa phùn",
        >= 61 and <= 67 => "mưa",
        >= 80 and <= 82 => "mưa rào",
        >= 95 => "dông",
        _ => "thời tiết thay đổi",
    };

    private static JsonArray BuildDefinitions()
    {
        static JsonObject Tool(string name, string description, JsonObject properties, params string[] required) => new()
        {
            ["type"] = "function",
            ["function"] = new JsonObject
            {
                ["name"] = name,
                ["description"] = description,
                ["parameters"] = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = properties,
                    ["required"] = new JsonArray(required.Select(r => (JsonNode)JsonValue.Create(r)!).ToArray()),
                },
            },
        };
        static JsonObject Str(string description) => new() { ["type"] = "string", ["description"] = description };

        return new JsonArray
        {
            Tool("search_places",
                "Tìm địa điểm du lịch ở Huế theo tên hoặc theo nhóm. Gửi tên CÓ DẤU tiếng Việt. Trả tối đa 8 địa điểm gồm mã, tên, loại, địa chỉ, giờ mở cửa, giá.",
                new JsonObject
                {
                    ["query"] = Str("Từ khóa trong tên địa điểm, ví dụ \"bún bò\", \"Đại Nội\". Bỏ trống để lọc theo nhóm."),
                    ["group"] = new JsonObject
                    {
                        ["type"] = "string", ["description"] = "Nhóm: heritage (di tích), scenery (cảnh quan), food (ẩm thực), stay (lưu trú), service (dịch vụ), transport (giao thông).",
                        ["enum"] = new JsonArray("heritage", "scenery", "food", "stay", "service", "transport"),
                    },
                    ["limit"] = new JsonObject { ["type"] = "integer", ["description"] = "Số kết quả tối đa, 1 đến 8 (mặc định 5)." },
                }),
            Tool("get_place", "Lấy thông tin đầy đủ của MỘT địa điểm theo mã (giờ mở cửa, giá, điện thoại, trang web, mô tả).",
                new JsonObject { ["id"] = Str("Mã địa điểm, ví dụ DL021.") }, "id"),
            Tool("plan_route",
                "Lập lộ trình đường bộ thật từ một điểm xuất phát qua các điểm dừng, sắp xếp thứ tự ngắn nhất (trừ khi optimize=false). Trả thứ tự, từng chặng (km, phút) và tổng. KHÔNG tự ước lượng thời gian di chuyển: luôn gọi công cụ này.",
                new JsonObject
                {
                    ["startId"] = Str("Mã địa điểm xuất phát."),
                    ["stopIds"] = new JsonObject { ["type"] = "array", ["items"] = new JsonObject { ["type"] = "string" }, ["description"] = "Các mã địa điểm cần đi qua, từ 1 đến 12, khác nhau và khác điểm xuất phát." },
                    ["optimize"] = new JsonObject { ["type"] = "boolean", ["description"] = "true (mặc định): sắp xếp lại cho ngắn nhất; false: giữ đúng thứ tự đã cho." },
                }, "startId", "stopIds"),
            Tool("get_weather", "Thời tiết hiện tại ở Huế: nhiệt độ, cảm giác, tình trạng, mưa, gió, tia UV.", new JsonObject()),
            Tool("list_tours", "Các tour gợi ý có sẵn (tên, thời lượng, các điểm theo thứ tự đi). Dùng khi người dùng muốn gợi ý lịch trình.", new JsonObject()),
        };
    }
}
