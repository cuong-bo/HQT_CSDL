using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Hue_travel.Api.Tests;

[Collection("api")]
public class ApiTests(ApiFixture fixture)
{
    private static string NewName() => "t" + Guid.NewGuid().ToString("N")[..12];

    private static async Task<JsonElement> Json(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>())!;

    private static async Task<(string Username, string Access, string Refresh)> RegisterAsync(HttpClient http)
    {
        string name = NewName();
        HttpResponseMessage response = await http.PostAsJsonAsync("/v1/auth/register", new { username = name, password = "matkhau-dai-123", displayName = "Người thử" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        JsonElement body = await Json(response);
        return (name, body.GetProperty("accessToken").GetString()!, body.GetProperty("refreshToken").GetString()!);
    }

    [RequiresDbFact]
    public async Task Healthz_IsOkAndNothingIsCached()
    {
        HttpResponseMessage response = await fixture.CreateClient().GetAsync("/healthz");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString());
    }

    [RequiresDbFact]
    public async Task Pois_ListsAllVisiblePlaces()
    {
        JsonElement page = await Json(await fixture.CreateClient().GetAsync("/v1/pois?pageSize=100"));
        Assert.True(page.GetProperty("total").GetInt64() >= 109);
        Assert.Equal(100, page.GetProperty("items").GetArrayLength());
    }

    [RequiresDbFact]
    public async Task Pois_FiltersBySeveralGroups()
    {
        HttpClient http = fixture.CreateClient();
        long stays = (await Json(await http.GetAsync("/v1/pois?group=stay"))).GetProperty("total").GetInt64();
        long transport = (await Json(await http.GetAsync("/v1/pois?group=transport"))).GetProperty("total").GetInt64();
        long both = (await Json(await http.GetAsync("/v1/pois?group=stay&group=transport"))).GetProperty("total").GetInt64();
        Assert.Equal(38, stays);
        Assert.Equal(11, transport);
        Assert.Equal(stays + transport, both);
    }

    [RequiresDbFact]
    public async Task Pois_SearchIgnoresDiacritics()
    {
        JsonElement page = await Json(await fixture.CreateClient().GetAsync("/v1/pois?q=hai%20van"));
        Assert.Contains(page.GetProperty("items").EnumerateArray(), p => p.GetProperty("name").GetString() == "Hải Vân Quan");
    }

    [RequiresDbFact]
    public async Task Pois_RejectsAnUnknownGroup()
    {
        HttpResponseMessage response = await fixture.CreateClient().GetAsync("/v1/pois?group=nope");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [RequiresDbFact]
    public async Task Poi_ByIdReturnsDetailsAndUnknownIsNotFound()
    {
        HttpClient http = fixture.CreateClient();
        JsonElement poi = await Json(await http.GetAsync("/v1/pois/DL001"));
        Assert.Equal("Hải Vân Quan", poi.GetProperty("name").GetString());
        Assert.InRange(poi.GetProperty("lon").GetDouble(), 107.0, 109.0);
        Assert.Equal(HttpStatusCode.NotFound, (await http.GetAsync("/v1/pois/KHONG-CO")).StatusCode);
    }

    [RequiresDbFact]
    public async Task Area_NamesTheCommuneAtImperialCity()
    {
        JsonElement area = await Json(await fixture.CreateClient().GetAsync("/v1/area?lon=107.5786&lat=16.4698"));
        Assert.False(string.IsNullOrWhiteSpace(area.GetProperty("name").GetString()));
    }

    [RequiresDbFact]
    public async Task Area_FarAwayIsNotFoundAndBadInputIsRejected()
    {
        HttpClient http = fixture.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await http.GetAsync("/v1/area?lon=0&lat=0")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await http.GetAsync("/v1/area?lon=500&lat=0")).StatusCode);
    }

    [RequiresDbFact]
    public async Task Register_Login_Me_Refresh_FullCycle()
    {
        HttpClient http = fixture.CreateClient();
        (string username, string access, string refresh) = await RegisterAsync(http);

        Assert.Equal(HttpStatusCode.Unauthorized, (await http.GetAsync("/v1/me")).StatusCode);
        var request = new HttpRequestMessage(HttpMethod.Get, "/v1/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access);
        JsonElement me = await Json(await http.SendAsync(request));
        Assert.Equal(username, me.GetProperty("username").GetString());
        Assert.Equal("user", me.GetProperty("role").GetString());

        Assert.Equal(HttpStatusCode.OK, (await http.PostAsJsonAsync("/v1/auth/login", new { username = username.ToUpperInvariant(), password = "matkhau-dai-123" })).StatusCode);

        HttpResponseMessage refreshed = await http.PostAsJsonAsync("/v1/auth/refresh", new { refreshToken = refresh });
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        Assert.NotEqual(refresh, (await Json(refreshed)).GetProperty("refreshToken").GetString());
        Assert.Equal(HttpStatusCode.Unauthorized, (await http.PostAsJsonAsync("/v1/auth/refresh", new { refreshToken = refresh })).StatusCode);
    }

    [RequiresDbFact]
    public async Task Register_RejectsDuplicatesAndWeakPasswords()
    {
        HttpClient http = fixture.CreateClient();
        (string username, _, _) = await RegisterAsync(http);
        Assert.Equal(HttpStatusCode.Conflict, (await http.PostAsJsonAsync("/v1/auth/register", new { username, password = "matkhau-dai-123" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await http.PostAsJsonAsync("/v1/auth/register", new { username = NewName(), password = "ngan" })).StatusCode);
    }

    [RequiresDbFact]
    public async Task Login_LocksTheAccountAfterFiveWrongPasswords()
    {
        HttpClient http = fixture.CreateClient();
        (string username, _, _) = await RegisterAsync(http);
        for (int i = 0; i < 5; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await http.PostAsJsonAsync("/v1/auth/login", new { username, password = "sai-mat-khau" })).StatusCode);
        Assert.Equal((HttpStatusCode)423, (await http.PostAsJsonAsync("/v1/auth/login", new { username, password = "matkhau-dai-123" })).StatusCode);
    }

    [RequiresDbFact]
    public async Task Login_DoesNotRevealWhetherTheUserExists()
    {
        HttpClient http = fixture.CreateClient();
        HttpResponseMessage unknown = await http.PostAsJsonAsync("/v1/auth/login", new { username = NewName(), password = "bat-ky-123" });
        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
        (string username, _, _) = await RegisterAsync(http);
        HttpResponseMessage wrong = await http.PostAsJsonAsync("/v1/auth/login", new { username, password = "bat-ky-123" });
        Assert.Equal(await unknown.Content.ReadAsStringAsync(), await wrong.Content.ReadAsStringAsync());
    }

    [RequiresDbFact]
    public async Task Route_OrdersStopsAndReturnsLegsAlongTheRoads()
    {
        HttpClient http = fixture.CreateClient();
        HttpResponseMessage response = await http.PostAsJsonAsync("/v1/route", new { startId = "GT002", stopIds = new[] { "DL001", "DL004", "DL002" }, optimize = true });
        // DL002 is about a kilometre from the nearest road; the route is still computed from that road.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        JsonElement route = await Json(response);
        Assert.Equal(3, route.GetProperty("order").GetArrayLength());
        Assert.Equal(3, route.GetProperty("legs").GetArrayLength());
        Assert.True(route.GetProperty("totalMeters").GetDouble() > 0);
        Assert.True(route.GetProperty("totalSeconds").GetDouble() > 0);
    }

    [RequiresDbFact]
    public async Task Route_KeepsTheGivenOrderWhenNotOptimizing()
    {
        HttpClient http = fixture.CreateClient();
        JsonElement route = await Json(await http.PostAsJsonAsync("/v1/route", new { startId = "GT002", stopIds = new[] { "DL004", "DL001" }, optimize = false }));
        Assert.Equal(new[] { "DL004", "DL001" }, route.GetProperty("order").EnumerateArray().Select(e => e.GetString()).ToArray());
    }

    [RequiresDbFact]
    public async Task Route_FromAFreePointStartsAtThatPointAndKeepsTheGivenOrder()
    {
        HttpClient http = fixture.CreateClient();
        // A point in the middle of Huế (VN-2000 / UTM 48N metres), e.g. picked on the map.
        JsonElement route = await Json(await http.PostAsJsonAsync("/v1/route", new { startX = 775000.0, startY = 1822500.0, stopIds = new[] { "DL004", "DL001" }, optimize = false }));
        Assert.Equal(new[] { "DL004", "DL001" }, route.GetProperty("order").EnumerateArray().Select(e => e.GetString()).ToArray());
        JsonElement firstLeg = route.GetProperty("legs")[0];
        Assert.Equal("start", firstLeg.GetProperty("fromId").GetString());
        Assert.Equal("DL004", firstLeg.GetProperty("toId").GetString());
        Assert.True(route.GetProperty("totalMeters").GetDouble() > 0);
    }

    [RequiresDbFact]
    public async Task Route_FromAFreePointCanOptimizeTheOrder()
    {
        HttpClient http = fixture.CreateClient();
        JsonElement route = await Json(await http.PostAsJsonAsync("/v1/route", new { startX = 775000.0, startY = 1822500.0, stopIds = new[] { "DL001", "DL004", "DL002" }, optimize = true }));
        Assert.Equal(3, route.GetProperty("order").GetArrayLength());
        Assert.Equal("start", route.GetProperty("legs")[0].GetProperty("fromId").GetString());
    }

    [RequiresDbFact]
    public async Task Route_GivesATimePerPointOfEveryLeg_RisingFromZeroToTheLegTime()
    {
        HttpClient http = fixture.CreateClient();
        JsonElement route = await Json(await http.PostAsJsonAsync("/v1/route", new { startX = 775000.0, startY = 1822500.0, stopIds = new[] { "DL004", "DL001" }, optimize = false }));
        foreach (JsonElement leg in route.GetProperty("legs").EnumerateArray())
        {
            double[] times = leg.GetProperty("times").EnumerateArray().Select(t => t.GetDouble()).ToArray();
            Assert.Equal(leg.GetProperty("geometryUtm").GetProperty("coordinates").GetArrayLength(), times.Length);
            Assert.Equal(0, times[0]);
            Assert.True(times.Zip(times.Skip(1), (a, b) => b >= a).All(ok => ok));
            Assert.Equal(leg.GetProperty("seconds").GetDouble(), times[^1], 1);
        }
    }

    [RequiresDbFact]
    public async Task Route_FromAFreePointJoinsTheRoadWhereItLies_NotAtTheNearestCrossing()
    {
        HttpClient http = fixture.CreateClient();
        JsonElement route = await Json(await http.PostAsJsonAsync("/v1/route", new { startX = 775000.0, startY = 1822500.0, stopIds = new[] { "DL004" }, optimize = false }));
        JsonElement line = route.GetProperty("legs")[0].GetProperty("geometryUtm").GetProperty("coordinates");
        double firstX = line[0][0].GetDouble(), firstY = line[0][1].GetDouble();
        // The line begins at the point the user picked (a short hop to the road), not at some crossing metres away.
        Assert.True(Math.Abs(firstX - 775000.0) < 1 && Math.Abs(firstY - 1822500.0) < 1);
    }

    [RequiresDbFact]
    public async Task Route_AnsweredTwice_GivesTheSamePlan()
    {
        HttpClient http = fixture.CreateClient();
        var request = new { startId = "GT002", stopIds = new[] { "DL004", "DL001" }, optimize = true };
        string first = await (await http.PostAsJsonAsync("/v1/route", request)).Content.ReadAsStringAsync();
        string second = await (await http.PostAsJsonAsync("/v1/route", request)).Content.ReadAsStringAsync();
        Assert.Equal(first, second);
    }

    [RequiresDbFact]
    public async Task Route_RefusesAFreeStartThatIsFarFromEveryRoad()
    {
        HttpClient http = fixture.CreateClient();
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await http.PostAsJsonAsync("/v1/route", new { startX = 1.0, startY = 1.0, stopIds = new[] { "DL001" } })).StatusCode);
    }

    [RequiresDbFact]
    public async Task Route_ValidatesTheRequest()
    {
        HttpClient http = fixture.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await http.PostAsJsonAsync("/v1/route", new { stopIds = new[] { "DL001" } })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await http.PostAsJsonAsync("/v1/route", new { startId = "GT002", stopIds = Array.Empty<string>() })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await http.PostAsJsonAsync("/v1/route", new { startId = "GT002", stopIds = new[] { "DL001", "DL001" } })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await http.PostAsJsonAsync("/v1/route", new { startId = "GT002", stopIds = new[] { "KHONG-CO" } })).StatusCode);
    }

    [RequiresDbFact]
    public async Task Config_ReturnsTileAndVersionInfo()
    {
        JsonElement config = await Json(await fixture.CreateClient().GetAsync("/v1/config"));
        Assert.False(string.IsNullOrEmpty(config.GetProperty("tileUrl").GetString()));
        Assert.False(string.IsNullOrEmpty(config.GetProperty("minimumClientVersion").GetString()));
        Assert.Equal(JsonValueKind.Object, config.GetProperty("tileUrls").ValueKind);   // empty unless the operator set per-map addresses
    }

    [RequiresDbFact]
    public async Task Login_IsRateLimitedPerClient()
    {
        using WebApplicationFactory<Program> limited = fixture.Factory.WithWebHostBuilder(b => b.UseSetting("RateLimit:AuthPerMinute", "3"));
        HttpClient http = limited.CreateClient();
        var codes = new List<HttpStatusCode>();
        for (int i = 0; i < 5; i++)
            codes.Add((await http.PostAsJsonAsync("/v1/auth/login", new { username = NewName(), password = "bat-ky-123" })).StatusCode);
        Assert.Contains(HttpStatusCode.TooManyRequests, codes);
        Assert.Equal(HttpStatusCode.Unauthorized, codes[0]);
    }
}

public class StartupTests
{
    [Fact]
    public void TheServerRefusesToStartWithoutASigningKey()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseSetting("ConnectionStrings:Default", "Host=localhost;Database=unused");
            b.UseSetting("Jwt:Key", "short");
        });
        Assert.ThrowsAny<Exception>(() => factory.CreateClient());
    }
}
