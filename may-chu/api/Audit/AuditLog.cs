using System.Security.Claims;
using System.Text.Json;
using Hue_travel.Api.Data;
using Npgsql;

namespace Hue_travel.Api.Audit;

public static class AuditLog
{
    /// <summary>Who did what to which record, with the old and new values. Written for every change of shared data.</summary>
    public static Task RecordAsync(
        this NpgsqlDataSource db, long? userId, string action, string entity, string? entityId, object? oldValue, object? newValue, CancellationToken ct) =>
        db.ExecAsync(
            "insert into audit_log (user_id, action, entity, entity_id, old_value, new_value) values (@u, @a, @e, @i, @o::jsonb, @n::jsonb)", ct,
            ("u", userId), ("a", action), ("e", entity), ("i", entityId),
            ("o", oldValue == null ? null : JsonSerializer.Serialize(oldValue)),
            ("n", newValue == null ? null : JsonSerializer.Serialize(newValue)));
}

public static class CurrentUser
{
    /// <summary>The signed-in user's id, taken from the validated token.</summary>
    public static long UserId(this ClaimsPrincipal principal) => long.Parse(principal.FindFirstValue("sub")!);
}
