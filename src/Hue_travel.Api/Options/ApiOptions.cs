using System.Text;

namespace Hue_travel.Api.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "hue-travel";
    public string Audience { get; set; } = "hue-travel-app";
    public int AccessMinutes { get; set; } = 15;
    public int RefreshDays { get; set; } = 30;

    /// <summary>The signing key comes from the environment (Jwt__Key); the server refuses to start without a strong one.</summary>
    public void Validate()
    {
        if (Encoding.UTF8.GetByteCount(Key) < 32)
            throw new InvalidOperationException("Jwt:Key must be at least 32 bytes long (set the Jwt__Key environment variable).");
        if (AccessMinutes < 1 || RefreshDays < 1)
            throw new InvalidOperationException("Jwt:AccessMinutes and Jwt:RefreshDays must be positive.");
    }
}

public sealed class RateLimitSettings
{
    public int GlobalPerMinute { get; set; } = 300;
    public int AuthPerMinute { get; set; } = 10;
    public int HeavyPerMinute { get; set; } = 30;   // route planning
}

public sealed class ClientSettings
{
    public string MinimumVersion { get; set; } = "1.0.0";
    public string TileUrl { get; set; } = string.Empty;
    public string TileAttribution { get; set; } = string.Empty;
}
