using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

namespace Hue_travel.Api.Chat;

/// <summary>
/// The only place that knows the DeepSeek wire format (OpenAI-compatible chat completions with function calling). A different
/// provider can replace this file without touching the orchestrator or the tools.
/// </summary>
public sealed class DeepSeekClient(IHttpClientFactory clients, IOptions<ChatOptions> options)
{
    public async Task<ChatCompletion> CompleteAsync(
        IReadOnlyList<ChatMessage> messages, IReadOnlyList<ChatToolDefinition> tools, CancellationToken ct)
    {
        ChatOptions settings = options.Value;
        if (!settings.Enabled) throw new ChatUpstreamException("Trợ lý chưa được bật.");

        var payload = new JsonObject
        {
            ["model"] = settings.Model,
            ["messages"] = new JsonArray(messages.Select(m => (JsonNode)m.ToWire()).ToArray()),
            ["temperature"] = settings.Temperature,
            ["max_tokens"] = settings.MaxOutputTokens,
            ["stream"] = false,
        };
        if (tools.Count > 0)
        {
            payload["tools"] = new JsonArray(tools.Select(t => (JsonNode)new JsonObject
            {
                ["type"] = "function",
                ["function"] = new JsonObject
                {
                    ["name"] = t.Name,
                    ["description"] = t.Description,
                    ["parameters"] = JsonNode.Parse(t.Parameters.GetRawText()),
                },
            }).ToArray());
        }
        if (!string.IsNullOrWhiteSpace(settings.Thinking))
        {
            payload["thinking"] = new JsonObject { ["type"] = settings.Thinking.Trim() };
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(5, settings.TimeoutSeconds)));
        using var request = new HttpRequestMessage(HttpMethod.Post, settings.BaseUrl.TrimEnd('/') + "/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        request.Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            using HttpClient client = clients.CreateClient("deepseek");
            response = await client.SendAsync(request, timeout.Token);
        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new ChatUpstreamException("Trợ lý đang bận, thử lại sau.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new ChatUpstreamException("Trợ lý đang bận, thử lại sau.", ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
                throw new ChatUpstreamException("Trợ lý đang bận, thử lại sau.");

            string body;
            try
            {
                body = await response.Content.ReadAsStringAsync(timeout.Token);
            }
            catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
            {
                throw new ChatUpstreamException("Trợ lý đang bận, thử lại sau.", ex);
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(body);
                JsonElement root = document.RootElement;
                if (!root.TryGetProperty("choices", out JsonElement choices) || choices.ValueKind != JsonValueKind.Array || choices.GetArrayLength() == 0)
                    throw new ChatUpstreamException("Trợ lý trả về câu trả lời không hợp lệ.");

                JsonElement message = choices[0].GetProperty("message");
                string? content = message.TryGetProperty("content", out JsonElement text) && text.ValueKind == JsonValueKind.String
                    ? text.GetString() : null;

                var calls = new List<ChatToolCall>();
                if (message.TryGetProperty("tool_calls", out JsonElement toolCalls) && toolCalls.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement call in toolCalls.EnumerateArray())
                    {
                        if (!call.TryGetProperty("id", out JsonElement id) || !call.TryGetProperty("function", out JsonElement function)) continue;
                        string? name = function.TryGetProperty("name", out JsonElement nameValue) ? nameValue.GetString() : null;
                        string arguments = function.TryGetProperty("arguments", out JsonElement args) && args.ValueKind == JsonValueKind.String
                            ? args.GetString() ?? "{}" : "{}";
                        if (!string.IsNullOrWhiteSpace(name)) calls.Add(new ChatToolCall(id.GetString() ?? "", name!, arguments));
                    }
                }

                int promptTokens = 0, completionTokens = 0;
                if (root.TryGetProperty("usage", out JsonElement usage))
                {
                    if (usage.TryGetProperty("prompt_tokens", out JsonElement prompt)) promptTokens = prompt.GetInt32();
                    if (usage.TryGetProperty("completion_tokens", out JsonElement completion)) completionTokens = completion.GetInt32();
                }
                return new ChatCompletion(content, calls, promptTokens, completionTokens);
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException or FormatException)
            {
                throw new ChatUpstreamException("Trợ lý trả về câu trả lời không hợp lệ.", ex);
            }
        }
    }
}
