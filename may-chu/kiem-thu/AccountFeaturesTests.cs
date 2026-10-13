using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Npgsql;
using SkiaSharp;

namespace Hue_travel.Api.Tests;

/// <summary>Registering as an owner, place photos, deleting accounts, importing places and a user's own reviews.</summary>
[Collection("api")]
public class AccountFeaturesTests(ApiFixture fixture)
{
    private const string Password = "matkhau-dai-123";

    private static string NewName() => "t" + Guid.NewGuid().ToString("N")[..12];

    private static async Task<JsonElement> Json(HttpResponseMessage response) => (await response.Content.ReadFromJsonAsync<JsonElement>())!;

    private static async Task Sql(string sql, params (string, object)[] args)
    {
        await using NpgsqlDataSource db = NpgsqlDataSource.Create(ApiFixture.ConnectionString!);
        await using NpgsqlCommand cmd = db.CreateCommand(sql);
        foreach ((string name, object value) in args) cmd.Parameters.AddWithValue(name, value);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task<(HttpClient Http, string Username, long Id)> SignInAsync(string name)
    {
        HttpClient http = fixture.CreateClient();
        JsonElement login = await Json(await http.PostAsJsonAsync("/v1/auth/login", new { username = name, password = Password }));
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.GetProperty("accessToken").GetString());
        return (http, name, login.GetProperty("user").GetProperty("id").GetInt64());
    }

    private async Task<(HttpClient Http, string Username, long Id)> UserAsync(string role = "user")
    {
        string name = NewName();
        HttpResponseMessage registered = await fixture.CreateClient().PostAsJsonAsync("/v1/auth/register", new { username = name, password = Password });
        Assert.Equal(HttpStatusCode.Created, registered.StatusCode);
        long id = (await Json(registered)).GetProperty("user").GetProperty("id").GetInt64();
        if (role != "user") await Sql("update app_user set role = @r where id = @i", ("r", role), ("i", id));
        return await SignInAsync(name);
    }

    private static async Task<string> CreatePlaceAsync(HttpClient admin)
    {
        string id = "T" + Guid.NewGuid().ToString("N")[..12];
        HttpResponseMessage response = await admin.PostAsJsonAsync("/v1/admin/pois", new { id, group = "service", name = "Điểm thử " + id, lon = 107.5786, lat = 16.4698 });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return id;
    }

    /// <summary>An owner whose claim on a new place an admin has approved.</summary>
    private async Task<(HttpClient Owner, string PlaceId, HttpClient Admin)> OwnerOfNewPlaceAsync()
    {
        (HttpClient admin, _, _) = await UserAsync("admin");
        string place = await CreatePlaceAsync(admin);
        string name = NewName();
        HttpResponseMessage registered = await fixture.CreateClient().PostAsJsonAsync("/v1/auth/register",
            new { username = name, password = Password, accountType = "owner", placeIds = new[] { place } });
        Assert.Equal(HttpStatusCode.Created, registered.StatusCode);
        long ownerId = (await Json(registered)).GetProperty("user").GetProperty("id").GetInt64();
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsJsonAsync($"/v1/admin/claims/{ownerId}/{place}/decision", new { approve = true })).StatusCode);
        (HttpClient owner, _, _) = await SignInAsync(name);
        return (owner, place, admin);
    }

    private static string PngBase64(int width = 400, int height = 300)
    {
        using var bitmap = new SKBitmap(width, height);
        using (var canvas = new SKCanvas(bitmap)) canvas.Clear(new SKColor(30, 120, 200));
        using SKData png = SKImage.FromBitmap(bitmap).Encode(SKEncodedImageFormat.Png, 100);
        return Convert.ToBase64String(png.ToArray());
    }

    // ------------------------------------------------------------------ registering

    [RequiresDbFact]
    public async Task Register_AsOwner_StaysAUserUntilAnAdminApprovesTheClaim()
    {
        (HttpClient admin, _, _) = await UserAsync("admin");
        string place = await CreatePlaceAsync(admin);
        string name = NewName();
        JsonElement registered = await Json(await fixture.CreateClient().PostAsJsonAsync("/v1/auth/register",
            new { username = name, password = Password, accountType = "owner", placeIds = new[] { place } }));
        Assert.Equal("user", registered.GetProperty("user").GetProperty("role").GetString());
        long id = registered.GetProperty("user").GetProperty("id").GetInt64();

        (HttpClient owner, _, _) = await SignInAsync(name);
        JsonElement claims = await Json(await owner.GetAsync("/v1/owner/claims"));
        Assert.Equal("pending", claims[0].GetProperty("status").GetString());
        Assert.Equal(0, (await Json(await owner.GetAsync("/v1/owner/places"))).GetArrayLength());

        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsJsonAsync($"/v1/admin/claims/{id}/{place}/decision", new { approve = true })).StatusCode);
        (HttpClient approved, _, _) = await SignInAsync(name);
        Assert.Equal(1, (await Json(await approved.GetAsync("/v1/owner/places"))).GetArrayLength());
        Assert.Equal("owner", (await Json(await approved.GetAsync("/v1/me"))).GetProperty("role").GetString());
    }

    [RequiresDbFact]
    public async Task Register_AsOwner_NeedsExistingPlaces_AndAKnownAccountType()
    {
        HttpClient http = fixture.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await http.PostAsJsonAsync("/v1/auth/register", new { username = NewName(), password = Password, accountType = "owner" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await http.PostAsJsonAsync("/v1/auth/register",
            new { username = NewName(), password = Password, accountType = "owner", placeIds = new[] { "KHONG-CO" } })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await http.PostAsJsonAsync("/v1/auth/register", new { username = NewName(), password = Password, accountType = "admin" })).StatusCode);
        // a plain user who sends places gets no claims
        string name = NewName();
        Assert.Equal(HttpStatusCode.Created, (await http.PostAsJsonAsync("/v1/auth/register", new { username = name, password = Password, placeIds = new[] { "DL001" } })).StatusCode);
        (HttpClient user, _, _) = await SignInAsync(name);
        Assert.Equal(0, (await Json(await user.GetAsync("/v1/owner/claims"))).GetArrayLength());
    }

    // ------------------------------------------------------------------ photos

    [RequiresDbFact]
    public async Task Photos_AnOwnerUploads_AnAdminApproves_ThenEveryoneSeesIt()
    {
        (HttpClient owner, string place, HttpClient admin) = await OwnerOfNewPlaceAsync();
        HttpResponseMessage uploaded = await owner.PostAsJsonAsync($"/v1/owner/places/{place}/photos", new { contentType = "image/png", data = PngBase64() });
        Assert.Equal(HttpStatusCode.Created, uploaded.StatusCode);
        long id = (await Json(uploaded)).GetProperty("id").GetInt64();

        HttpClient guest = fixture.CreateClient();
        Assert.Equal(0, (await Json(await guest.GetAsync($"/v1/pois/{place}/photos"))).GetArrayLength());
        Assert.Equal(HttpStatusCode.NotFound, (await guest.GetAsync($"/v1/photos/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/v1/photos/{id}")).StatusCode);   // its owner sees it while it waits
        Assert.Equal("pending", (await Json(await owner.GetAsync($"/v1/owner/places/{place}/photos")))[0].GetProperty("status").GetString());

        JsonElement pending = await Json(await admin.GetAsync("/v1/admin/photos"));
        Assert.Contains(pending.EnumerateArray(), p => p.GetProperty("id").GetInt64() == id);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsJsonAsync($"/v1/admin/photos/{id}/decision", new { approve = true })).StatusCode);

        Assert.Equal(1, (await Json(await guest.GetAsync($"/v1/pois/{place}/photos"))).GetArrayLength());
        JsonElement photo = await Json(await guest.GetAsync($"/v1/photos/{id}"));
        byte[] bytes = Convert.FromBase64String(photo.GetProperty("data").GetString()!);
        Assert.Equal(new byte[] { 0xFF, 0xD8 }, bytes[..2]);   // it was re-encoded as a JPEG
        Assert.Equal("image/jpeg", photo.GetProperty("contentType").GetString());
    }

    [RequiresDbFact]
    public async Task Photos_RejectWhatIsNotAnImage_ATinyImage_AndAnOutsider()
    {
        (HttpClient owner, string place, _) = await OwnerOfNewPlaceAsync();
        string text = Convert.ToBase64String(Encoding.UTF8.GetBytes("không phải ảnh"));
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsJsonAsync($"/v1/owner/places/{place}/photos", new { contentType = "image/png", data = text })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsJsonAsync($"/v1/owner/places/{place}/photos", new { contentType = "image/png", data = "***" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsJsonAsync($"/v1/owner/places/{place}/photos", new { contentType = "image/png", data = PngBase64(50, 50) })).StatusCode);

        (HttpClient stranger, _, _) = await UserAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.PostAsJsonAsync($"/v1/owner/places/{place}/photos", new { contentType = "image/png", data = PngBase64() })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.GetAsync($"/v1/owner/places/{place}/photos")).StatusCode);
    }

    [RequiresDbFact]
    public async Task Photos_AnOwnerHasAFewWaitingPhotosAtMost_AndCanDeleteTheirs()
    {
        (HttpClient owner, string place, _) = await OwnerOfNewPlaceAsync();
        long first = 0;
        for (int i = 0; i < 4; i++)
        {
            HttpResponseMessage response = await owner.PostAsJsonAsync($"/v1/owner/places/{place}/photos", new { contentType = "image/png", data = PngBase64() });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            if (i == 0) first = (await Json(response)).GetProperty("id").GetInt64();
        }
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsJsonAsync($"/v1/owner/places/{place}/photos", new { contentType = "image/png", data = PngBase64() })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/v1/owner/places/{place}/photos/{first}")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await owner.PostAsJsonAsync($"/v1/owner/places/{place}/photos", new { contentType = "image/png", data = PngBase64() })).StatusCode);
    }

    [RequiresDbFact]
    public async Task Photos_AnAdminCanAddOneThatIsApprovedAtOnce_AndRefuseOthers()
    {
        (HttpClient owner, string place, HttpClient admin) = await OwnerOfNewPlaceAsync();
        HttpResponseMessage added = await admin.PostAsJsonAsync($"/v1/admin/pois/{place}/photos", new { contentType = "image/png", data = PngBase64() });
        Assert.Equal(HttpStatusCode.Created, added.StatusCode);
        Assert.Equal("approved", (await Json(added)).GetProperty("status").GetString());

        long id = (await Json(await owner.PostAsJsonAsync($"/v1/owner/places/{place}/photos", new { contentType = "image/png", data = PngBase64() }))).GetProperty("id").GetInt64();
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsJsonAsync($"/v1/admin/photos/{id}/decision", new { approve = false, note = "Ảnh mờ" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await fixture.CreateClient().GetAsync($"/v1/photos/{id}")).StatusCode);
        Assert.Equal("rejected", (await Json(await owner.GetAsync($"/v1/owner/places/{place}/photos"))).EnumerateArray().First(p => p.GetProperty("id").GetInt64() == id).GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.GetAsync("/v1/admin/photos")).StatusCode);
    }

    // ------------------------------------------------------------------ accounts

    [RequiresDbFact]
    public async Task DeleteUser_NeedsTheAccountToBeDisabledFirst_AndNeverYourOwn()
    {
        (HttpClient admin, _, long adminId) = await UserAsync("admin");
        (_, _, long victim) = await UserAsync();
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.DeleteAsync($"/v1/admin/users/{victim}")).StatusCode);   // still active
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.DeleteAsync($"/v1/admin/users/{adminId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync($"/v1/admin/users/{victim}", new { status = "locked" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/v1/admin/users/{victim}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.DeleteAsync($"/v1/admin/users/{victim}")).StatusCode);
        (HttpClient user, _, _) = await UserAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await user.DeleteAsync($"/v1/admin/users/{victim}")).StatusCode);
    }

    [RequiresDbFact]
    public async Task Users_CanBeFilteredByRole_AndShowTheirPlaces()
    {
        (HttpClient owner, _, HttpClient admin) = await OwnerOfNewPlaceAsync();
        JsonElement owners = await Json(await admin.GetAsync("/v1/admin/users?role=owner"));
        Assert.All(owners.EnumerateArray(), u => Assert.Equal("owner", u.GetProperty("role").GetString()));
        Assert.Contains(owners.EnumerateArray(), u => u.GetProperty("ownedPlaces").GetInt32() >= 1);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync("/v1/admin/users?role=king")).StatusCode);
    }

    // ------------------------------------------------------------------ places

    [RequiresDbFact]
    public async Task Import_DryRunChangesNothing_ApplyCreatesUpdatesAndReportsBadRows()
    {
        (HttpClient admin, _, _) = await UserAsync("admin");
        string fresh = "T" + Guid.NewGuid().ToString("N")[..12];
        string existing = await CreatePlaceAsync(admin);
        object[] rows =
        [
            new { id = fresh, group = "service", name = "Dịch vụ nhập thử", lon = 107.59, lat = 16.46, phone = "0234 111 222" },
            new { id = existing, phone = "0234 999 888" },
            new { id = "X!", group = "stay", name = "Mã sai", lon = 107.5, lat = 16.4 },
            new { id = "T" + Guid.NewGuid().ToString("N")[..12], group = "stay" },
        ];

        JsonElement dry = await Json(await admin.PostAsJsonAsync("/v1/admin/pois/import", new { places = rows, dryRun = true }));
        Assert.Equal(1, dry.GetProperty("counts").GetProperty("create").GetInt32());
        Assert.Equal(1, dry.GetProperty("counts").GetProperty("update").GetInt32());
        Assert.Equal(2, dry.GetProperty("counts").GetProperty("invalid").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/v1/pois/{fresh}")).StatusCode);   // nothing happened yet

        JsonElement applied = await Json(await admin.PostAsJsonAsync("/v1/admin/pois/import", new { places = rows, dryRun = false }));
        Assert.False(applied.GetProperty("dryRun").GetBoolean());
        JsonElement created = await Json(await admin.GetAsync($"/v1/pois/{fresh}"));
        Assert.Equal("0234 111 222", created.GetProperty("phone").GetString());
        Assert.Equal("0234 999 888", (await Json(await admin.GetAsync($"/v1/pois/{existing}"))).GetProperty("phone").GetString());

        JsonElement again = await Json(await admin.PostAsJsonAsync("/v1/admin/pois/import", new { places = new object[] { rows[1] }, dryRun = true }));
        Assert.Equal(1, again.GetProperty("counts").GetProperty("unchanged").GetInt32());   // the same file twice changes nothing
    }

    [RequiresDbFact]
    public async Task Import_IsForAdminsOnly_AndRefusesEmptyOrDuplicatedFiles()
    {
        (HttpClient user, _, _) = await UserAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await user.PostAsJsonAsync("/v1/admin/pois/import", new { places = new object[] { new { id = "AB" } }, dryRun = true })).StatusCode);
        (HttpClient admin, _, _) = await UserAsync("admin");
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/v1/admin/pois/import", new { places = Array.Empty<object>() })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/v1/admin/pois/import", new { places = new object[] { new { id = "ABC" }, new { id = "ABC" } } })).StatusCode);
    }

    [RequiresDbFact]
    public async Task AllPlaces_ListsHiddenOnesToo_ButOnlyForAdmins()
    {
        (HttpClient admin, _, _) = await UserAsync("admin");
        string id = await CreatePlaceAsync(admin);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync($"/v1/admin/pois/{id}", new { status = "hidden" })).StatusCode);
        JsonElement hidden = await Json(await admin.GetAsync($"/v1/admin/pois?status=hidden&q={id}"));
        Assert.Equal(id, hidden[0].GetProperty("id").GetString());
        (HttpClient user, _, _) = await UserAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await user.GetAsync("/v1/admin/pois")).StatusCode);
    }

    // ------------------------------------------------------------------ reviews

    [RequiresDbFact]
    public async Task Reviews_AUserWritesChangesAndDeletesTheirOwn_AndSeesItMarked()
    {
        (HttpClient user, _, _) = await UserAsync();
        (HttpClient other, _, _) = await UserAsync();
        Assert.Equal(HttpStatusCode.NoContent, (await user.PostAsJsonAsync("/v1/pois/DL021/reviews", new { rating = 5, comment = "Rất đẹp" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await other.PostAsJsonAsync("/v1/pois/DL021/reviews", new { rating = 3, comment = "Tạm được" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await user.PostAsJsonAsync("/v1/pois/DL021/reviews", new { rating = 4, comment = "Sửa lại" })).StatusCode);

        JsonElement list = await Json(await user.GetAsync("/v1/pois/DL021/reviews"));
        JsonElement first = list.GetProperty("items")[0];
        Assert.True(first.GetProperty("mine").GetBoolean());
        Assert.Equal("Sửa lại", first.GetProperty("comment").GetString());
        Assert.All(list.GetProperty("items").EnumerateArray().Skip(1), r => Assert.False(r.GetProperty("mine").GetBoolean()));
        Assert.All((await Json(await fixture.CreateClient().GetAsync("/v1/pois/DL021/reviews"))).GetProperty("items").EnumerateArray(), r => Assert.False(r.GetProperty("mine").GetBoolean()));

        JsonElement mine = await Json(await user.GetAsync("/v1/me/reviews"));
        Assert.Single(mine.EnumerateArray());
        Assert.Equal("DL021", mine[0].GetProperty("poiId").GetString());
        Assert.Equal(HttpStatusCode.NoContent, (await user.DeleteAsync("/v1/pois/DL021/reviews")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await user.DeleteAsync("/v1/pois/DL021/reviews")).StatusCode);
        Assert.Equal(0, (await Json(await user.GetAsync("/v1/me/reviews"))).GetArrayLength());
    }
}
