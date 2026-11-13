using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Hue_travel.Api.Chat;

public sealed record ChatTurn(string? Role, string? Content);
public sealed record ChatContext(string? SelectedPoiId);
public sealed record ChatRequest(ChatTurn[]? Messages, ChatContext? Context);

public sealed record ChatPlaceRef(string Id, string Name, string? Group);
public sealed record ChatRouteDto(
    IReadOnlyList<string> Order, double Meters, double Seconds, IReadOnlyList<ChatPlaceRef> Stops);
public sealed record ChatResponse(string Reply, IReadOnlyList<ChatPlaceRef> Places, ChatRouteDto? Route, int Remaining);

/// <summary>A tool result that names a route, kept for the final reply (the route itself always comes from the server).</summary>
public sealed record ChatRouteInfo(IReadOnlyList<ChatPlaceRef> Order, IReadOnlyList<ChatPlaceRef> Stops, double Meters, double Seconds);

public sealed record ChatResult(string Reply, IReadOnlyList<ChatPlaceRef> Places, ChatRouteInfo? Route, long Tokens, int Rounds);

public sealed record ChatToolCall(string Id, string Name, string Arguments);

public sealed record ChatToolDefinition(string Name, string Description, JsonElement Parameters);

public sealed record ChatCompletion(string? Content, IReadOnlyList<ChatToolCall> ToolCalls, int PromptTokens, int CompletionTokens);

/// <summary>One message in the OpenAI-compatible conversation; tool calls are kept in the wire shape the API expects.</summary>
public sealed record ChatMessage(
    string Role, string? Content, IReadOnlyList<ChatToolCall>? ToolCalls = null, string? ToolCallId = null)
{
    public static ChatMessage System(string content) => new("system", content);
    public static ChatMessage User(string content) => new("user", content);
    public static ChatMessage Assistant(string? content) => new("assistant", content);
    public static ChatMessage AssistantTools(string? content, IReadOnlyList<ChatToolCall> calls) => new("assistant", content, calls);
    public static ChatMessage Tool(string callId, string content) => new("tool", content, null, callId);

    public JsonObject ToWire()
    {
        var message = new JsonObject { ["role"] = Role, ["content"] = Content };
        if (ToolCalls is { Count: > 0 })
        {
            var calls = new JsonArray();
            foreach (ChatToolCall call in ToolCalls)
            {
                calls.Add(new JsonObject
                {
                    ["id"] = call.Id,
                    ["type"] = "function",
                    ["function"] = new JsonObject { ["name"] = call.Name, ["arguments"] = call.Arguments },
                });
            }
            message["tool_calls"] = calls;
        }
        if (!string.IsNullOrEmpty(ToolCallId)) message["tool_call_id"] = ToolCallId;
        return message;
    }
}

public sealed class ChatUpstreamException : Exception
{
    public ChatUpstreamException(string message, Exception? inner = null) : base(message, inner) { }
}
