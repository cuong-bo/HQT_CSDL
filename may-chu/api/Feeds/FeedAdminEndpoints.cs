using System.Security.Claims;
using Hue_travel.Api.Audit;
using Hue_travel.Api.Data;
using Npgsql;

namespace Hue_travel.Api.Feeds;

public sealed record PostDecision(bool Approve, string? Note);
public sealed record ReportResolution(string? Action);
public sealed record PostStatusChange(string? Status);

/// <summary>What an admin does with "Khoảnh khắc": approve posts of new accounts, handle reports, restore hidden posts.</summary>
public static class FeedAdminEndpoints
{
    public static void MapFeedAdminEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder admin = app.MapGroup("/v1/admin").RequireFeed().RequireAuthorization("admin");
        admin.MapGet("/feed/pending", PendingAsync);
        admin.MapPost("/posts/{id:long}/decision", DecideAsync);
        admin.MapGet("/feed/reports", ReportsAsync);
        admin.MapPost("/feed/reports/{id:long}/resolve", ResolveAsync);
        admin.MapPut("/posts/{id:long}/status", SetStatusAsync);
    }

    private static async Task<IResult> PendingAsync(ClaimsPrincipal admin, NpgsqlDataSource db, CancellationToken ct) =>
        Results.Ok(await FeedEndpoints.QueryPostsAsync(db, "where p.status = 'pending' order by p.created_at limit 50", admin.UserId(), ct));

    private static async Task<IResult> DecideAsync(long id, PostDecision decision, ClaimsPrincipal admin, NpgsqlDataSource db, CancellationToken ct)
    {
        int rows = await db.ExecAsync(
            "update post set status = @s, approved_by = case when @ok then @a end where id = @i and status = 'pending'", ct,
            ("s", decision.Approve ? "visible" : "removed"), ("ok", decision.Approve), ("a", admin.UserId()), ("i", id));
        if (rows == 0) return FeedRules.NotFound("Không có bài đang chờ duyệt.");
        await db.RecordAsync(admin.UserId(), decision.Approve ? "post.approve" : "post.reject", "post", id.ToString(), null, new { decision.Note }, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ReportsAsync(NpgsqlDataSource db, CancellationToken ct) =>
        Results.Ok(await db.ListAsync(
            "select r.id, r.kind, r.content, r.created_at, ru.display_name, r.post_id, r.comment_id, " +
            "coalesce(p.body, c.body), coalesce(p.status, c.status), coalesce(pu.display_name, cu.display_name) " +
            "from feed_report r join app_user ru on ru.id = r.user_id " +
            "left join post p on p.id = r.post_id left join app_user pu on pu.id = p.author_id " +
            "left join post_comment c on c.id = r.comment_id left join app_user cu on cu.id = c.author_id " +
            "where r.status = 'open' order by r.created_at limit 50",
            r => new
            {
                id = r.GetInt64(0), kind = r.GetString(1), content = r.Text(2), createdAt = r.GetFieldValue<DateTimeOffset>(3), reporter = r.GetString(4),
                postId = r.IsDBNull(5) ? (long?)null : r.GetInt64(5), commentId = r.IsDBNull(6) ? (long?)null : r.GetInt64(6),
                text = r.Text(7), targetStatus = r.Text(8), author = r.Text(9),
            }, ct));

    /// <summary>action: dismiss (the report is wrong), hide (kept but not shown), remove (gone for good). All open reports of the same thing close together.</summary>
    private static async Task<IResult> ResolveAsync(long id, ReportResolution resolution, ClaimsPrincipal admin, NpgsqlDataSource db, CancellationToken ct)
    {
        string? action = resolution.Action;
        if (action is not ("dismiss" or "hide" or "remove")) return FeedRules.Bad("Hành động không hợp lệ (dismiss, hide hoặc remove).");

        long? postId = null, commentId = null;
        await using (NpgsqlCommand cmd = db.Command("select post_id, comment_id from feed_report where id = @i and status = 'open'", [("i", id)]))
        await using (NpgsqlDataReader r = await cmd.ExecuteReaderAsync(ct))
        {
            if (!await r.ReadAsync(ct)) return FeedRules.NotFound("Không có báo cáo đang chờ.");
            if (!r.IsDBNull(0)) postId = r.GetInt64(0);
            if (!r.IsDBNull(1)) commentId = r.GetInt64(1);
        }

        if (action != "dismiss")
        {
            if (postId != null) await db.ExecAsync("update post set status = @s where id = @i", ct, ("s", action == "remove" ? "removed" : "hidden"), ("i", postId));
            if (commentId != null)
            {
                if (action == "remove") await db.ExecAsync("delete from post_comment where id = @i", ct, ("i", commentId));
                else await db.ExecAsync("update post_comment set status = 'hidden' where id = @i", ct, ("i", commentId));
            }
        }

        // The comment may be gone (its reports go with it), so close by target and then by id.
        await db.ExecAsync(
            "update feed_report set status = 'resolved', handled_by = @a where status = 'open' and (id = @i or (post_id is not null and post_id = @p) or (comment_id is not null and comment_id = @c))", ct,
            ("a", admin.UserId()), ("i", id), ("p", postId), ("c", commentId));
        await db.RecordAsync(admin.UserId(), "feed.report.resolve", "feed_report", id.ToString(), null, new { action, postId, commentId }, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> SetStatusAsync(long id, PostStatusChange change, ClaimsPrincipal admin, NpgsqlDataSource db, CancellationToken ct)
    {
        if (change.Status is not ("visible" or "hidden" or "removed")) return FeedRules.Bad("Trạng thái không hợp lệ (visible, hidden hoặc removed).");
        int rows = await db.ExecAsync("update post set status = @s where id = @i", ct, ("s", change.Status), ("i", id));
        if (rows == 0) return FeedRules.NotFound("Không tìm thấy bài viết.");
        await db.RecordAsync(admin.UserId(), change.Status == "visible" ? "post.restore" : change.Status == "hidden" ? "post.hide" : "post.remove", "post", id.ToString(), null, null, ct);
        return Results.NoContent();
    }
}
