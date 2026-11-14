using Hue_travel.Api.Pois;
using Microsoft.Extensions.Options;

namespace Hue_travel.Api.Chat;

/// <summary>
/// One chat turn: the model is asked, may ask for tools (the server runs them), is asked again with the results, and so on until it answers
/// or the number of rounds runs out. The reply is then made from what the tools really returned.
/// </summary>
public sealed class ChatOrchestrator(IDeepSeek model, ChatTools tools, IChatData data, IOptions<ChatOptions> options, TimeProvider clock)
{
    public const string GaveUp = "Mình chưa tìm đủ thông tin để trả lời, bạn thử hỏi cụ thể hơn nhé.";

    /// <summary>The request must already have passed <see cref="ChatText.Validate"/>. Returns the reply and the tokens it cost.</summary>
    public async Task<(ChatReply Reply, int Tokens)> RunAsync(ChatRequest request, int? remaining, CancellationToken ct)
    {
        ChatOptions o = options.Value;
        var ctx = new ChatToolContext();

        string? selectedLine = null;
        string? selectedId = request.Context?.SelectedPoiId;
        if (ChatText.IsPlaceId(selectedId))
        {
            PoiDto? selected = await data.GetPlaceAsync(selectedId!, ct);
            if (selected != null)
            {
                ctx.Seen[selected.Id] = new PlaceRef(selected.Id, selected.Name, selected.Group);
                selectedLine = $"{selected.Name} (mã {selected.Id})";
            }
        }

        var messages = new List<ModelMessage> { ModelMessage.System(ChatPrompt.Build(clock.GetUtcNow(), selectedLine)) };
        messages.AddRange(ChatText.History(request.Messages!));

        int tokens = 0;
        for (int round = 0; round < Math.Max(1, o.MaxToolRounds); round++)
        {
            ModelReply reply = await model.CompleteAsync(messages, tools.Definitions, ct);
            tokens += reply.PromptTokens + reply.CompletionTokens;
            if (reply.ToolCalls.Count == 0) return (ChatText.Finish(reply.Content, ctx, remaining), tokens);

            messages.Add(reply.AsMessage());
            foreach (ModelToolCall call in reply.ToolCalls)
                messages.Add(ModelMessage.Tool(call.Id, await tools.RunAsync(call, ctx, ct)));
        }
        return (ChatText.Finish(GaveUp, ctx, remaining), tokens);
    }
}
