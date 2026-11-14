using System.Security.Claims;
using System.Text;
using Hue_travel.Api.Audit;
using Hue_travel.Api.Data;
using Npgsql;

namespace Hue_travel.Api.Feeds;

/// <summary>Settings of "Khoảnh khắc" (section "Feed"). It is OFF until Feed__Enabled=true, so a server without the feature still starts.</summary>
public sealed class FeedOptions
{
    public const string SectionName = "Feed";

    public bool Enabled { get; set; } = false;
    public int NewAccountHours { get; set; } = 24;          // younger accounts, or ones without enough approved posts, are "new"
    public int ApprovedPostsToTrust { get; set; } = 2;
    public int PostsPerDayNew { get; set; } = 3;
    public int PostsPerDay { get; set; } = 15;
    public int CommentsPerDay { get; set; } = 100;
    public int MediaPerDay { get; set; } = 30;
    public int MaxMediaPerPost { get; set; } = 4;
    public int MaxOrphanMedia { get; set; } = 8;
    public int AutoHideAfterReports { get; set; } = 3;      // different people reporting before a post hides itself, waiting for an admin
    public int MaxOpenReportsPerUser { get; set; } = 20;

    /// <summary>A post containing one of these goes to review instead of being published (case-insensitive).</summary>
    public string[] HoldWords { get; set; } = [];
}

/// <summary>The rules that are the same in every endpoint: text cleaning, who counts as trusted, cursors, the error replies.</summary>
public static class FeedRules
{
    public const int MaxBody = 2000;
    public const int MaxComment = 1000;
    public const int MaxRoutePlaces = 12;

    private static IResult Problem(string detail, int status) => Results.Problem(detail: detail, statusCode: status);

    public static IResult Bad(string detail) => Problem(detail, StatusCodes.Status400BadRequest);

    public static IResult Forbidden(string detail) => Problem(detail, StatusCodes.Status403Forbidden);

    public static IResult NotFound(string detail) => Problem(detail, StatusCodes.Status404NotFound);

    public static IResult TooMany(string detail) => Problem(detail, StatusCodes.Status429TooManyRequests);

    /// <summary>
    /// Plain text only: control characters are dropped, line breaks become \n (at most two in a row), ends are trimmed.
    /// The result is never treated as HTML anywhere.
    /// </summary>
    public static string CleanText(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        var sb = new StringBuilder(text.Length);
        int breaks = 0;
        foreach (char c in text.Replace("\r\n", "\n").Replace('\r', '\n'))
        {
            if (c == '\n')
            {
                if (++breaks <= 2) sb.Append('\n');
                continue;
            }
            if (char.IsControl(c) || c is '​' or '‎' or '‏' or '‪' or '‫' or '‬' or '‭' or '‮' or '﻿') continue;
            breaks = 0;
            sb.Append(c);
        }
        return sb.ToString().Trim();
    }

    public static bool ShouldHold(string text, FeedOptions options) =>
        options.HoldWords.Any(w => !string.IsNullOrWhiteSpace(w) && text.Contains(w.Trim(), StringComparison.OrdinalIgnoreCase));

    public static long CurrentUserId(ClaimsPrincipal user) => user.Identity?.IsAuthenticated == true ? user.UserId() : 0;

    /// <summary>Admins and owners are trusted; others after the account is old enough and enough of their posts were approved by an admin.</summary>
    public static async Task<bool> IsTrustedAsync(NpgsqlDataSource db, ClaimsPrincipal user, FeedOptions options, CancellationToken ct)
    {
        if (user.IsInRole("admin") || user.IsInRole("owner")) return true;
        long id = user.UserId();
        bool old = await db.ExistsAsync("select created_at <= now() - make_interval(hours => @h) from app_user where id = @u", ct, ("h", options.NewAccountHours), ("u", id));
        if (!old) return false;
        long approved = (long)(await db.ScalarAsync("select count(*) from post where author_id = @u and approved_by is not null and status in ('visible', 'hidden')", ct, ("u", id)))!;
        return approved >= options.ApprovedPostsToTrust;
    }

    public static Task<bool> HasAcceptedTermsAsync(NpgsqlDataSource db, long userId, CancellationToken ct) =>
        db.ExistsAsync("select feed_terms_at is not null from app_user where id = @u", ct, ("u", userId));

    public const string TermsRequired = "Hãy đồng ý quy tắc cộng đồng trước khi dùng Khoảnh khắc.";

    public static string CursorOf(DateTimeOffset createdAt, long id) =>
        Convert.ToBase64String(Encoding.ASCII.GetBytes(createdAt.UtcTicks + ":" + id));

    /// <summary>Reads a cursor made by <see cref="CursorOf"/>; false when it is empty or not one of ours.</summary>
    public static bool TryReadCursor(string? cursor, out DateTimeOffset createdAt, out long id)
    {
        createdAt = default;
        id = 0;
        if (string.IsNullOrEmpty(cursor)) return false;
        try
        {
            string[] parts = Encoding.ASCII.GetString(Convert.FromBase64String(cursor)).Split(':');
            if (parts.Length != 2 || !long.TryParse(parts[0], out long ticks) || !long.TryParse(parts[1], out id)) return false;
            if (ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks) return false;
            createdAt = new DateTimeOffset(ticks, TimeSpan.Zero);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
