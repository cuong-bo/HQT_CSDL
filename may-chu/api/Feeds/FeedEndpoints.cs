using System.Security.Claims;
using System.Text.Json;
using Hue_travel.Api.Audit;
using Hue_travel.Api.Data;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Hue_travel.Api.Feeds;

public sealed record CreatePostRequest(string? Body, string? PoiId, string[]? Route, long[]? MediaIds);
public sealed record CommentRequest(string? Body);
public sealed record ReportRequest(string? Kind, string? Content);

public sealed record FeedAuthor(long Id, string Name);
public sealed record FeedPlace(string Id, string Name);
public sealed record FeedMedia(long Id, int Width, int Height);
public sealed record FeedComment(long Id, FeedAuthor Author, string Body, DateTimeOffset CreatedAt, bool Mine);

public sealed record FeedPost(
    long Id, DateTimeOffset CreatedAt, FeedAuthor Author, string Body, FeedPlace? Poi, JsonElement? Route, IReadOnlyList<FeedMedia> Media,
    int LikeCount, int CommentCount, bool LikedByMe, bool Mine, string Status);

/// <summary>"Khoảnh khắc": the community feed. Posts of text, pictures, a place or a shared route; likes, comments, reports.</summary>
public static class FeedEndpoints
{
    public const int PageSize = 20;
    private const int CommentPage = 30;
    private static readonly string[] ReportKinds = ["spam", "inappropriate", "wrong_info", "privacy", "other"];

    /// <summary>Every endpoint of the group answers 503 until Feed:Enabled is true.</summary>
    public static TBuilder RequireFeed<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder =>
        builder.AddEndpointFilter(async (context, next) =>
            context.HttpContext.RequestServices.GetRequiredService<IOptions<FeedOptions>>().Value.Enabled
                ? await next(context)
                : Results.Problem(detail: "Tính năng này chưa được bật.", statusCode: StatusCodes.Status503ServiceUnavailable));

    public static void MapFeedEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder open = app.MapGroup("/v1").RequireFeed();
        open.MapGet("/feed", ListAsync);
        open.MapGet("/feed/new-count", NewCountAsync);
        open.MapGet("/posts/{id:long}", GetAsync);
        open.MapGet("/posts/{id:long}/comments", ListCommentsAsync);

        RouteGroupBuilder signedIn = app.MapGroup("/v1").RequireFeed().RequireAuthorization();
        signedIn.MapGet("/me/feed", MyStatusAsync);
        signedIn.MapPost("/me/feed-terms", AcceptTermsAsync);
        signedIn.MapGet("/me/posts", MyPostsAsync);
        signedIn.MapPost("/posts", CreateAsync).RequireRateLimiting("feed");
        signedIn.MapDelete("/posts/{id:long}", DeleteAsync);
        signedIn.MapPut("/posts/{id:long}/like", LikeAsync).RequireRateLimiting("feed");
        signedIn.MapDelete("/posts/{id:long}/like", UnlikeAsync).RequireRateLimiting("feed");
        signedIn.MapPost("/posts/{id:long}/comments", CommentAsync).RequireRateLimiting("feed");
        signedIn.MapDelete("/comments/{id:long}", DeleteCommentAsync);
        signedIn.MapPost("/posts/{id:long}/reports", ReportPostAsync).RequireRateLimiting("feed");
        signedIn.MapPost("/comments/{id:long}/reports", ReportCommentAsync).RequireRateLimiting("feed");
    }

    // ------------------------------------------------------------ reading

    private sealed record Row(
        long Id, DateTimeOffset CreatedAt, long AuthorId, string AuthorName, string Body, string? PoiId, string? PoiName, string? Route,
        int Likes, int Comments, string Status, bool Liked);

    private const string PostColumns =
        "select p.id, p.created_at, p.author_id, u.display_name, p.body, p.poi_id, poi.ten, p.shared_route::text, p.like_count, p.comment_count, p.status, " +
        "exists (select 1 from post_like l where l.post_id = p.id and l.user_id = @me) " +
        "from post p join app_user u on u.id = p.author_id left join poi on poi.id = p.poi_id ";

    /// <summary>Runs a query that selects <see cref="PostColumns"/> and attaches the pictures of all the posts with one more query (not one per post).</summary>
    public static async Task<List<FeedPost>> QueryPostsAsync(NpgsqlDataSource db, string tail, long me, CancellationToken ct, params (string Name, object? Value)[] args)
    {
        var all = new List<(string, object?)> { ("me", me) };
        all.AddRange(args);
        List<Row> rows = await db.ListAsync(PostColumns + tail,
            r => new Row(r.GetInt64(0), r.GetFieldValue<DateTimeOffset>(1), r.GetInt64(2), r.GetString(3), r.GetString(4), r.Text(5), r.Text(6), r.Text(7),
                r.GetInt32(8), r.GetInt32(9), r.GetString(10), r.GetBoolean(11)), ct, all.ToArray());

        var media = new Dictionary<long, List<FeedMedia>>();
        if (rows.Count > 0)
        {
            await using NpgsqlCommand cmd = db.Command(
                "select post_id, id, width, height from post_media where post_id = any(@ids) order by post_id, position, id", [("ids", rows.Select(r => r.Id).ToArray())]);
            await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                long postId = reader.GetInt64(0);
                if (!media.TryGetValue(postId, out List<FeedMedia>? list)) media[postId] = list = [];
                list.Add(new FeedMedia(reader.GetInt64(1), reader.GetInt32(2), reader.GetInt32(3)));
            }
        }

        return rows.Select(r => new FeedPost(
            r.Id, r.CreatedAt, new FeedAuthor(r.AuthorId, r.AuthorName), r.Body,
            r.PoiId == null ? null : new FeedPlace(r.PoiId, r.PoiName ?? r.PoiId),
            r.Route == null ? null : JsonDocument.Parse(r.Route).RootElement.Clone(),
            media.TryGetValue(r.Id, out List<FeedMedia>? m) ? m : [],
            r.Likes, r.Comments, r.Liked, r.AuthorId == me, r.Status)).ToList();
    }

    private static async Task<IResult> ListAsync(string? cursor, string? poi, int? limit, ClaimsPrincipal user, NpgsqlDataSource db, CancellationToken ct)
    {
        int n = Math.Clamp(limit ?? PageSize, 1, 50);
        var args = new List<(string, object?)> { ("n", n + 1) };
        string where = "where p.status = 'visible' ";
        if (!string.IsNullOrEmpty(poi)) { where += "and p.poi_id = @poi "; args.Add(("poi", poi)); }
        if (!string.IsNullOrEmpty(cursor))
        {
            if (!FeedRules.TryReadCursor(cursor, out DateTimeOffset at, out long afterId)) return FeedRules.Bad("Con trỏ phân trang không hợp lệ.");
            where += "and (p.created_at, p.id) < (@cc, @ci) ";
            args.Add(("cc", at));
            args.Add(("ci", afterId));
        }

        List<FeedPost> page = await QueryPostsAsync(db, where + "order by p.created_at desc, p.id desc limit @n", FeedRules.CurrentUserId(user), ct, args.ToArray());
        bool more = page.Count > n;
        if (more) page.RemoveAt(page.Count - 1);
        string? next = more ? FeedRules.CursorOf(page[^1].CreatedAt, page[^1].Id) : null;
        return Results.Ok(new { items = page, next });
    }

    private static async Task<IResult> NewCountAsync(long? after, NpgsqlDataSource db, CancellationToken ct) =>
        Results.Ok(new { count = (long)(await db.ScalarAsync("select count(*) from post where status = 'visible' and id > @a", ct, ("a", after ?? long.MaxValue)))! });

    private static async Task<IResult> GetAsync(long id, ClaimsPrincipal user, NpgsqlDataSource db, CancellationToken ct)
    {
        long me = FeedRules.CurrentUserId(user);
        List<FeedPost> found = await QueryPostsAsync(db, "where p.id = @i", me, ct, ("i", id));
        if (found.Count == 0) return FeedRules.NotFound("Không tìm thấy bài viết.");
        FeedPost post = found[0];
        if (post.Status != "visible" && !post.Mine && !user.IsInRole("admin")) return FeedRules.NotFound("Không tìm thấy bài viết.");
        return Results.Ok(post);
    }

    private static async Task<IResult> MyPostsAsync(ClaimsPrincipal user, NpgsqlDataSource db, CancellationToken ct)
    {
        long me = user.UserId();
        return Results.Ok(await QueryPostsAsync(db, "where p.author_id = @me and p.status <> 'removed' order by p.created_at desc, p.id desc limit 50", me, ct));
    }

    private static async Task<IResult> MyStatusAsync(ClaimsPrincipal user, NpgsqlDataSource db, IOptions<FeedOptions> options, CancellationToken ct) =>
        Results.Ok(new
        {
            termsAccepted = true,   // there is no confirmation to give any more; kept so that apps of 1.2.0 do not ask for one
            trusted = await FeedRules.IsTrustedAsync(db, user, options.Value, ct),
            maxMediaPerPost = options.Value.MaxMediaPerPost,
        });

    private static async Task<IResult> AcceptTermsAsync(ClaimsPrincipal user, NpgsqlDataSource db, CancellationToken ct)
    {
        await db.ExecAsync("update app_user set feed_terms_at = coalesce(feed_terms_at, now()) where id = @u", ct, ("u", user.UserId()));
        return Results.NoContent();
    }

    // ------------------------------------------------------------ posting

    private static async Task<IResult> CreateAsync(
        CreatePostRequest request, ClaimsPrincipal user, NpgsqlDataSource db, IOptions<FeedOptions> options, CancellationToken ct)
    {
        FeedOptions feed = options.Value;
        long me = user.UserId();

        string body = FeedRules.CleanText(request.Body);
        if (body.Length > FeedRules.MaxBody) return FeedRules.Bad($"Bài viết tối đa {FeedRules.MaxBody} ký tự.");

        long[] mediaIds = request.MediaIds?.Distinct().ToArray() ?? [];
        if (mediaIds.Length > feed.MaxMediaPerPost) return FeedRules.Bad($"Mỗi bài có tối đa {feed.MaxMediaPerPost} ảnh.");

        string? poiId = string.IsNullOrWhiteSpace(request.PoiId) ? null : request.PoiId.Trim();
        if (poiId != null && !await db.ExistsAsync("select exists (select 1 from poi where id = @p and deleted_at is null)", ct, ("p", poiId)))
            return FeedRules.Bad("Địa điểm gắn vào bài không tồn tại.");

        string? routeJson = null;
        if (request.Route is { Length: > 0 })
        {
            string[] ids = request.Route.Select(s => s?.Trim() ?? "").ToArray();
            if (ids.Length < 2 || ids.Length > FeedRules.MaxRoutePlaces || ids.Distinct().Count() != ids.Length || ids.Any(s => s.Length == 0))
                return FeedRules.Bad($"Lộ trình cần từ 2 đến {FeedRules.MaxRoutePlaces} địa điểm khác nhau.");
            Dictionary<string, string> names = (await db.ListAsync("select id, ten from poi where id = any(@ids) and deleted_at is null",
                r => (Id: r.GetString(0), Name: r.GetString(1)), ct, ("ids", ids))).ToDictionary(t => t.Id, t => t.Name);
            if (names.Count != ids.Length) return FeedRules.Bad("Lộ trình có địa điểm không tồn tại.");
            routeJson = JsonSerializer.Serialize(ids.Select(i => new { poiId = i, name = names[i] }));   // names come from our table, not from the client
        }

        if (body.Length == 0 && mediaIds.Length == 0 && routeJson == null) return FeedRules.Bad("Bài viết cần có chữ, ảnh hoặc lộ trình.");

        bool trusted = await FeedRules.IsTrustedAsync(db, user, feed, ct);
        long today = (long)(await db.ScalarAsync("select count(*) from post where author_id = @u and created_at > now() - interval '1 day'", ct, ("u", me)))!;
        if (today >= (trusted ? feed.PostsPerDay : feed.PostsPerDayNew)) return FeedRules.TooMany("Hôm nay bạn đã đăng đủ số bài cho phép. Hãy thử lại vào ngày mai.");

        string status = trusted && !FeedRules.ShouldHold(body, feed) ? "visible" : "pending";

        long id;
        await using (NpgsqlConnection connection = await db.OpenConnectionAsync(ct))
        await using (NpgsqlTransaction tx = await connection.BeginTransactionAsync(ct))
        {
            await using (var insert = new NpgsqlCommand(
                "insert into post (author_id, body, poi_id, shared_route, status) values (@u, @b, @p, @r::jsonb, @s) returning id", connection, tx))
            {
                Sql.Fill(insert, [("u", me), ("b", body), ("p", poiId), ("r", routeJson), ("s", status)]);
                id = (long)(await insert.ExecuteScalarAsync(ct))!;
            }

            for (int i = 0; i < mediaIds.Length; i++)
            {
                await using var attach = new NpgsqlCommand(
                    "update post_media set post_id = @p, position = @o where id = @m and uploaded_by = @u and post_id is null", connection, tx);
                Sql.Fill(attach, [("p", id), ("o", (short)i), ("m", mediaIds[i]), ("u", me)]);
                if (await attach.ExecuteNonQueryAsync(ct) != 1)
                {
                    await tx.RollbackAsync(ct);
                    return FeedRules.Bad("Có ảnh không hợp lệ hoặc đã được dùng. Hãy tải ảnh lại.");
                }
            }
            await tx.CommitAsync(ct);
        }

        await db.RecordAsync(me, "post.create", "post", id.ToString(), null, new { status, media = mediaIds.Length, hasRoute = routeJson != null }, ct);
        return Results.Json(new { id, status }, statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> DeleteAsync(long id, ClaimsPrincipal user, NpgsqlDataSource db, CancellationToken ct)
    {
        bool admin = user.IsInRole("admin");
        int rows = await db.ExecAsync("delete from post where id = @i and (author_id = @u or @a)", ct, ("i", id), ("u", user.UserId()), ("a", admin));
        if (rows == 0) return FeedRules.NotFound("Không tìm thấy bài viết của bạn.");
        await db.RecordAsync(user.UserId(), "post.delete", "post", id.ToString(), null, new { byAdmin = admin }, ct);
        return Results.NoContent();
    }

    // ------------------------------------------------------------ likes and comments

    private static Task<bool> IsVisibleAsync(NpgsqlDataSource db, long postId, CancellationToken ct) =>
        db.ExistsAsync("select exists (select 1 from post where id = @i and status = 'visible')", ct, ("i", postId));

    private static async Task<IResult> LikeAsync(long id, ClaimsPrincipal user, NpgsqlDataSource db, CancellationToken ct)
    {
        if (!await IsVisibleAsync(db, id, ct)) return FeedRules.NotFound("Không tìm thấy bài viết.");
        await db.ExecAsync("insert into post_like (post_id, user_id) values (@p, @u) on conflict do nothing", ct, ("p", id), ("u", user.UserId()));
        return Results.NoContent();
    }

    private static async Task<IResult> UnlikeAsync(long id, ClaimsPrincipal user, NpgsqlDataSource db, CancellationToken ct)
    {
        await db.ExecAsync("delete from post_like where post_id = @p and user_id = @u", ct, ("p", id), ("u", user.UserId()));
        return Results.NoContent();
    }

    private static async Task<IResult> ListCommentsAsync(long id, long? after, ClaimsPrincipal user, NpgsqlDataSource db, CancellationToken ct)
    {
        if (!await IsVisibleAsync(db, id, ct)) return FeedRules.NotFound("Không tìm thấy bài viết.");
        long me = FeedRules.CurrentUserId(user);
        List<FeedComment> page = await db.ListAsync(
            "select c.id, c.author_id, u.display_name, c.body, c.created_at from post_comment c join app_user u on u.id = c.author_id " +
            "where c.post_id = @p and c.status = 'visible' and c.id > @a order by c.id limit @n",
            r => new FeedComment(r.GetInt64(0), new FeedAuthor(r.GetInt64(1), r.GetString(2)), r.GetString(3), r.GetFieldValue<DateTimeOffset>(4), r.GetInt64(1) == me),
            ct, ("p", id), ("a", after ?? 0L), ("n", CommentPage + 1));
        bool more = page.Count > CommentPage;
        if (more) page.RemoveAt(page.Count - 1);
        long? next = more ? page[^1].Id : null;
        return Results.Ok(new { items = page, next });
    }

    private static async Task<IResult> CommentAsync(long id, CommentRequest request, ClaimsPrincipal user, NpgsqlDataSource db, IOptions<FeedOptions> options, CancellationToken ct)
    {
        long me = user.UserId();
        string body = FeedRules.CleanText(request.Body);
        if (body.Length == 0) return FeedRules.Bad("Bình luận không được để trống.");
        if (body.Length > FeedRules.MaxComment) return FeedRules.Bad($"Bình luận tối đa {FeedRules.MaxComment} ký tự.");
        if (!await IsVisibleAsync(db, id, ct)) return FeedRules.NotFound("Không tìm thấy bài viết.");
        long today = (long)(await db.ScalarAsync("select count(*) from post_comment where author_id = @u and created_at > now() - interval '1 day'", ct, ("u", me)))!;
        if (today >= options.Value.CommentsPerDay) return FeedRules.TooMany("Hôm nay bạn đã bình luận quá nhiều. Hãy thử lại vào ngày mai.");

        long commentId = (long)(await db.ScalarAsync("insert into post_comment (post_id, author_id, body) values (@p, @u, @b) returning id", ct, ("p", id), ("u", me), ("b", body)))!;
        return Results.Json(new { id = commentId, body }, statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> DeleteCommentAsync(long id, ClaimsPrincipal user, NpgsqlDataSource db, CancellationToken ct)
    {
        int rows = await db.ExecAsync(
            "delete from post_comment c using post p where c.id = @i and p.id = c.post_id and (c.author_id = @u or p.author_id = @u or @a)", ct,
            ("i", id), ("u", user.UserId()), ("a", user.IsInRole("admin")));
        if (rows == 0) return FeedRules.NotFound("Không tìm thấy bình luận.");
        await db.RecordAsync(user.UserId(), "comment.delete", "post_comment", id.ToString(), null, null, ct);
        return Results.NoContent();
    }

    // ------------------------------------------------------------ reports

    private static Task<IResult> ReportPostAsync(long id, ReportRequest request, ClaimsPrincipal user, NpgsqlDataSource db, IOptions<FeedOptions> options, CancellationToken ct) =>
        ReportAsync(isPost: true, id, request, user, db, options.Value, ct);

    private static Task<IResult> ReportCommentAsync(long id, ReportRequest request, ClaimsPrincipal user, NpgsqlDataSource db, IOptions<FeedOptions> options, CancellationToken ct) =>
        ReportAsync(isPost: false, id, request, user, db, options.Value, ct);

    private static async Task<IResult> ReportAsync(bool isPost, long id, ReportRequest request, ClaimsPrincipal user, NpgsqlDataSource db, FeedOptions feed, CancellationToken ct)
    {
        long me = user.UserId();
        if (request.Kind == null || !ReportKinds.Contains(request.Kind)) return FeedRules.Bad("Loại báo cáo không hợp lệ.");
        string? note = FeedRules.CleanText(request.Content);
        if (note!.Length > 1000) return FeedRules.Bad("Nội dung báo cáo tối đa 1000 ký tự.");
        if (note.Length == 0) note = null;

        string table = isPost ? "post" : "post_comment", column = isPost ? "post_id" : "comment_id";
        long? authorId = (long?)await db.ScalarAsync($"select author_id from {table} where id = @i and status = 'visible'", ct, ("i", id));
        if (authorId == null) return FeedRules.NotFound(isPost ? "Không tìm thấy bài viết." : "Không tìm thấy bình luận.");
        if (authorId == me) return FeedRules.Bad("Bạn không thể báo cáo nội dung của chính mình.");

        long open = (long)(await db.ScalarAsync("select count(*) from feed_report where user_id = @u and status = 'open'", ct, ("u", me)))!;
        if (open >= feed.MaxOpenReportsPerUser) return FeedRules.TooMany("Bạn có quá nhiều báo cáo đang chờ xử lý.");

        await db.ExecAsync($"insert into feed_report (user_id, {column}, kind, content) values (@u, @i, @k, @c) on conflict do nothing", ct,
            ("u", me), ("i", id), ("k", request.Kind), ("c", note));

        long people = (long)(await db.ScalarAsync($"select count(distinct user_id) from feed_report where {column} = @i and status = 'open'", ct, ("i", id)))!;
        if (people >= feed.AutoHideAfterReports)
        {
            int hidden = await db.ExecAsync($"update {table} set status = 'hidden' where id = @i and status = 'visible'", ct, ("i", id));
            if (hidden > 0) await db.RecordAsync(null, isPost ? "post.autohide" : "comment.autohide", table, id.ToString(), null, new { reporters = people }, ct);
        }
        return Results.NoContent();
    }
}
