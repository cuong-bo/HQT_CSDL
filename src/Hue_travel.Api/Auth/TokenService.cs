using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Hue_travel.Api.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Hue_travel.Api.Auth;

public sealed class TokenService(JwtOptions options, TimeProvider clock)
{
    /// <summary>A signed access token (15 minutes by default) carrying the user id, name and role.</summary>
    public (string Token, int ExpiresInSeconds) CreateAccessToken(AppUser user)
    {
        DateTimeOffset now = clock.GetUtcNow();
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim("sub", user.Id.ToString()),
                new Claim("name", user.Username),
                new Claim("role", user.Role),
                new Claim("display_name", user.DisplayName),
            ]),
            Issuer = options.Issuer,
            Audience = options.Audience,
            NotBefore = now.UtcDateTime,
            Expires = now.AddMinutes(options.AccessMinutes).UtcDateTime,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key)), SecurityAlgorithms.HmacSha256),
        };
        return (new JsonWebTokenHandler().CreateToken(descriptor), options.AccessMinutes * 60);
    }

    public DateTimeOffset RefreshExpiry() => clock.GetUtcNow().AddDays(options.RefreshDays);

    /// <summary>A random refresh token for the client and the hash that is stored in the database.</summary>
    public static (string Token, byte[] Hash) NewRefreshToken()
    {
        string token = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
        return (token, HashRefreshToken(token));
    }

    public static byte[] HashRefreshToken(string token) => SHA256.HashData(Encoding.UTF8.GetBytes(token));
}
