using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Hue_travel.Api.Chat;

/// <summary>
/// Three cheap limits: per-minute rate limiting lives in Program.cs, here are the daily counts and the global token fuse. The
/// counters are in memory: a restart resets them, which is acceptable for the first version and documented.
/// </summary>
public sealed class ChatQuota(IMemoryCache cache, IOptions<ChatOptions> options)
{
    private readonly object gate = new();

    public bool GlobalTokensExceeded
    {
        get
        {
            lock (gate)
            {
                long used = cache.Get<long>(TokenKey());
                return used >= Math.Max(1, options.Value.GlobalTokensPerDay);
            }
        }
    }

    /// <summary>Counts one question and returns how many the caller has left today; false means the daily limit was reached.</summary>
    public bool TryTake(bool guest, string scope, out int remaining)
    {
        ChatOptions settings = options.Value;
        int limit = Math.Max(1, guest ? settings.GuestPerDay : settings.UserPerDay);
        string key = DayKey(guest, scope);
        lock (gate)
        {
            int used = cache.Get<int>(key);
            if (used >= limit)
            {
                remaining = 0;
                return false;
            }
            used++;
            cache.Set(key, used, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(25) });
            remaining = limit - used;
            return true;
        }
    }

    public void AddTokens(long tokens)
    {
        if (tokens <= 0) return;
        lock (gate)
        {
            string key = TokenKey();
            long used = cache.Get<long>(key);
            cache.Set(key, used + tokens, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(25) });
        }
    }

    private static string DayKey(bool guest, string scope) => $"chat-day:{VietnamDay()}:{(guest ? "ip" : "user")}:{scope}";

    private static string TokenKey() => $"chat-tokens:{VietnamDay()}";

    private static string VietnamDay() => DateTime.UtcNow.AddHours(7).ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
}
