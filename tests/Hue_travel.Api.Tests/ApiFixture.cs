using Microsoft.AspNetCore.Mvc.Testing;

namespace Hue_travel.Api.Tests;

/// <summary>Integration tests need a database built by db/run.sh; point HUE_TEST_DB at it (an Npgsql connection string).</summary>
public sealed class RequiresDbFactAttribute : FactAttribute
{
    public RequiresDbFactAttribute()
    {
        if (string.IsNullOrEmpty(ApiFixture.ConnectionString)) Skip = "Set HUE_TEST_DB to run the database tests.";
    }
}

public sealed class ApiFixture : IDisposable
{
    public const string JwtKey = "test-only-signing-key-0123456789-abcdef";

    public static string? ConnectionString { get; } = Environment.GetEnvironmentVariable("HUE_TEST_DB");

    public WebApplicationFactory<Program> Factory { get; }

    public ApiFixture()
    {
        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Default", ConnectionString ?? "Host=localhost;Database=unused");
            builder.UseSetting("Jwt:Key", JwtKey);
            builder.UseSetting("RateLimit:AuthPerMinute", "1000");
            builder.UseSetting("RateLimit:GlobalPerMinute", "100000");
            builder.UseSetting("RateLimit:HeavyPerMinute", "1000");
        });
    }

    public HttpClient CreateClient() => Factory.CreateClient();

    public void Dispose() => Factory.Dispose();
}

[CollectionDefinition("api")]
public sealed class ApiCollection : ICollectionFixture<ApiFixture>;
