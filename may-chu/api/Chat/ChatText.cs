using System.Text;
using System.Text.RegularExpressions;

namespace Hue_travel.Api.Chat;

/// <summary>Checks what the app sends, and cleans what the model answers. Pure functions, tested without a server.</summary>
public static class ChatText
{
    public const int MaxTurns = 12;
    public const int MaxLength = 2000;
    public const int MaxReply = 2500;
    public const int KeepTurns = 10;

    private static readonly Regex Marker = new(@"[ \t]*\[\[([A-Za-z0-9_\-]{1,32})\]\]", RegexOptions.Compiled);
    private static readonly Regex IdPattern = new(@"^[A-Za-z0-9_\-]{1,32}$", RegexOptions.Compiled);
    private static readonly Regex HeadingMark = new(@"^[ \t]*#{1,6}[ \t]+", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex StarBullet = new(@"^([ \t]*)[\*•][ \t]+", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex ManyBlankLines = new(@"\n{3,}", RegexOptions.Compiled);

    public static bool IsPlaceId(string? id) => id != null && IdPattern.IsMatch(id);

    /// <summary>The sentence for the person when the request is not acceptable, else null. Only "user" and "assistant" are accepted: a "system" turn from the app would be a way to rewrite the assistant's rules.</summary>
    public static string? Validate(ChatRequest request)
    {
        List<ChatTurn>? turns = request.Messages;
        if (turns == null || turns.Count == 0) return "Hãy nhập câu hỏi.";
        if (turns.Count > MaxTurns) return $"Mỗi lần hỏi gửi tối đa {MaxTurns} tin nhắn.";
        foreach (ChatTurn turn in turns)
        {
            string role = (turn.Role ?? "").Trim().ToLowerInvariant();
            if (role is not ("user" or "assistant")) return "Vai trò của tin nhắn chỉ có thể là user hoặc assistant.";
            string text = Clean(turn.Content);
            if (text.Length == 0) return "Tin nhắn không được để trống.";
            if (text.Length > MaxLength) return $"Mỗi tin nhắn tối đa {MaxLength} ký tự.";
        }
        if (!string.Equals(turns[^1].Role?.Trim(), "user", StringComparison.OrdinalIgnoreCase)) return "Tin nhắn cuối cùng phải là của người dùng.";
        string? selected = request.Context?.SelectedPoiId;
        if (!string.IsNullOrEmpty(selected) && !IsPlaceId(selected)) return "Mã địa điểm không hợp lệ.";
        return null;
    }

    /// <summary>The text without control characters (a new line stays), trimmed. Never HTML: the app shows it as plain text.</summary>
    public static string Clean(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        var sb = new StringBuilder(text.Length);
        foreach (char c in text)
        {
            if (c == '\r') continue;
            sb.Append(c == '\n' || c == '\t' || !char.IsControl(c) ? c : ' ');
        }
        return sb.ToString().Trim();
    }

    /// <summary>The last turns the model gets, validated and cleaned; place markers are removed so the app cannot plant them.</summary>
    public static List<ModelMessage> History(IEnumerable<ChatTurn> turns) =>
        turns.TakeLast(KeepTurns)
            .Select(t => new ModelMessage(t.Role!.Trim().ToLowerInvariant(), Marker.Replace(Clean(t.Content), "")))
            .ToList();

    /// <summary>
    /// Turns the model's text into the reply: a place marker like [[DL021]] is removed, and the place goes into the list of places only when the
    /// tools really returned it this turn (an invented code is dropped); Markdown marks are removed (the window does not draw them); the text
    /// is cut to a sane length. The route comes from what the planner computed, not from the words.
    /// </summary>
    public static ChatReply Finish(string? content, ChatToolContext ctx, int? remaining)
    {
        var referenced = new List<PlaceRef>();
        string text = Marker.Replace(content ?? "", m =>
        {
            string id = m.Groups[1].Value;
            if (ctx.Seen.TryGetValue(id, out PlaceRef? place) && referenced.All(r => r.Id != id)) referenced.Add(place);
            return "";
        });
        text = StripMarkdown(text);
        if (text.Length == 0) text = "Mình chưa có câu trả lời cho câu này, bạn thử hỏi lại theo cách khác nhé.";
        if (text.Length > MaxReply) text = text[..(MaxReply - 1)].TrimEnd() + "…";
        return new ChatReply(text, referenced, ctx.LastRoute, remaining);
    }

    public static string StripMarkdown(string text)
    {
        text = text.Replace("\r", "").Replace("**", "").Replace("__", "").Replace("`", "").Replace("~~", "");
        text = HeadingMark.Replace(text, "");
        text = StarBullet.Replace(text, "$1- ");
        text = ManyBlankLines.Replace(text, "\n\n");
        return text.Trim();
    }
}
