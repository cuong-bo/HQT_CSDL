using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Hue_travel.Api.Tests;

/// <summary>The client manifest does not touch the database, so these tests run without HUE_TEST_DB.</summary>
public sealed class ClientUpdateTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "hue-client-" + Guid.NewGuid().ToString("N"));

    public ClientUpdateTests() => Directory.CreateDirectory(directory);

    public void Dispose() => Directory.Delete(directory, recursive: true);

    private HttpClient CreateClient() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Default", "Host=localhost;Database=unused");
            builder.UseSetting("Jwt:Key", ApiFixture.JwtKey);
            builder.UseSetting("ClientUpdate:Directory", directory);
            builder.UseSetting("RateLimit:GlobalPerMinute", "100000");
        }).CreateClient();

    [Fact]
    public async Task Manifest_IsNotFoundUntilAnInstallerHasBeenPublished()
    {
        HttpResponseMessage response = await CreateClient().GetAsync("/v1/client/manifest");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("publish-client", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Manifest_IsSentExactlyAsPublished_WithoutSignIn()
    {
        const string published = """
            {"version":"1.1.0","required":true,"installer":{"path":"/static/client/1.1.0/HueTravel-Setup-1.1.0.exe","size":3,"sha256":"aa"},"notes":"","publishedAt":"2026-01-01T00:00:00Z"}
            """;
        await File.WriteAllTextAsync(Path.Combine(directory, "latest.json"), published);

        HttpResponseMessage response = await CreateClient().GetAsync("/v1/client/manifest");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(published, await response.Content.ReadAsStringAsync());
    }
}
