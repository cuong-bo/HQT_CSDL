using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace Hue_travel.Api.Chat;

/// <summary>
/// Runs one chat turn: system prompt + recent history, then up to <see cref="ChatOptions.MaxToolRounds"/> rounds of tool calls.
/// The model's prose is cleaned against the place ids the tools actually returned, so a made-up id never becomes a link.
/// </summary>
public sealed class ChatOrchestrator(DeepSeekClient deepSeek, ChatTools tools, IOptions<ChatOptions> options, TimeProvider clock)
{
    private static readonly Regex PlaceToken = new(@"\[\[([A-Za-z0-9_-]{1,32})\]\]", RegexOptions.CultureInvariant);

    public async Task<ChatResult> RunAsync(ChatRequest request, CancellationToken ct)
    {
        ChatOptions settings = options.Value;
        var seen = new Dictionary<string, ChatPlaceRef>(StringComparer.Ordinal);
        ChatRouteInfo? lastRoute = null;
        var messages = new List<ChatMessage>
        {
            ChatMessage.System(Prompt.Build(clock.GetUtcNow(), request.Context?.SelectedPoiId)),
        };
        foreach (ChatTurn turn in (request.Messages ?? []).TakeLast(10))
        {
            if (string.Equals(turn.Role, "assistant", StringComparison.Ordinal))
                messages.Add(ChatMessage.Assistant(turn.Content ?? ""));
            else
                messages.Add(ChatMessage.User(turn.Content ?? ""));
        }

        long tokens = 0;
        int rounds = Math.Clamp(settings.MaxToolRounds, 1, 6);
        for (int round = 0; round < rounds; round++)
        {
            ChatCompletion reply = await deepSeek.CompleteAsync(messages, tools.Definitions, ct);
            tokens += reply.PromptTokens + reply.CompletionTokens;
            if (reply.ToolCalls.Count == 0)
                return Finish(reply.Content, seen, lastRoute, tokens, round + 1);

            messages.Add(ChatMessage.AssistantTools(reply.Content, reply.ToolCalls));
            foreach (ChatToolCall call in reply.ToolCalls)
            {
                string result = await tools.RunAsync(call, seen, route => lastRoute = route, ct);
                messages.Add(ChatMessage.Tool(call.Id, result));
            }
        }

        return Finish("Mình chưa tìm đủ thông tin, bạn thử hỏi cụ thể hơn nhé.", seen, lastRoute, tokens, rounds);
    }

    private static ChatResult Finish(string? raw, Dictionary<string, ChatPlaceRef> seen, ChatRouteInfo? route, long tokens, int rounds)
    {
        var places = new List<ChatPlaceRef>();
        var added = new HashSet<string>(StringComparer.Ordinal);
        string text = StripMarkdown(raw ?? "");
        text = PlaceToken.Replace(text, match =>
        {
            string id = match.Groups[1].Value;
            if (!seen.TryGetValue(id, out ChatPlaceRef? place)) return "";   // the model invented it: drop the marker
            if (added.Add(place.Id)) places.Add(place);
            return place.Name;
        });
        text = Regex.Replace(text, @"[ \t]{2,}", " ").Trim();
        if (text.Length > 2500) text = text[..2500].TrimEnd() + "…";
        if (text.Length == 0) text = "Mình chưa có câu trả lời cho câu hỏi này.";
        return new ChatResult(text, places, route, tokens, rounds);
    }

    private static string StripMarkdown(string text)
    {
        text = Regex.Replace(text, @"\*\*|__|`{1,3}", "");
        text = Regex.Replace(text, @"(?m)^\s{0,3}#{1,6}\s*", "");
        text = Regex.Replace(text, @"(?m)^\s*>\s?", "");
        return text.Trim();
    }
}
