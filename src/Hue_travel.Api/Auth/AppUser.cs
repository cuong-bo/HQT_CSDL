namespace Hue_travel.Api.Auth;

public sealed record AppUser(
    long Id,
    string Username,
    string PasswordHash,
    string DisplayName,
    string Role,
    string Status,
    int FailedLogins,
    DateTimeOffset? LockedUntil);

public sealed record UserDto(long Id, string Username, string DisplayName, string Role);

public sealed record RegisterRequest(string? Username, string? Password, string? DisplayName);

public sealed record LoginRequest(string? Username, string? Password);

public sealed record RefreshRequest(string? RefreshToken);

public sealed record AuthResponse(string AccessToken, string RefreshToken, int ExpiresIn, UserDto User, bool MustChangePassword);
