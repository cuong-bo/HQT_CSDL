using System.Security.Claims;
using Hue_travel.Api.Audit;
using Microsoft.Extensions.Options;

namespace Hue_travel.Api.Chat;

public static class ChatEndpoints
{
    public static void MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        // Open to guests (like the rest of the read-only features); the limits below keep the cost in check.
        app.MapGet("/v1/chat/status", (IOptions<ChatOptions> options) => Results.Ok(new { enabled = options.Value.Enabled }));
        app.MapPost("/v1/chat", ChatAsync).RequireRateLimiting("chat");
    }

    private static async Task<IResult> ChatAsync(
        ChatRequest request, ClaimsPrincipal user, HttpContext http, IOptions<ChatOptions> options, ChatOrchestrator orchestrator,
        ChatQuota quota, ILogger<ChatOrchestrator> log, CancellationToken ct)
    {
        if (!options.Value.Enabled)
            return Results.Problem(detail: "Trợ lý chưa được bật.", statusCode: StatusCodes.Status503ServiceUnavailable);
        string? problem = ChatText.Validate(request);
        if (problem != null) return Results.Problem(detail: problem, statusCode: StatusCodes.Status400BadRequest);
        if (!quota.BudgetOpen())
            return Results.Problem(detail: "Trợ lý tạm nghỉ hôm nay, bạn quay lại vào ngày mai nhé.", statusCode: StatusCodes.Status503ServiceUnavailable);

        bool signedIn = user.Identity?.IsAuthenticated == true;
        string subject = signedIn ? "u:" + user.UserId() : "ip:" + (http.Connection.RemoteIpAddress?.ToString() ?? "unknown");
        if (!quota.TryTake(subject, signedIn, out int remaining))
            return Results.Problem(detail: "Bạn đã dùng hết số câu hỏi hôm nay. Hãy quay lại vào ngày mai" + (signedIn ? "." : " hoặc đăng nhập để hỏi nhiều hơn."),
                statusCode: StatusCodes.Status429TooManyRequests);

        var watch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            (ChatReply reply, int tokens) = await orchestrator.RunAsync(request, remaining, ct);
            quota.AddTokens(tokens);
            // Counts and timing only: what people asked is not kept anywhere.
            log.LogInformation("Chat answered: signedIn={SignedIn} turns={Turns} tokens={Tokens} ms={Ms}", signedIn, request.Messages!.Count, tokens, watch.ElapsedMilliseconds);
            return Results.Ok(reply);
        }
        catch (ChatUpstreamException ex)
        {
            quota.Refund(subject);
            log.LogWarning("Chat failed upstream: {Message}", ex.Message);
            return Results.Problem(detail: "Trợ lý đang bận, bạn thử lại sau ít phút nhé.", statusCode: StatusCodes.Status502BadGateway);
        }
    }
}
