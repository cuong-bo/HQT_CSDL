using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Hue_travel.Api.Feeds;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Hue_travel.Api.Tests;

// Rules of "Khoảnh khắc" that need no database, plus the on/off switch and the sign-in gate through the real pipeline.
public class FeedRulesTests
{
    [Fact]
    public void CleanText_DropsControlCharacters_KeepsAtMostTwoLineBreaks_AndTrims()
    {
        Assert.Equal("a\n\nb", FeedRules.CleanText("  a\r\n\r\n\r\n\r\nb \u0007​ "));
        Assert.Equal("xin chào", FeedRules.CleanText("xin‮ chào"));
        Assert.Equal("", FeedRules.CleanText(null));
        Assert.Equal("<b>x</b>", FeedRules.CleanText("<b>x</b>"));   // plain text: markup is kept as text, never interpreted
    }

    [Fact]
    public void Cursor_RoundTrips_AndRefusesGarbage()
    {
        var at = new DateTimeOffset(2026, 11, 13, 1, 2, 3, TimeSpan.Zero).AddTicks(1234);
        string cursor = FeedRules.CursorOf(at, 481);
        Assert.True(FeedRules.TryReadCursor(cursor, out DateTimeOffset back, out long id));
        Assert.Equal(at, back);
        Assert.Equal(481, id);

        Assert.False(FeedRules.TryReadCursor("", out _, out _));
        Assert.False(FeedRules.TryReadCursor("not base64!", out _, out _));
        Assert.False(FeedRules.TryReadCursor(Convert.ToBase64String("1:2:3"u8.ToArray()), out _, out _));
        Assert.False(FeedRules.TryReadCursor(Convert.ToBase64String("3155378976000000000:1"u8.ToArray()), out _, out _));
    }

    [Fact]
    public void HoldWords_SendAPostToReview_CaseInsensitively()
    {
        var options = new FeedOptions { HoldWords = ["casino", " "] };
        Assert.True(FeedRules.ShouldHold("Mời vào CaSiNo nhé", options));
        Assert.False(FeedRules.ShouldHold("Đại Nội buổi sáng", options));
        Assert.False(FeedRules.ShouldHold("anything", new FeedOptions()));
    }
}

public class FeedEndpointGateTests
{
    private static WebApplicationFactory<Program> Factory(bool enabled) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Default", "Host=localhost;Database=unused");
            builder.UseSetting("Jwt:Key", ApiFixture.JwtKey);
            builder.UseSetting("Signing:PrivateKeyPkcs8", Convert.ToBase64String(ApiFixture.SigningKey.ExportPkcs8PrivateKey()));
            builder.UseSetting("RateLimit:GlobalPerMinute", "100000");
            builder.UseSetting("Feed:Enabled", enabled ? "true" : "false");
        });

    [Theory]
    [InlineData("GET", "/v1/feed")]
    [InlineData("GET", "/v1/feed/new-count")]
    [InlineData("GET", "/v1/posts/1")]
    [InlineData("GET", "/v1/posts/1/comments")]
    [InlineData("GET", "/v1/media/1")]
    public async Task WhenOff_EveryReadingEndpoint_Answers503(string method, string path)
    {
        using WebApplicationFactory<Program> factory = Factory(enabled: false);
        HttpResponseMessage response = await factory.CreateClient().SendAsync(new HttpRequestMessage(new HttpMethod(method), path));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("chưa được bật", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("POST", "/v1/posts")]
    [InlineData("PUT", "/v1/posts/1/like")]
    [InlineData("POST", "/v1/posts/1/comments")]
    [InlineData("POST", "/v1/posts/1/reports")]
    [InlineData("POST", "/v1/media")]
    [InlineData("GET", "/v1/me/feed")]
    [InlineData("GET", "/v1/admin/feed/pending")]
    public async Task WhenOn_AGuest_CannotWrite_Gets401(string method, string path)
    {
        using WebApplicationFactory<Program> factory = Factory(enabled: true);
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method is "POST" or "PUT") request.Content = JsonContent.Create(new { });
        HttpResponseMessage response = await factory.CreateClient().SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ABadCursor_IsRefused_BeforeAnyQuery()
    {
        using WebApplicationFactory<Program> factory = Factory(enabled: true);
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/v1/feed?cursor=garbage");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        JsonElement problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("Con trỏ", problem.GetProperty("detail").GetString());
    }
}
