using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Npgsql;

namespace Hue_travel.Api.Tests;

[Collection("api")]
public class RolesTests(ApiFixture fixture)
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

    /// <summary>A registered user with a ready-to-use client; role "admin" is set directly in the database, then the user logs in again.</summary>
    private async Task<(HttpClient Http, string Username, long Id)> UserAsync(string role = "user")
    {
        HttpClient http = fixture.CreateClient();
        string name = NewName();
        HttpResponseMessage registered = await http.PostAsJsonAsync("/v1/auth/register", new { username = name, password = Password });
        Assert.Equal(HttpStatusCode.Created, registered.StatusCode);
        long id = (await Json(registered)).GetProperty("user").GetProperty("id").GetInt64();
        if (role != "user") await Sql("update app_user set role = @r where id = @i", ("r", role), ("i", id));
        JsonElement login = await Json(await http.PostAsJsonAsync("/v1/auth/login", new { username = name, password = Password }));
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.GetProperty("accessToken").GetString());
        return (http, name, id);
    }

    private static async Task<string> CreatePlaceAsync(HttpClient admin)
    {
        string id = "T" + Guid.NewGuid().ToString("N")[..12];
        HttpResponseMessage response = await admin.PostAsJsonAsync("/v1/admin/pois", new { id, group = "service", name = "Điểm thử " + id, lon = 107.5786, lat = 16.4698, address = "Địa chỉ cũ" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return id;
    }

    [RequiresDbFact]
    public async Task Favorites_AddListRemove()
    {
        (HttpClient user, _, _) = await UserAsync();
        Assert.Equal(HttpStatusCode.NoContent, (await user.PutAsync("/v1/favorites/DL001", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await user.PutAsync("/v1/favorites/DL001", null)).StatusCode);
        JsonElement list = await Json(await user.GetAsync("/v1/favorites"));
        Assert.Equal(1, list.GetArrayLength());
        Assert.Equal(HttpStatusCode.NotFound, (await user.PutAsync("/v1/favorites/KHONG-CO", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await user.DeleteAsync("/v1/favorites/DL001")).StatusCode);
        Assert.Equal(0, (await Json(await user.GetAsync("/v1/favorites"))).GetArrayLength());
    }

    [RequiresDbFact]
    public async Task Itineraries_CreateUpdateDeleteAndAreOnlyVisibleToTheirOwner()
    {
        (HttpClient user, _, _) = await UserAsync();
        (HttpClient other, _, _) = await UserAsync();
        HttpResponseMessage created = await user.PostAsJsonAsync("/v1/itineraries", new { name = "Một ngày ở Huế", poiIds = new[] { "DL001", "DL004" } });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        long id = (await Json(created)).GetProperty("id").GetInt64();

        Assert.Equal(new[] { "DL001", "DL004" }, (await Json(await user.GetAsync("/v1/itineraries")))[0].GetProperty("poiIds").EnumerateArray().Select(e => e.GetString()).ToArray());
        Assert.Equal(0, (await Json(await other.GetAsync("/v1/itineraries"))).GetArrayLength());
        Assert.Equal(HttpStatusCode.NotFound, (await other.PutAsJsonAsync($"/v1/itineraries/{id}", new { name = "x", poiIds = new[] { "DL001" } })).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await user.PutAsJsonAsync($"/v1/itineraries/{id}", new { name = "Đổi tên", poiIds = new[] { "DL004" } })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await user.PostAsJsonAsync("/v1/itineraries", new { name = "", poiIds = Array.Empty<string>() })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await user.PostAsJsonAsync("/v1/itineraries", new { name = "Sai", poiIds = new[] { "KHONG-CO" } })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await user.DeleteAsync($"/v1/itineraries/{id}")).StatusCode);
    }

    [RequiresDbFact]
    public async Task Reviews_AreSavedOncePerUserAndAveraged()
    {
        (HttpClient admin, _, _) = await UserAsync("admin");
        string place = await CreatePlaceAsync(admin);
        (HttpClient a, _, _) = await UserAsync();
        (HttpClient b, _, _) = await UserAsync();
        Assert.Equal(HttpStatusCode.NoContent, (await a.PostAsJsonAsync($"/v1/pois/{place}/reviews", new { rating = 5, comment = "Rất tốt" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await a.PostAsJsonAsync($"/v1/pois/{place}/reviews", new { rating = 4, comment = "Sửa lại" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await b.PostAsJsonAsync($"/v1/pois/{place}/reviews", new { rating = 2 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await b.PostAsJsonAsync($"/v1/pois/{place}/reviews", new { rating = 6 })).StatusCode);

        JsonElement list = await Json(await fixture.CreateClient().GetAsync($"/v1/pois/{place}/reviews"));
        Assert.Equal(2, list.GetProperty("count").GetInt64());
        Assert.Equal(3.0, list.GetProperty("average").GetDouble());
    }

    [RequiresDbFact]
    public async Task Reports_NeedAKnownKind()
    {
        (HttpClient user, _, _) = await UserAsync();
        Assert.Equal(HttpStatusCode.Created, (await user.PostAsJsonAsync("/v1/pois/DL001/reports", new { kind = "closed", content = "Đóng cửa" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await user.PostAsJsonAsync("/v1/pois/DL001/reports", new { kind = "spam" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await fixture.CreateClient().PostAsJsonAsync("/v1/pois/DL001/reports", new { kind = "closed" })).StatusCode);
    }

    [RequiresDbFact]
    public async Task AdminEndpoints_NeedTheAdminRole()
    {
        (HttpClient user, _, _) = await UserAsync();
        (HttpClient admin, _, _) = await UserAsync("admin");
        Assert.Equal(HttpStatusCode.Unauthorized, (await fixture.CreateClient().GetAsync("/v1/admin/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await user.GetAsync("/v1/admin/users")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/v1/admin/users")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/v1/admin/stats")).StatusCode);
    }

    [RequiresDbFact]
    public async Task Owner_ClaimIsApproved_EditRequestIsReviewed_AndEverythingIsAudited()
    {
        (HttpClient admin, _, _) = await UserAsync("admin");
        string place = await CreatePlaceAsync(admin);
        (HttpClient owner, string ownerName, long ownerId) = await UserAsync();

        // Nobody may edit before the claim is approved.
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.PostAsJsonAsync($"/v1/owner/places/{place}/edit-requests", new { changes = new { address = "Mới" } })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await owner.PostAsJsonAsync("/v1/owner/claims", new { poiId = place })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await owner.PostAsJsonAsync("/v1/owner/claims", new { poiId = place })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.PostAsJsonAsync($"/v1/owner/places/{place}/edit-requests", new { changes = new { address = "Mới" } })).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsJsonAsync($"/v1/admin/claims/{ownerId}/{place}/decision", new { approve = true })).StatusCode);
        Assert.Equal(1, (await Json(await owner.GetAsync("/v1/owner/places"))).GetArrayLength());

        // Only the allowed fields, and the data stays unchanged until an admin approves.
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsJsonAsync($"/v1/owner/places/{place}/edit-requests", new { changes = new { name = "Tên mới" } })).StatusCode);
        HttpResponseMessage requested = await owner.PostAsJsonAsync($"/v1/owner/places/{place}/edit-requests", new { changes = new { address = "Địa chỉ mới", priceText = "50.000đ/người" } });
        Assert.Equal(HttpStatusCode.Created, requested.StatusCode);
        long requestId = (await Json(requested)).GetProperty("id").GetInt64();
        Assert.Equal("Địa chỉ cũ", (await Json(await fixture.CreateClient().GetAsync($"/v1/pois/{place}"))).GetProperty("address").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsJsonAsync($"/v1/admin/edit-requests/{requestId}/decision", new { approve = true })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PostAsJsonAsync($"/v1/admin/edit-requests/{requestId}/decision", new { approve = true })).StatusCode);
        JsonElement poi = await Json(await fixture.CreateClient().GetAsync($"/v1/pois/{place}"));
        Assert.Equal("Địa chỉ mới", poi.GetProperty("address").GetString());
        Assert.Equal(50000, poi.GetProperty("priceFrom").GetInt32());

        JsonElement audit = await Json(await admin.GetAsync("/v1/admin/audit?entity=poi"));
        Assert.Contains(audit.EnumerateArray(), e => e.GetProperty("action").GetString() == "edit_request.approve" && e.GetProperty("entityId").GetString() == place);
        Assert.NotEmpty(ownerName);
    }

    [RequiresDbFact]
    public async Task Owner_CannotTouchAPlaceThatIsNotTheirs()
    {
        (HttpClient admin, _, _) = await UserAsync("admin");
        string mine = await CreatePlaceAsync(admin);
        string theirs = await CreatePlaceAsync(admin);
        (HttpClient owner, _, long ownerId) = await UserAsync();
        await owner.PostAsJsonAsync("/v1/owner/claims", new { poiId = mine });
        await admin.PostAsJsonAsync($"/v1/admin/claims/{ownerId}/{mine}/decision", new { approve = true });

        Assert.Equal(HttpStatusCode.Forbidden, (await owner.PostAsJsonAsync($"/v1/owner/places/{theirs}/announcements", new { title = "x", body = "y" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.GetAsync($"/v1/owner/places/{theirs}/stats")).StatusCode);
    }

    [RequiresDbFact]
    public async Task Owner_AnnouncementIsPublic_ReplyAndStatsWork()
    {
        (HttpClient admin, _, _) = await UserAsync("admin");
        string place = await CreatePlaceAsync(admin);
        (HttpClient owner, _, long ownerId) = await UserAsync();
        (HttpClient guest, _, _) = await UserAsync();
        await owner.PostAsJsonAsync("/v1/owner/claims", new { poiId = place });
        await admin.PostAsJsonAsync($"/v1/admin/claims/{ownerId}/{place}/decision", new { approve = true });

        Assert.Equal(HttpStatusCode.Created, (await owner.PostAsJsonAsync($"/v1/owner/places/{place}/announcements", new { title = "Giảm giá", body = "Giảm 20% cuối tuần" })).StatusCode);
        Assert.Equal(1, (await Json(await fixture.CreateClient().GetAsync($"/v1/pois/{place}/announcements"))).GetArrayLength());

        await guest.PostAsJsonAsync($"/v1/pois/{place}/reviews", new { rating = 5, comment = "Tuyệt" });
        long reviewId = (await Json(await fixture.CreateClient().GetAsync($"/v1/pois/{place}/reviews"))).GetProperty("items")[0].GetProperty("id").GetInt64();
        Assert.Equal(HttpStatusCode.NoContent, (await owner.PostAsJsonAsync($"/v1/owner/places/{place}/reviews/{reviewId}/reply", new { reply = "Cảm ơn bạn" })).StatusCode);
        Assert.Equal("Cảm ơn bạn", (await Json(await fixture.CreateClient().GetAsync($"/v1/pois/{place}/reviews"))).GetProperty("items")[0].GetProperty("ownerReply").GetString());

        await fixture.CreateClient().GetAsync($"/v1/pois/{place}");
        JsonElement stats = await Json(await owner.GetAsync($"/v1/owner/places/{place}/stats"));
        Assert.True(stats.GetProperty("days")[0].GetProperty("views").GetInt32() >= 1);
        Assert.Equal(1, stats.GetProperty("reviewCount").GetInt64());
    }

    [RequiresDbFact]
    public async Task Admin_ResetPasswordForcesAChange_AndLockBlocksLogin()
    {
        (HttpClient admin, _, _) = await UserAsync("admin");
        (HttpClient user, string username, long id) = await UserAsync();

        JsonElement reset = await Json(await admin.PostAsync($"/v1/admin/users/{id}/reset-password", null));
        string temporary = reset.GetProperty("temporaryPassword").GetString()!;
        HttpClient http = fixture.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await http.PostAsJsonAsync("/v1/auth/login", new { username, password = Password })).StatusCode);
        JsonElement login = await Json(await http.PostAsJsonAsync("/v1/auth/login", new { username, password = temporary }));
        Assert.True(login.GetProperty("mustChangePassword").GetBoolean());

        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.GetProperty("accessToken").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, (await http.PostAsJsonAsync("/v1/me/password", new { currentPassword = "sai-sai-sai", newPassword = "mat-khau-moi-123" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await http.PostAsJsonAsync("/v1/me/password", new { currentPassword = temporary, newPassword = "mat-khau-moi-123" })).StatusCode);
        JsonElement again = await Json(await fixture.CreateClient().PostAsJsonAsync("/v1/auth/login", new { username, password = "mat-khau-moi-123" }));
        Assert.False(again.GetProperty("mustChangePassword").GetBoolean());

        Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync($"/v1/admin/users/{id}", new { status = "locked" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await fixture.CreateClient().PostAsJsonAsync("/v1/auth/login", new { username, password = "mat-khau-moi-123" })).StatusCode);
        Assert.NotNull(user);
    }

    [RequiresDbFact]
    public async Task Admin_CannotLockOrDemoteThemselves()
    {
        (HttpClient admin, _, long id) = await UserAsync("admin");
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync($"/v1/admin/users/{id}", new { status = "locked" })).StatusCode);
    }

    [RequiresDbFact]
    public async Task Admin_CreatesAPlace_ItIsRoutable_ThenHidesAndDeletesIt()
    {
        (HttpClient admin, _, _) = await UserAsync("admin");
        string place = await CreatePlaceAsync(admin);
        HttpClient guest = fixture.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await guest.GetAsync($"/v1/pois/{place}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await guest.PostAsJsonAsync("/v1/route", new { startId = "GT002", stopIds = new[] { place } })).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync($"/v1/admin/pois/{place}", new { status = "hidden" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await guest.GetAsync($"/v1/pois/{place}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/v1/admin/pois/{place}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.DeleteAsync($"/v1/admin/pois/{place}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync("/v1/admin/pois", new { id = place, group = "service", name = "Trùng", lon = 107.5, lat = 16.4 })).StatusCode);
    }

    [RequiresDbFact]
    public async Task Admin_CreatesAnOwnerAccount_AndModeratesReviewsAndReports()
    {
        (HttpClient admin, _, _) = await UserAsync("admin");
        string name = NewName();
        HttpResponseMessage created = await admin.PostAsJsonAsync("/v1/admin/users", new { username = name, password = "tam-thoi-123", displayName = "Chủ quán", role = "owner" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.True((await Json(await fixture.CreateClient().PostAsJsonAsync("/v1/auth/login", new { username = name, password = "tam-thoi-123" }))).GetProperty("mustChangePassword").GetBoolean());

        (HttpClient user, _, _) = await UserAsync();
        string place = await CreatePlaceAsync(admin);
        await user.PostAsJsonAsync($"/v1/pois/{place}/reviews", new { rating = 1, comment = "Spam" });
        long reviewId = (await Json(await fixture.CreateClient().GetAsync($"/v1/pois/{place}/reviews"))).GetProperty("items")[0].GetProperty("id").GetInt64();
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync($"/v1/admin/reviews/{reviewId}", new { status = "hidden" })).StatusCode);
        Assert.Equal(0, (await Json(await fixture.CreateClient().GetAsync($"/v1/pois/{place}/reviews"))).GetProperty("count").GetInt64());

        await user.PostAsJsonAsync($"/v1/pois/{place}/reports", new { kind = "wrong_info", content = "Sai giờ" });
        JsonElement reports = await Json(await admin.GetAsync("/v1/admin/reports"));
        long reportId = reports.EnumerateArray().First(r => r.GetProperty("poiId").GetString() == place).GetProperty("id").GetInt64();
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsync($"/v1/admin/reports/{reportId}/resolve", null)).StatusCode);
    }
}
