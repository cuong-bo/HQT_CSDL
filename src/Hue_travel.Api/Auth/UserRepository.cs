using Npgsql;

namespace Hue_travel.Api.Auth;

public sealed class UserRepository(NpgsqlDataSource db)
{
    private const string Columns = "id, username::text, password_hash, display_name, role, status, failed_logins, locked_until";

    public async Task<AppUser?> FindByUsernameAsync(string username, CancellationToken ct)
    {
        // "@u::citext" matters: citext = text would compare as plain, case-sensitive text.
        await using NpgsqlCommand cmd = db.CreateCommand($"select {Columns} from app_user where username = @u::citext");
        cmd.Parameters.AddWithValue("u", username);
        return await ReadOneAsync(cmd, ct);
    }

    public async Task<AppUser?> FindByIdAsync(long id, CancellationToken ct)
    {
        await using NpgsqlCommand cmd = db.CreateCommand($"select {Columns} from app_user where id = @id");
        cmd.Parameters.AddWithValue("id", id);
        return await ReadOneAsync(cmd, ct);
    }

    /// <summary>Creates a user; returns null when the user name is already taken.</summary>
    public async Task<AppUser?> CreateAsync(
        string username, string passwordHash, string displayName, CancellationToken ct, string role = "user", string status = "active")
    {
        await using NpgsqlCommand cmd = db.CreateCommand(
            "insert into app_user (username, password_hash, display_name, role, status) values (@u::citext, @h, @d, @r, @s) returning " + Columns);
        cmd.Parameters.AddWithValue("u", username);
        cmd.Parameters.AddWithValue("h", passwordHash);
        cmd.Parameters.AddWithValue("d", displayName);
        cmd.Parameters.AddWithValue("r", role);
        cmd.Parameters.AddWithValue("s", status);
        try
        {
            return await ReadOneAsync(cmd, ct);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return null;
        }
    }

    /// <summary>Counts a wrong password; after <paramref name="maxFailures"/> of them the account is locked for a while.</summary>
    public async Task RecordFailedLoginAsync(long id, int maxFailures, int lockMinutes, CancellationToken ct)
    {
        await using NpgsqlCommand cmd = db.CreateCommand(
            "update app_user set " +
            "locked_until = case when failed_logins + 1 >= @max then now() + make_interval(mins => @mins) else locked_until end, " +
            "failed_logins = case when failed_logins + 1 >= @max then 0 else failed_logins + 1 end " +
            "where id = @id");
        cmd.Parameters.AddWithValue("max", maxFailures);
        cmd.Parameters.AddWithValue("mins", lockMinutes);
        cmd.Parameters.AddWithValue("id", id);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task RecordLoginSuccessAsync(long id, CancellationToken ct)
    {
        await using NpgsqlCommand cmd = db.CreateCommand(
            "update app_user set failed_logins = 0, locked_until = null, last_login_at = now() where id = @id");
        cmd.Parameters.AddWithValue("id", id);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    /// <summary>Sets a new password, clears the failed-login counter and lock, and ends every session (refresh tokens).</summary>
    public async Task SetPasswordAsync(long id, string passwordHash, CancellationToken ct, bool mustChange = false)
    {
        await using NpgsqlCommand cmd = db.CreateCommand(
            "update app_user set password_hash = @h, failed_logins = 0, locked_until = null, " +
            "status = case when @must then 'must_change_password' when status = 'must_change_password' then 'active' else status end where id = @id");
        cmd.Parameters.AddWithValue("h", passwordHash);
        cmd.Parameters.AddWithValue("must", mustChange);
        cmd.Parameters.AddWithValue("id", id);
        await cmd.ExecuteNonQueryAsync(ct);
        await RevokeRefreshTokensAsync(id, ct);
    }

    public async Task RevokeRefreshTokensAsync(long id, CancellationToken ct)
    {
        await using NpgsqlCommand cmd = db.CreateCommand("update refresh_token set revoked_at = now() where user_id = @id and revoked_at is null");
        cmd.Parameters.AddWithValue("id", id);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task StoreRefreshTokenAsync(long userId, byte[] hash, DateTimeOffset expiresAt, CancellationToken ct)
    {
        await using NpgsqlCommand cmd = db.CreateCommand(
            "insert into refresh_token (user_id, token_hash, expires_at) values (@u, @h, @e)");
        cmd.Parameters.AddWithValue("u", userId);
        cmd.Parameters.AddWithValue("h", hash);
        cmd.Parameters.AddWithValue("e", expiresAt);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    /// <summary>Uses a refresh token once: returns its user id, or null when it is unknown, expired or already used.</summary>
    public async Task<long?> ConsumeRefreshTokenAsync(byte[] hash, CancellationToken ct)
    {
        await using NpgsqlCommand cmd = db.CreateCommand(
            "update refresh_token set revoked_at = now() " +
            "where token_hash = @h and revoked_at is null and expires_at > now() returning user_id");
        cmd.Parameters.AddWithValue("h", hash);
        object? result = await cmd.ExecuteScalarAsync(ct);
        return result is long id ? id : null;
    }

    private static async Task<AppUser?> ReadOneAsync(NpgsqlCommand cmd, CancellationToken ct)
    {
        await using NpgsqlDataReader r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;
        return new AppUser(
            r.GetInt64(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4), r.GetString(5), r.GetInt32(6),
            r.IsDBNull(7) ? null : r.GetFieldValue<DateTimeOffset>(7));
    }
}
