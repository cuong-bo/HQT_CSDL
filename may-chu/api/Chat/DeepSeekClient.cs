using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

namespace Hue_travel.Api.Chat;

/// <summary>
/// Talks to DeepSeek (an OpenAI-compatible chat API). Everything that is specific to DeepSeek is here, so changing the provider or the
/// model is a change of this file and of the Chat settings only. Nothing about the person (name, account, address) is ever sent.
/// </summary>
public sealed class DeepSeekClient(IHttpClientFactory clients, IOptions<ChatOptions> options) : IDeepSeek
{
    public async Task<ModelReply> CompleteAsync(IReadOnlyList<ModelMessage> messages, JsonArray tools, CancellationToken ct)
    {
        ChatOptions o = options.Value;
        var body = new JsonObject
        {
            ["model"] = o.Model,
            ["temperature"] = o.Temperature,
            ["max_tokens"] = o.MaxOutputTokens,
            ["stream"] = false,
            ["messages"] = ToJson(messages),
        };
        if (tools.Count > 0) body["tools"] = tools.DeepClone();

        using HttpClient http = clients.CreateClient("deepseek");
        using var request = new HttpRequestMessage(HttpMethod.Post, o.BaseUrl.TrimEnd('/') + "/chat/completions")
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", o.ApiKey);

        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(5, o.TimeoutSeconds)));
        try
        {
            using HttpResponseMessage response = await http.SendAsync(request, timeout.Token);
            string text = await response.Content.ReadAsStringAsync(timeout.Token);
            // The status only: an error body can echo what was sent.
            if (!response.IsSuccessStatusCode) throw new ChatUpstreamException("DeepSeek answered " + (int)response.StatusCode);
            return Parse(text);
        }
        catch (HttpRequestException ex)
        {
            throw new ChatUpstreamException("DeepSeek could not be reached", ex);
        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new ChatUpstreamException("DeepSeek took too long", ex);   // the timeout, not the person leaving
        }
    }

    /// <summary>The messages in the wire format (assistant turns that ask for tools carry their tool_calls; tool results carry the call id).</summary>
    public static JsonArray ToJson(IReadOnlyList<ModelMessage> messages)
    {
        var array = new JsonArray();
        foreach (ModelMessage m in messages)
        {
            var node = new JsonObject { ["role"] = m.Role, ["content"] = m.Content ?? string.Empty };
            if (m.ToolCalls is { Count: > 0 })
            {
                var calls = new JsonArray();
                foreach (ModelToolCall call in m.ToolCalls)
                {
                    calls.Add(new JsonObject
                    {
                        ["id"] = call.Id,
                        ["type"] = "function",
                        ["function"] = new JsonObject { ["name"] = call.Name, ["arguments"] = call.ArgumentsJson },
                    });
                }
                node["tool_calls"] = calls;
            }
            if (m.ToolCallId != null) node["tool_call_id"] = m.ToolCallId;
            array.Add(node);
        }
        return array;
    }

    /// <summary>Reads choices[0].message (text and tool calls) and the token usage of a chat completion.</summary>
    public static ModelReply Parse(string json)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;
            if (!root.TryGetProperty("choices", out JsonElement choices) || choices.ValueKind != JsonValueKind.Array || choices.GetArrayLength() == 0
                || !choices[0].TryGetProperty("message", out JsonElement message))
                throw new ChatUpstreamException("DeepSeek answered without a message");

            string? content = message.TryGetProperty("content", out JsonElement c) && c.ValueKind == JsonValueKind.String ? c.GetString() : null;
            var calls = new List<ModelToolCall>();
            if (message.TryGetProperty("tool_calls", out JsonElement toolCalls) && toolCalls.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement call in toolCalls.EnumerateArray())
                {
                    if (!call.TryGetProperty("function", out JsonElement fn)) continue;
                    string id = call.TryGetProperty("id", out JsonElement i) && i.ValueKind == JsonValueKind.String ? i.GetString()! : "call_" + calls.Count;
                    string name = fn.TryGetProperty("name", out JsonElement n) && n.ValueKind == JsonValueKind.String ? n.GetString()! : "";
                    string args = fn.TryGetProperty("arguments", out JsonElement a) && a.ValueKind == JsonValueKind.String ? a.GetString()! : "{}";
                    if (name.Length > 0) calls.Add(new ModelToolCall(id, name, string.IsNullOrWhiteSpace(args) ? "{}" : args));
                }
            }

            int prompt = 0, completion = 0;
            if (root.TryGetProperty("usage", out JsonElement usage))
            {
                if (usage.TryGetProperty("prompt_tokens", out JsonElement p) && p.TryGetInt32(out int pv)) prompt = pv;
                if (usage.TryGetProperty("completion_tokens", out JsonElement q) && q.TryGetInt32(out int qv)) completion = qv;
            }
            return new ModelReply(content, calls, prompt, completion);
        }
        catch (JsonException ex)
        {
            throw new ChatUpstreamException("DeepSeek answered with something that is not JSON", ex);
        }
    }
}
