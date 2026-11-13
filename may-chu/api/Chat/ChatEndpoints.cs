using System.Diagnostics;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace Hue_travel.Api.Chat;

public sealed class ChatEndpointLog;

public static partial class ChatEndpoints
{
    private const int MaxMessages = 12;
    private const int MaxMessageLength = 2000;

    [GeneratedRegex("^[A-Za-z0-9_-]{1,32}$")]
    private static partial Regex PoiIdPattern();

    public static void MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/chat", ChatAsync).RequireRateLimiting("chat");
    }

    private static async Task<IResult> ChatAsync(
        ChatRequest request, ClaimsPrincipal user, ChatOrchestrator orchestrator, ChatQuota quota,
        IOptions<ChatOptions> options, HttpContext http, ILogger<ChatEndpointLog> log, CancellationToken ct)
    {
        ChatTurn[] messages = request.Messages ?? [];
        if (messages.Length == 0) return Bad("Cần ít nhất một tin nhắn.");
        if (messages.Length > MaxMessages) return Bad($"Mỗi lượt chỉ gửi tối đa {MaxMessages} tin nhắn.");
        if (messages[^1] == null || !string.Equals(messages[^1].Role, "user", StringComparison.Ordinal))
            return Bad("Tin nhắn cuối phải của người dùng.");
        foreach (ChatTurn? message in messages)
        {
            if (message == null || message.Role is not ("user" or "assistant")) return Bad("Chỉ nhận tin nhắn user hoặc assistant.");
            if (message.Content == null || message.Content.Length > MaxMessageLength)
                return Bad($"Mỗi tin nhắn tối đa {MaxMessageLength} ký tự.");
        }
        string? selected = request.Context?.SelectedPoiId;
        if (selected != null && !PoiIdPattern().IsMatch(selected)) return Bad("selectedPoiId không hợp lệ.");

        if (!options.Value.Enabled)
            return Results.Problem(detail: "Trợ lý chưa được bật.", statusCode: StatusCodes.Status503ServiceUnavailable);
        if (quota.GlobalTokensExceeded)
            return Results.Problem(detail: "Trợ lý tạm nghỉ hôm nay.", statusCode: StatusCodes.Status503ServiceUnavailable);

        bool signedIn = user.Identity?.IsAuthenticated == true;
        string scope = "unknown";
        if (signedIn)
        {
            string? sub = user.FindFirstValue("sub");
            scope = long.TryParse(sub, out long id) ? id.ToString() : "unknown";
        }
        else
        {
            scope = http.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
        if (!quota.TryTake(!signedIn, scope, out int remaining))
            return Results.Problem(detail: "Bạn đã dùng hết số câu hỏi hôm nay. Hãy thử lại vào ngày mai.", statusCode: StatusCodes.Status429TooManyRequests);

        long started = Stopwatch.GetTimestamp();
        try
        {
            ChatResult result = await orchestrator.RunAsync(request, ct);
            quota.AddTokens(result.Tokens);
            ChatRouteDto? route = result.Route == null
                ? null
                : new ChatRouteDto(result.Route.Order.Select(p => p.Id).ToList(), result.Route.Meters, result.Route.Seconds, result.Route.Stops);
            log.LogInformation("chat: signedIn={SignedIn} rounds={Rounds} tokens={Tokens} ms={Elapsed}",
                signedIn, result.Rounds, result.Tokens, (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            return Results.Ok(new ChatResponse(result.Reply, result.Places, route, remaining));
        }
        catch (ChatUpstreamException ex)
        {
            log.LogWarning(ex, "chat upstream failed");
            return Results.Problem(detail: "Trợ lý đang bận, thử lại sau.", statusCode: StatusCodes.Status502BadGateway);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Results.Problem(detail: "Trợ lý đang bận, thử lại sau.", statusCode: StatusCodes.Status502BadGateway);
        }
    }

    private static IResult Bad(string detail) => Results.Problem(detail: detail, statusCode: StatusCodes.Status400BadRequest);
}
