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

public sealed record UserState(string Role, string Status);

public sealed record MeDto(long Id, string Username, string DisplayName, string Role, bool MustChangePassword);

public sealed record UserDto(long Id, string Username, string DisplayName, string Role);

// AccountType is "user" (the default) or "owner"; an owner names the places they run (PlaceIds), each becoming a claim an admin decides on.
public sealed record RegisterRequest(string? Username, string? Password, string? DisplayName, string? AccountType = null, string[]? PlaceIds = null);

public sealed record LoginRequest(string? Username, string? Password);

public sealed record RefreshRequest(string? RefreshToken);

public sealed record AuthResponse(string AccessToken, string RefreshToken, int ExpiresIn, UserDto User, bool MustChangePassword);
