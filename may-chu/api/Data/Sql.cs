using Npgsql;

namespace Hue_travel.Api.Data;

/// <summary>Tiny helpers so endpoint code reads as "run this SQL with these parameters".</summary>
public static class Sql
{
    public static NpgsqlCommand Command(this NpgsqlDataSource db, string sql, (string Name, object? Value)[] args)
    {
        NpgsqlCommand cmd = db.CreateCommand(sql);
        Fill(cmd, args);
        return cmd;
    }

    public static void Fill(NpgsqlCommand cmd, (string Name, object? Value)[] args)
    {
        foreach ((string name, object? value) in args) cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
    }

    public static async Task<int> ExecAsync(this NpgsqlDataSource db, string sql, CancellationToken ct, params (string Name, object? Value)[] args)
    {
        await using NpgsqlCommand cmd = db.Command(sql, args);
        return await cmd.ExecuteNonQueryAsync(ct);
    }

    public static async Task<object?> ScalarAsync(this NpgsqlDataSource db, string sql, CancellationToken ct, params (string Name, object? Value)[] args)
    {
        await using NpgsqlCommand cmd = db.Command(sql, args);
        object? value = await cmd.ExecuteScalarAsync(ct);
        return value is DBNull ? null : value;
    }

    public static async Task<List<T>> ListAsync<T>(
        this NpgsqlDataSource db, string sql, Func<NpgsqlDataReader, T> map, CancellationToken ct, params (string Name, object? Value)[] args)
    {
        await using NpgsqlCommand cmd = db.Command(sql, args);
        await using NpgsqlDataReader r = await cmd.ExecuteReaderAsync(ct);
        var items = new List<T>();
        while (await r.ReadAsync(ct)) items.Add(map(r));
        return items;
    }

    public static async Task<bool> ExistsAsync(this NpgsqlDataSource db, string sql, CancellationToken ct, params (string Name, object? Value)[] args) =>
        await db.ScalarAsync(sql, ct, args) is true;

    public static string? Text(this NpgsqlDataReader r, int i) => r.IsDBNull(i) ? null : r.GetString(i);
}
