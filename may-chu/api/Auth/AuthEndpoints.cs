using System.Security.Claims;
using Hue_travel.Api.Audit;
using Hue_travel.Api.Data;
using Npgsql;

namespace Hue_travel.Api.Auth;

public static class AuthEndpoints
{
    private const int MaxFailedLogins = 5;
    private const int LockMinutes = 15;
    private const string BadLogin = "Sai tên đăng nhập hoặc mật khẩu.";

    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder auth = app.MapGroup("/v1/auth").RequireRateLimiting("auth");
        auth.MapPost("/register", RegisterAsync);
        auth.MapPost("/login", LoginAsync);
        auth.MapPost("/refresh", RefreshAsync);
        auth.MapPost("/logout", LogoutAsync);

        app.MapGet("/v1/me", MeAsync).RequireAuthorization();
    }

    private const int MaxPlacesAtRegistration = 5;

    private static async Task<IResult> RegisterAsync(RegisterRequest request, UserRepository users, TokenService tokens, NpgsqlDataSource db, CancellationToken ct)
    {
        string? problem = AuthValidation.ValidateRegistration(request.Username, request.Password, request.DisplayName);
        if (problem != null) return Results.Problem(detail: problem, statusCode: StatusCodes.Status400BadRequest);

        // An owner names the places they run. They start as an ordinary user; an admin approves each claim before they can manage the place.
        string accountType = request.AccountType ?? "user";
        if (accountType is not ("user" or "owner"))
            return Results.Problem(detail: "Loại tài khoản phải là user hoặc owner.", statusCode: StatusCodes.Status400BadRequest);
        string[] placeIds = (request.PlaceIds ?? []).Select(p => p.Trim()).Where(p => p.Length > 0).Distinct().ToArray();
        if (accountType == "owner")
        {
            if (placeIds.Length is < 1 or > MaxPlacesAtRegistration)
                return Results.Problem(detail: $"Chủ quản cần chọn từ 1 đến {MaxPlacesAtRegistration} địa điểm mình quản lý.", statusCode: StatusCodes.Status400BadRequest);
            long found = (long)(await db.ScalarAsync("select count(*) from poi where id = any(@ids) and deleted_at is null and status = 'visible'", ct, ("ids", placeIds)))!;
            if (found != placeIds.Length)
                return Results.Problem(detail: "Có địa điểm không tồn tại hoặc đang ẩn.", statusCode: StatusCodes.Status404NotFound);
        }
        else
        {
            placeIds = [];
        }

        string username = request.Username!.Trim();
        AppUser? user = await users.CreateAsync(
            username, PasswordHasher.Hash(request.Password!), AuthValidation.DisplayNameOrUsername(request.DisplayName, username), ct);
        if (user == null) return Results.Problem(detail: "Tên đăng nhập đã có người dùng.", statusCode: StatusCodes.Status409Conflict);

        foreach (string placeId in placeIds)
        {
            await db.ExecAsync("insert into place_owner (user_id, poi_id) values (@u, @p) on conflict do nothing", ct, ("u", user.Id), ("p", placeId));
            await db.RecordAsync(user.Id, "claim.create", "place_owner", placeId, null, new { poiId = placeId, atRegistration = true }, ct);
        }
        return Results.Json(await IssueAsync(user, users, tokens, ct), statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> LoginAsync(LoginRequest request, UserRepository users, TokenService tokens, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrEmpty(request.Password))
            return Results.Problem(detail: BadLogin, statusCode: StatusCodes.Status401Unauthorized);

        AppUser? user = await users.FindByUsernameAsync(request.Username.Trim(), ct);
        if (user == null)
        {
            PasswordHasher.SpendTimeLikeAVerification();
            return Results.Problem(detail: BadLogin, statusCode: StatusCodes.Status401Unauthorized);
        }

        // The attempt is counted first, atomically; while the account is locked the password is not even checked.
        if (!await users.TryReserveAttemptAsync(user.Id, MaxFailedLogins, LockMinutes, ct))
        {
            PasswordHasher.SpendTimeLikeAVerification();
            return Results.Problem(detail: "Đăng nhập sai nhiều lần, tài khoản tạm khóa. Hãy thử lại sau.", statusCode: StatusCodes.Status423Locked);
        }
        if (!PasswordHasher.Verify(request.Password, user.PasswordHash))
            return Results.Problem(detail: BadLogin, statusCode: StatusCodes.Status401Unauthorized);

        // Only someone who knows the password learns that an admin locked the account.
        if (user.Status == "locked")
            return Results.Problem(detail: "Tài khoản đã bị khóa. Hãy liên hệ quản trị viên.", statusCode: StatusCodes.Status403Forbidden);

        await users.RecordLoginSuccessAsync(user.Id, ct);
        return Results.Ok(await IssueAsync(user, users, tokens, ct));
    }

    private static async Task<IResult> RefreshAsync(RefreshRequest request, UserRepository users, TokenService tokens, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return Results.Problem(detail: "Thiếu mã làm mới.", statusCode: StatusCodes.Status400BadRequest);

        long? userId = await users.ConsumeRefreshTokenAsync(TokenService.HashRefreshToken(request.RefreshToken), ct);
        AppUser? user = userId is { } id ? await users.FindByIdAsync(id, ct) : null;
        if (user == null || user.Status == "locked")
            return Results.Problem(detail: "Phiên đăng nhập đã hết hạn, hãy đăng nhập lại.", statusCode: StatusCodes.Status401Unauthorized);

        return Results.Ok(await IssueAsync(user, users, tokens, ct));
    }

    /// <summary>Ends a session: the refresh token can no longer be used. Always succeeds, so the app can sign out while offline-ish.</summary>
    private static async Task<IResult> LogoutAsync(RefreshRequest request, UserRepository users, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
            await users.ConsumeRefreshTokenAsync(TokenService.HashRefreshToken(request.RefreshToken), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> MeAsync(ClaimsPrincipal principal, UserRepository users, CancellationToken ct)
    {
        string? sub = principal.FindFirstValue("sub");
        AppUser? user = long.TryParse(sub, out long id) ? await users.FindByIdAsync(id, ct) : null;
        return user == null
            ? Results.Problem(detail: "Tài khoản không còn tồn tại.", statusCode: StatusCodes.Status401Unauthorized)
            : Results.Ok(new MeDto(user.Id, user.Username, user.DisplayName, user.Role, user.Status == "must_change_password"));
    }

    private static async Task<AuthResponse> IssueAsync(AppUser user, UserRepository users, TokenService tokens, CancellationToken ct)
    {
        (string access, int expiresIn) = tokens.CreateAccessToken(user);
        (string refresh, byte[] hash) = TokenService.NewRefreshToken();
        await users.StoreRefreshTokenAsync(user.Id, hash, tokens.RefreshExpiry(), ct);
        return new AuthResponse(access, refresh, expiresIn, new UserDto(user.Id, user.Username, user.DisplayName, user.Role),
            user.Status == "must_change_password");
    }
}
