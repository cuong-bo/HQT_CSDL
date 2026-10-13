using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Hue_travel.Api.Tests;

/// <summary>The base map manifest does not touch the database, so these tests run without HUE_TEST_DB.</summary>
public sealed class BasemapTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "hue-basemap-" + Guid.NewGuid().ToString("N"));

    public BasemapTests() => Directory.CreateDirectory(directory);

    public void Dispose() => Directory.Delete(directory, recursive: true);

    private HttpClient CreateClient() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Default", "Host=localhost;Database=unused");
            builder.UseSetting("Jwt:Key", ApiFixture.JwtKey);
            builder.UseSetting("Basemap:Directory", directory);
            builder.UseSetting("RateLimit:GlobalPerMinute", "100000");
        }).CreateClient();

    [Fact]
    public async Task Manifest_IsNotFoundUntilTheBaseMapHasBeenPacked()
    {
        HttpResponseMessage response = await CreateClient().GetAsync("/v1/basemap/manifest");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("deploy.sh pack", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Manifest_IsSentExactlyAsPacked()
    {
        const string packed = """
            {"version":"0123456789abcdef","package":{"path":"/static/basemap/0123456789abcdef/package.zip","size":3,"sha256":"aa"},"totalSize":5,"files":[{"name":"rgXa.shp","size":5,"sha256":"bb"}]}
            """;
        await File.WriteAllTextAsync(Path.Combine(directory, "latest.json"), packed);

        HttpResponseMessage response = await CreateClient().GetAsync("/v1/basemap/manifest");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(packed, await response.Content.ReadAsStringAsync());
        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("0123456789abcdef", body.RootElement.GetProperty("version").GetString());
    }

    [Fact]
    public async Task Manifest_NeedsNoSignIn()
    {
        await File.WriteAllTextAsync(Path.Combine(directory, "latest.json"), "{}");
        HttpResponseMessage response = await CreateClient().GetAsync("/v1/basemap/manifest");
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
