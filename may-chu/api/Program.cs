using System.Text;
using System.Threading.RateLimiting;
using Hue_travel.Api.Admin;
using Hue_travel.Api.Area;
using Hue_travel.Api.Auth;
using Hue_travel.Api.Meta;
using Hue_travel.Api.Options;
using Hue_travel.Api.Owner;
using Hue_travel.Api.Photos;
using Hue_travel.Api.Pois;
using Hue_travel.Api.Routing;
using Hue_travel.Api.Security;
using Hue_travel.Api.Social;
using Hue_travel.Api.Weather;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Every body this API reads is small JSON; refuse anything bigger before it is parsed.
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 64 * 1024);

// Settings are read when the services are first used, so tests and the environment can override them.
builder.Services.AddSingleton(sp =>
{
    JwtOptions jwt = sp.GetRequiredService<IConfiguration>().GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
    jwt.Validate();
    return jwt;
});
builder.Services.AddSingleton(sp =>
{
    string connection = sp.GetRequiredService<IConfiguration>().GetConnectionString("Default")
        ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured (set ConnectionStrings__Default).");
    return new NpgsqlDataSourceBuilder(connection).Build();
});
builder.Services.Configure<ClientSettings>(builder.Configuration.GetSection("Client"));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ResponseSigner>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<UserRepository>();
builder.Services.AddSingleton<PoiRepository>();
builder.Services.AddSingleton<RouteRepository>();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient("openmeteo", client => client.Timeout = TimeSpan.FromSeconds(10));
builder.Services.AddProblemDetails();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme).Configure<JwtOptions>((options, jwt) =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwt.Issuer,
        ValidateAudience = true,
        ValidAudience = jwt.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30),
        NameClaimType = "name",
        RoleClaimType = "role",
    };
    options.Events = new JwtBearerEvents { OnTokenValidated = Program.CheckAccountStillAllowedAsync };
});
builder.Services.AddAuthorization(options => options.AddPolicy("admin", policy => policy.RequireRole("admin")));

builder.Services.AddRateLimiter(_ => { });   // the options are filled in below, once the configuration is available
builder.Services.AddOptions<RateLimiterOptions>().Configure<IConfiguration>((options, configuration) =>
{
    RateLimitSettings limits = configuration.GetSection("RateLimit").Get<RateLimitSettings>() ?? new RateLimitSettings();
    static string Client(HttpContext context) => context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    FixedWindowRateLimiterOptions Window(int permits) => new() { PermitLimit = permits, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 };

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
        context => RateLimitPartition.GetFixedWindowLimiter(Client(context), _ => Window(limits.GlobalPerMinute)));
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(Client(context), _ => Window(limits.AuthPerMinute)));
    options.AddPolicy("heavy", context => RateLimitPartition.GetFixedWindowLimiter(Client(context), _ => Window(limits.HeavyPerMinute)));
});

WebApplication app = builder.Build();
app.Services.GetRequiredService<JwtOptions>();   // fail at start-up, not at the first request, when the key is missing

// The client address comes from X-Forwarded-For only when the request arrives from a configured proxy network
// (Proxy:KnownNetworks, e.g. "172.30.0.0/24" for Caddy); otherwise only loopback is trusted, so the header cannot be forged.
var forwarded = new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto };
string[] proxyNetworks = app.Configuration.GetSection("Proxy:KnownNetworks").Get<string[]>() ?? [];
if (proxyNetworks.Length > 0)
{
    forwarded.KnownNetworks.Clear();
    forwarded.KnownProxies.Clear();
    foreach (string cidr in proxyNetworks)
    {
        string[] parts = cidr.Split('/');
        forwarded.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(System.Net.IPAddress.Parse(parts[0]), int.Parse(parts[1])));
    }
}
app.UseForwardedHeaders(forwarded);

app.UseResponseSigning();   // outside the exception handler, so error replies are signed too
app.UseExceptionHandler();
app.Use(async (context, next) =>
{
    // Nothing is cached on the way (the app talks plain HTTP), and browsers may not sniff the content type.
    context.Response.Headers.CacheControl = "no-store";
    context.Response.Headers.XContentTypeOptions = "nosniff";
    await next();
});
app.Use(async (context, next) =>
{
    // Only photo uploads and the place import carry more than a few kilobytes.
    long? bigger = PhotoEndpoints.IsUploadRequest(context.Request) ? PhotoEndpoints.MaxBodyBytes
        : HttpMethods.IsPost(context.Request.Method) && context.Request.Path == "/v1/admin/pois/import" ? Hue_travel.Api.Admin.AdminEndpoints.MaxImportBodyBytes : null;
    var sizeLimit = bigger == null ? null : context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature>();
    if (sizeLimit is { IsReadOnly: false }) sizeLimit.MaxRequestBodySize = bigger;   // the in-memory test server has no such limit
    await next();
});
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseStatusCodePages();

app.MapMetaEndpoints();
app.MapAuthEndpoints();
app.MapPoiEndpoints();
app.MapAreaEndpoints();
app.MapRouteEndpoints();
app.MapWeatherEndpoints();
app.MapSocialEndpoints();
app.MapOwnerEndpoints();
app.MapPhotoEndpoints();
app.MapAdminEndpoints();

app.Run();

public partial class Program
{
    private static readonly string[] PasswordChangePaths = ["/v1/me", "/v1/me/password"];

    /// <summary>
    /// A token is only as good as the account behind it: a locked account, a demoted admin, or an account that still has
    /// a temporary password (it may only change it) is refused even while the token itself has not expired.
    /// The answer is cached for 20 seconds so this does not cost a query per request.
    /// </summary>
    internal static async Task CheckAccountStillAllowedAsync(TokenValidatedContext context)
    {
        if (!long.TryParse(context.Principal?.FindFirstValue("sub"), out long id)) { context.Fail("bad subject"); return; }
        UserRepository users = context.HttpContext.RequestServices.GetRequiredService<UserRepository>();
        IMemoryCache cache = context.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();
        UserState? state = await cache.GetOrCreateAsync($"user-state:{id}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(20);
            return await users.GetStateAsync(id, context.HttpContext.RequestAborted);
        });

        if (state == null || state.Status == "locked") { context.Fail("account not allowed"); return; }
        if (context.Principal!.FindFirstValue("role") == "admin" && state.Role != "admin") { context.Fail("no longer an admin"); return; }
        if (state.Status == "must_change_password" && !PasswordChangePaths.Contains(context.HttpContext.Request.Path.Value))
            context.Fail("password change required");
    }
}
