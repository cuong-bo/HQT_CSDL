using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using SkiaSharp;

namespace Hue_travel.Api.Tests;

/// <summary>"Khoảnh khắc" end to end against a real database: terms, posting, review, likes, comments, reports, pictures, pages.</summary>
[Collection("api")]
public class FeedDbTests : IDisposable
{
    private const string Password = "matkhau-dai-123";
    private readonly WebApplicationFactory<Program> factory;

    public FeedDbTests(ApiFixture fixture)
    {
        factory = fixture.Factory.WithWebHostBuilder(b => b.UseSetting("Feed:Enabled", "true"));
    }

    public void Dispose() => factory.Dispose();

    private static string NewName() => "f" + Guid.NewGuid().ToString("N")[..12];

    private static async Task<JsonElement> Json(HttpResponseMessage response) => (await response.Content.ReadFromJsonAsync<JsonElement>())!;

    private static async Task Sql(string sql, params (string, object)[] args)
    {
        await using NpgsqlDataSource db = NpgsqlDataSource.Create(ApiFixture.ConnectionString!);
        await using NpgsqlCommand cmd = db.CreateCommand(sql);
        foreach ((string name, object value) in args) cmd.Parameters.AddWithValue(name, value);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task<(HttpClient Http, long Id, string Name)> UserAsync(string role = "user", bool terms = false)
    {
        string name = NewName();
        HttpClient http = factory.CreateClient();
        HttpResponseMessage registered = await http.PostAsJsonAsync("/v1/auth/register", new { username = name, password = Password });
        Assert.Equal(HttpStatusCode.Created, registered.StatusCode);
        long id = (await Json(registered)).GetProperty("user").GetProperty("id").GetInt64();
        if (role != "user") await Sql("update app_user set role = @r where id = @i", ("r", role), ("i", id));
        if (terms) await Sql("update app_user set feed_terms_at = now() where id = @i", ("i", id));
        JsonElement login = await Json(await http.PostAsJsonAsync("/v1/auth/login", new { username = name, password = Password }));
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.GetProperty("accessToken").GetString());
        return (http, id, name);
    }

    private static async Task<string> PlaceAsync(HttpClient admin)
    {
        string id = "F" + Guid.NewGuid().ToString("N")[..12];
        HttpResponseMessage response = await admin.PostAsJsonAsync("/v1/admin/pois", new { id, group = "service", name = "Điểm thử " + id, lon = 107.5786, lat = 16.4698 });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return id;
    }

    private static string PngBase64()
    {
        using var bitmap = new SKBitmap(2400, 1600);   // bigger than 1600 px, so the full picture is shrunk and the thumbnail is smaller still
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(new SKColor(30, 120, 200));
            using var paint = new SKPaint { Color = new SKColor(250, 220, 40) };
            for (int i = 0; i < 40; i++) canvas.DrawCircle(60 + i * 55, 80 + (i * 37) % 1400, 40 + i, paint);
        }
        using SKData png = SKImage.FromBitmap(bitmap).Encode(SKEncodedImageFormat.Png, 100);
        return Convert.ToBase64String(png.ToArray());
    }

    private static async Task<long> PostAsync(HttpClient http, object body, HttpStatusCode expected = HttpStatusCode.Created)
    {
        HttpResponseMessage response = await http.PostAsJsonAsync("/v1/posts", body);
        Assert.Equal(expected, response.StatusCode);
        return expected == HttpStatusCode.Created ? (await Json(response)).GetProperty("id").GetInt64() : 0;
    }

    private static async Task<JsonElement> FeedOfAsync(HttpClient http, string poi, string? cursor = null, int limit = 20) =>
        await Json(await http.GetAsync($"/v1/feed?poi={poi}&limit={limit}" + (cursor == null ? "" : "&cursor=" + Uri.EscapeDataString(cursor))));

    [RequiresDbFact]
    public async Task PostingNeedsTheRules_ANewAccountWaitsForReview_ThenTheAdminApproves()
    {
        (HttpClient admin, _, _) = await UserAsync("admin");
        string place = await PlaceAsync(admin);
        (HttpClient user, _, _) = await UserAsync();

        Assert.Equal(HttpStatusCode.Forbidden, (await user.PostAsJsonAsync("/v1/posts", new { body = "chưa đồng ý quy tắc", poiId = place })).StatusCode);
        JsonElement me = await Json(await user.GetAsync("/v1/me/feed"));
        Assert.False(me.GetProperty("termsAccepted").GetBoolean());
        Assert.False(me.GetProperty("trusted").GetBoolean());

        Assert.Equal(HttpStatusCode.NoContent, (await user.PostAsync("/v1/me/feed-terms", null)).StatusCode);
        long id = await PostAsync(user, new { body = "  Sáng sớm ở Đại Nội \u0007 ", poiId = place });

        JsonElement mine = (await Json(await user.GetAsync("/v1/me/posts")))[0];
        Assert.Equal("pending", mine.GetProperty("status").GetString());
        Assert.Equal("Sáng sớm ở Đại Nội", mine.GetProperty("body").GetString());   // cleaned: control characters and edge spaces gone

        Assert.Equal(0, (await FeedOfAsync(factory.CreateClient(), place)).GetProperty("items").GetArrayLength());   // nobody else sees it yet
        Assert.Equal(HttpStatusCode.NotFound, (await factory.CreateClient().GetAsync($"/v1/posts/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await user.GetAsync($"/v1/posts/{id}")).StatusCode);                          // but its author does

        JsonElement pending = await Json(await admin.GetAsync("/v1/admin/feed/pending"));
        Assert.Contains(pending.EnumerateArray(), p => p.GetProperty("id").GetInt64() == id);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsJsonAsync($"/v1/admin/posts/{id}/decision", new { approve = true })).StatusCode);

        JsonElement feed = await FeedOfAsync(factory.CreateClient(), place);
        Assert.Equal(1, feed.GetProperty("items").GetArrayLength());
        JsonElement item = feed.GetProperty("items")[0];
        Assert.Equal("visible", item.GetProperty("status").GetString());
        Assert.Equal(place, item.GetProperty("poi").GetProperty("id").GetString());
        Assert.False(item.GetProperty("likedByMe").GetBoolean());
    }

    [RequiresDbFact]
    public async Task ATrustedAuthor_PublishesAtOnce_AndLikesAndCommentsAreCounted()
    {
        (HttpClient admin, _, _) = await UserAsync("admin", terms: true);
        string place = await PlaceAsync(admin);
        long id = await PostAsync(admin, new { body = "Bài của quản trị viên", poiId = place });
        (HttpClient fan, _, _) = await UserAsync(terms: true);

        Assert.Equal(HttpStatusCode.NoContent, (await fan.PutAsync($"/v1/posts/{id}/like", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await fan.PutAsync($"/v1/posts/{id}/like", null)).StatusCode);   // pressing twice changes nothing
        JsonElement liked = (await FeedOfAsync(fan, place)).GetProperty("items")[0];
        Assert.Equal(1, liked.GetProperty("likeCount").GetInt32());
        Assert.True(liked.GetProperty("likedByMe").GetBoolean());

        Assert.Equal(HttpStatusCode.NoContent, (await fan.DeleteAsync($"/v1/posts/{id}/like")).StatusCode);
        Assert.Equal(0, (await FeedOfAsync(fan, place)).GetProperty("items")[0].GetProperty("likeCount").GetInt32());

        Assert.Equal(HttpStatusCode.Created, (await fan.PostAsJsonAsync($"/v1/posts/{id}/comments", new { body = "  Đẹp quá!  " })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await fan.PostAsJsonAsync($"/v1/posts/{id}/comments", new { body = "   " })).StatusCode);
        JsonElement comments = await Json(await factory.CreateClient().GetAsync($"/v1/posts/{id}/comments"));
        Assert.Equal(1, comments.GetProperty("items").GetArrayLength());
        Assert.Equal("Đẹp quá!", comments.GetProperty("items")[0].GetProperty("body").GetString());
        Assert.Equal(1, (await FeedOfAsync(fan, place)).GetProperty("items")[0].GetProperty("commentCount").GetInt32());

        long commentId = comments.GetProperty("items")[0].GetProperty("id").GetInt64();
        (HttpClient stranger, _, _) = await UserAsync(terms: true);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.DeleteAsync($"/v1/comments/{commentId}")).StatusCode);   // not theirs
        Assert.Equal(HttpStatusCode.NoContent, (await fan.DeleteAsync($"/v1/comments/{commentId}")).StatusCode);
        Assert.Equal(0, (await FeedOfAsync(fan, place)).GetProperty("items")[0].GetProperty("commentCount").GetInt32());
    }

    [RequiresDbFact]
    public async Task ThreeDifferentReporters_HideAPost_AndTheAdminCanRestoreIt()
    {
        (HttpClient admin, _, _) = await UserAsync("admin", terms: true);
        string place = await PlaceAsync(admin);
        long id = await PostAsync(admin, new { body = "Nội dung bị báo cáo", poiId = place });

        (HttpClient author, _, _) = await UserAsync("owner", terms: true);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync($"/v1/posts/{id}/reports", new { kind = "spam" })).StatusCode);   // not one's own
        Assert.Equal(HttpStatusCode.BadRequest, (await author.PostAsJsonAsync($"/v1/posts/{id}/reports", new { kind = "nonsense" })).StatusCode);

        for (int i = 0; i < 3; i++)
        {
            (HttpClient reporter, _, _) = await UserAsync(terms: true);
            Assert.Equal(HttpStatusCode.NoContent, (await reporter.PostAsJsonAsync($"/v1/posts/{id}/reports", new { kind = "spam", content = "quảng cáo" })).StatusCode);
            if (i == 0) Assert.Equal(HttpStatusCode.NoContent, (await reporter.PostAsJsonAsync($"/v1/posts/{id}/reports", new { kind = "spam" })).StatusCode);   // the same person again adds nothing
        }

        Assert.Equal(0, (await FeedOfAsync(factory.CreateClient(), place)).GetProperty("items").GetArrayLength());   // hidden by itself after three
        JsonElement reports = await Json(await admin.GetAsync("/v1/admin/feed/reports"));
        JsonElement first = reports.EnumerateArray().First(r => r.GetProperty("postId").ValueKind == JsonValueKind.Number && r.GetProperty("postId").GetInt64() == id);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsJsonAsync($"/v1/admin/feed/reports/{first.GetProperty("id").GetInt64()}/resolve", new { action = "dismiss" })).StatusCode);
        JsonElement after = await Json(await admin.GetAsync("/v1/admin/feed/reports"));
        Assert.DoesNotContain(after.EnumerateArray(), r => r.GetProperty("postId").ValueKind == JsonValueKind.Number && r.GetProperty("postId").GetInt64() == id);   // all three closed together

        Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync($"/v1/admin/posts/{id}/status", new { status = "visible" })).StatusCode);
        Assert.Equal(1, (await FeedOfAsync(factory.CreateClient(), place)).GetProperty("items").GetArrayLength());
    }

    [RequiresDbFact]
    public async Task Pages_FollowTheCursor_WithoutRepeatsOrGaps()
    {
        (HttpClient admin, _, _) = await UserAsync("admin", terms: true);
        string place = await PlaceAsync(admin);
        var ids = new List<long>();
        for (int i = 0; i < 5; i++) ids.Add(await PostAsync(admin, new { body = "Bài " + i, poiId = place }));

        var seen = new List<long>();
        string? cursor = null;
        int pages = 0;
        do
        {
            JsonElement page = await FeedOfAsync(factory.CreateClient(), place, cursor, limit: 2);
            seen.AddRange(page.GetProperty("items").EnumerateArray().Select(p => p.GetProperty("id").GetInt64()));
            cursor = page.GetProperty("next").ValueKind == JsonValueKind.String ? page.GetProperty("next").GetString() : null;
            pages++;
        } while (cursor != null && pages < 10);

        Assert.Equal(3, pages);
        Assert.Equal(ids.OrderByDescending(x => x), seen);   // newest first, each once

        JsonElement none = await Json(await factory.CreateClient().GetAsync($"/v1/feed/new-count?after={ids.Max()}"));
        Assert.True(none.GetProperty("count").GetInt64() >= 0);
    }

    [RequiresDbFact]
    public async Task Pictures_AreShrunk_OnlyPublicOnceThePostIs_AndCannotBeStolen()
    {
        (HttpClient admin, _, _) = await UserAsync("admin", terms: true);
        string place = await PlaceAsync(admin);
        (HttpClient thief, _, _) = await UserAsync(terms: true);

        HttpResponseMessage upload = await admin.PostAsJsonAsync("/v1/media", new { data = PngBase64() });
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
        long media = (await Json(upload)).GetProperty("id").GetInt64();

        Assert.Equal(HttpStatusCode.NotFound, (await factory.CreateClient().GetAsync($"/v1/media/{media}")).StatusCode);   // not in a post yet: private
        Assert.Equal(HttpStatusCode.BadRequest, (await thief.PostAsJsonAsync("/v1/posts", new { body = "ảnh của người khác", mediaIds = new[] { media } })).StatusCode);

        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/v1/media", new { data = "không phải base64!" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/v1/media", new { data = Convert.ToBase64String("không phải ảnh"u8.ToArray()) })).StatusCode);

        long id = await PostAsync(admin, new { body = "Có ảnh", poiId = place, mediaIds = new[] { media } });
        JsonElement item = (await FeedOfAsync(factory.CreateClient(), place)).GetProperty("items").EnumerateArray().First(p => p.GetProperty("id").GetInt64() == id);
        Assert.Equal(media, item.GetProperty("media")[0].GetProperty("id").GetInt64());

        JsonElement thumb = await Json(await factory.CreateClient().GetAsync($"/v1/media/{media}?size=thumb"));
        JsonElement full = await Json(await factory.CreateClient().GetAsync($"/v1/media/{media}?size=full"));
        Assert.Equal("image/jpeg", thumb.GetProperty("contentType").GetString());
        Assert.True(Convert.FromBase64String(thumb.GetProperty("data").GetString()!).Length < Convert.FromBase64String(full.GetProperty("data").GetString()!).Length);

        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/v1/posts", new { body = "dùng lại ảnh", mediaIds = new[] { media } })).StatusCode);   // already in a post
    }

    [RequiresDbFact]
    public async Task ASharedRoute_KeepsNamesFromTheServer_AndRefusesUnknownPlaces()
    {
        (HttpClient admin, _, _) = await UserAsync("admin", terms: true);
        string a = await PlaceAsync(admin), b = await PlaceAsync(admin);

        long id = await PostAsync(admin, new { body = "", route = new[] { a, b }, poiId = a });
        JsonElement item = (await FeedOfAsync(factory.CreateClient(), a)).GetProperty("items").EnumerateArray().First(p => p.GetProperty("id").GetInt64() == id);
        JsonElement route = item.GetProperty("route");
        Assert.Equal(2, route.GetArrayLength());
        Assert.Equal(a, route[0].GetProperty("poiId").GetString());
        Assert.StartsWith("Điểm thử", route[1].GetProperty("name").GetString());

        await PostAsync(admin, new { route = new[] { a, "KHONG-CO" } }, HttpStatusCode.BadRequest);
        await PostAsync(admin, new { route = new[] { a, a } }, HttpStatusCode.BadRequest);
        await PostAsync(admin, new { route = new[] { a } }, HttpStatusCode.BadRequest);
        await PostAsync(admin, new { body = "x", poiId = "KHONG-CO" }, HttpStatusCode.BadRequest);
        await PostAsync(admin, new { body = "   " }, HttpStatusCode.BadRequest);
    }

    [RequiresDbFact]
    public async Task ANewAccount_HasADailyLimit_AndOnlyTheAuthorOrAdminCanDelete()
    {
        (HttpClient user, _, _) = await UserAsync(terms: true);
        for (int i = 0; i < 3; i++) await PostAsync(user, new { body = "Bài " + i });
        await PostAsync(user, new { body = "Bài thứ tư" }, HttpStatusCode.TooManyRequests);

        long id = (await Json(await user.GetAsync("/v1/me/posts")))[0].GetProperty("id").GetInt64();
        (HttpClient other, _, _) = await UserAsync(terms: true);
        Assert.Equal(HttpStatusCode.NotFound, (await other.DeleteAsync($"/v1/posts/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await user.DeleteAsync($"/v1/posts/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await user.GetAsync($"/v1/posts/{id}")).StatusCode);
    }
}
