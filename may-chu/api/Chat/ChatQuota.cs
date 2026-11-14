using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Hue_travel.Api.Chat;

/// <summary>
/// Limits per day, because every question costs money: a number of questions per account (a smaller one per client address for a guest)
/// and a fuse on the tokens the whole server may use in a day. Kept in memory: a restart gives everybody a fresh day, which is acceptable.
/// The day is the Vietnamese day.
/// </summary>
public sealed class ChatQuota(IMemoryCache cache, IOptions<ChatOptions> options, TimeProvider clock)
{
    private sealed class Counter { public long Value; }

    private readonly object gate = new();

    private static string DayOf(DateTimeOffset now) => now.ToOffset(TimeSpan.FromHours(7)).ToString("yyyyMMdd");

    private Counter Get(string key)
    {
        return cache.GetOrCreate(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(26);
            return new Counter();
        })!;
    }

    /// <summary>True while the server as a whole is under its token budget for today.</summary>
    public bool BudgetOpen()
    {
        lock (gate) return Get("chat-tokens:" + DayOf(clock.GetUtcNow())).Value < options.Value.GlobalTokensPerDay;
    }

    public void AddTokens(long tokens)
    {
        if (tokens <= 0) return;
        lock (gate) Get("chat-tokens:" + DayOf(clock.GetUtcNow())).Value += tokens;
    }

    /// <summary>Takes one question from today's allowance; false when it is used up. <paramref name="remaining"/> is what is left after this one.</summary>
    public bool TryTake(string subject, bool signedIn, out int remaining)
    {
        int limit = signedIn ? options.Value.UserPerDay : options.Value.GuestPerDay;
        lock (gate)
        {
            Counter used = Get("chat-day:" + DayOf(clock.GetUtcNow()) + ":" + subject);
            if (used.Value >= limit) { remaining = 0; return false; }
            used.Value++;
            remaining = (int)(limit - used.Value);
            return true;
        }
    }

    /// <summary>Gives the question back (the model could not answer: the person should not pay for the server's trouble).</summary>
    public void Refund(string subject)
    {
        lock (gate)
        {
            Counter used = Get("chat-day:" + DayOf(clock.GetUtcNow()) + ":" + subject);
            if (used.Value > 0) used.Value--;
        }
    }
}
