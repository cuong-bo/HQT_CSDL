using System.Text.Json.Nodes;
using Hue_travel.Api.Pois;
using Hue_travel.Api.Routing;
using Hue_travel.Api.Weather;

namespace Hue_travel.Api.Chat;

// ---- what the desktop app sends and receives (POST /v1/chat)

public sealed record ChatTurn(string? Role, string? Content);

public sealed record ChatContext(string? SelectedPoiId);

public sealed record ChatRequest(List<ChatTurn>? Messages, ChatContext? Context);

public sealed record PlaceRef(string Id, string Name, string Group);

public sealed record RouteStopRef(string Id, string Name);

/// <summary>The route the model asked for, as the SERVER computed it (never taken from the model's words).</summary>
public sealed record ChatRoute(IReadOnlyList<string> Order, double Meters, double Seconds, IReadOnlyList<RouteStopRef> Stops);

public sealed record ChatReply(string Reply, IReadOnlyList<PlaceRef> Places, ChatRoute? Route, int? Remaining);

// ---- the conversation with the model

/// <summary>One message sent to the model: the system prompt, a user / assistant turn, an assistant turn that asks for tools, or a tool result.</summary>
public sealed record ModelMessage(string Role, string? Content, IReadOnlyList<ModelToolCall>? ToolCalls = null, string? ToolCallId = null)
{
    public static ModelMessage System(string text) => new("system", text);
    public static ModelMessage Tool(string callId, string json) => new("tool", json, null, callId);
}

public sealed record ModelToolCall(string Id, string Name, string ArgumentsJson);

public sealed record ModelReply(string? Content, IReadOnlyList<ModelToolCall> ToolCalls, int PromptTokens, int CompletionTokens)
{
    public ModelMessage AsMessage() => new("assistant", Content, ToolCalls.Count > 0 ? ToolCalls : null);
}

/// <summary>The model provider could not answer (network, timeout, error status): the person is told to try again.</summary>
public sealed class ChatUpstreamException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>Asks the model. The one place that knows DeepSeek's wire format; tests replace it.</summary>
public interface IDeepSeek
{
    Task<ModelReply> CompleteAsync(IReadOnlyList<ModelMessage> messages, JsonArray tools, CancellationToken ct);
}

/// <summary>What the tools read: places, routes and weather. The real one goes to the database; tests use a fake.</summary>
public interface IChatData
{
    Task<IReadOnlyList<PoiDto>> SearchPlacesAsync(string? query, string? group, int limit, CancellationToken ct);

    Task<PoiDto?> GetPlaceAsync(string id, CancellationToken ct);

    Task<RoutePlanOutcome> PlanRouteAsync(string startId, string[] stopIds, bool optimize, CancellationToken ct);

    Task<WeatherDto?> GetWeatherAsync(CancellationToken ct);
}
