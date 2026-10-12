using System.Text;
using System.Threading.RateLimiting;
using Hue_travel.Api.Area;
using Hue_travel.Api.Auth;
using Hue_travel.Api.Meta;
using Hue_travel.Api.Options;
using Hue_travel.Api.Pois;
using Hue_travel.Api.Routing;
using Hue_travel.Api.Weather;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

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
});
builder.Services.AddAuthorization();

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
});

WebApplication app = builder.Build();
app.Services.GetRequiredService<JwtOptions>();   // fail at start-up, not at the first request, when the key is missing

// The API is only reachable through the reverse proxy on the internal network, so its forwarded address is trusted.
var forwarded = new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto };
forwarded.KnownNetworks.Clear();
forwarded.KnownProxies.Clear();
app.UseForwardedHeaders(forwarded);

app.UseExceptionHandler();
app.Use(async (context, next) =>
{
    // Nothing is cached on the way (the app talks plain HTTP), and browsers may not sniff the content type.
    context.Response.Headers.CacheControl = "no-store";
    context.Response.Headers.XContentTypeOptions = "nosniff";
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

app.Run();

public partial class Program;
