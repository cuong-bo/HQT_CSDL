namespace Hue_travel.Api.Routing;

/// <summary>One opening range of a day; the end may pass 24:00 for a place open overnight.</summary>
public sealed record OpeningRange(int StartMinutes, int EndMinutes);

/// <summary>
/// The server's copy of the free-text opening-hours reader (the app has its own, because it also works offline). It exists to
/// let the route optimizer prefer an order that can actually visit the stops before they close; the app still shows the
/// individual arrival times and warnings.
/// </summary>
public sealed class OpeningHours
{
    private const int DayMinutes = 24 * 60;
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

    // A day trip should not be rearranged to wait overnight; only a short wait for the door to open is useful.
    private static readonly TimeSpan MaxWait = TimeSpan.FromHours(4);

    private static readonly System.Text.RegularExpressions.Regex DayPrefix = new(
        @"^\s*(?:(t[2-7]|cn)(?:\s*-\s*(t[2-7]|cn))?)\s*:?\s*(.*)$",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    private static readonly System.Text.RegularExpressions.Regex HourRange = new(
        @"(\d{1,2})\s*[:h]\s*(\d{1,2})\s*-\s*(\d{1,2})\s*[:h]\s*(\d{1,2})",
        System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    private static readonly System.Text.RegularExpressions.Regex OpenFrom = new(
        @"^mo cua (?:tu|luc)\s+(\d{1,2})\s*[:h]\s*(\d{1,2})",
        System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    private static readonly System.Text.RegularExpressions.Regex CloseAt = new(
        @"^dong cua (?:tu|luc)\s+(\d{1,2})\s*[:h]\s*(\d{1,2})",
        System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    private static readonly System.Text.RegularExpressions.Regex NightHour = new(
        @"^\s*24\s*(?:h|gio)\s*$",
        System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    public bool IsKnown { get; private set; }

    /// <summary>False for the "Mở cửa từ 07:00" form: once open it never reports a closing time.</summary>
    public bool ClosingKnown { get; private set; } = true;

    /// <summary>[0] = Sunday .. [6] = Saturday; an empty list means closed all day.</summary>
    public List<OpeningRange>[] Days { get; } = Enumerable.Range(0, 7).Select(_ => new List<OpeningRange>()).ToArray();

    public static OpeningHours Parse(string? text, string? group)
    {
        var hours = new OpeningHours();
        if (string.Equals(group, "stay", StringComparison.OrdinalIgnoreCase)) return hours;   // check-in / check-out is not an opening time

        string folded = Fold(text);
        if (folded.Length == 0) return hours;
        if (folded.StartsWith("khong") || folded.StartsWith("nen") || folded.Contains("dang cap nhat")) return hours;

        if (folded.Contains("ca ngay") || folded.Contains("24/7") || NightHour.IsMatch(folded))
        {
            hours.OpenAllWeek(0, DayMinutes);
            return hours;
        }

        System.Text.RegularExpressions.Match openFrom = OpenFrom.Match(folded);
        if (openFrom.Success)
        {
            int minutes = Minutes(openFrom.Groups[1].Value, openFrom.Groups[2].Value);
            if (minutes >= 0)
            {
                hours.OpenAllWeek(minutes, DayMinutes);
                hours.ClosingKnown = false;
            }
            return hours;
        }
        if (CloseAt.IsMatch(folded)) return hours;

        int[]? currentDays = null;
        bool any = false;
        foreach (string raw in folded.Split(';'))
        {
            string segment = raw.Trim();
            if (segment.Length == 0) continue;

            System.Text.RegularExpressions.Match day = DayPrefix.Match(segment);
            if (day.Success)
            {
                currentDays = DaysOf(day.Groups[1].Value, day.Groups[2].Value);
                segment = day.Groups[3].Value.Trim();
            }
            else if (currentDays == null)
            {
                currentDays = [0, 1, 2, 3, 4, 5, 6];
            }

            foreach (System.Text.RegularExpressions.Match m in HourRange.Matches(segment))
            {
                int start = Minutes(m.Groups[1].Value, m.Groups[2].Value);
                int end = Minutes(m.Groups[3].Value, m.Groups[4].Value);
                if (start < 0 || end < 0) continue;
                if (end == 0 && start > 0) end = DayMinutes;
                if (end <= start) end += DayMinutes;
                foreach (int dayIndex in currentDays) hours.Days[dayIndex].Add(new OpeningRange(start, end));
                any = true;
            }
        }

        hours.IsKnown = any;
        return hours;
    }

    /// <summary>
    /// The first moment from <paramref name="arrival"/> when the visit can start (waiting for the door to open) and the moment
    /// it ends. A visit that cannot fit within 24 hours of waiting is refused. Unknown hours always answer true.
    /// </summary>
    public bool TryVisit(DateTimeOffset arrival, int stayMinutes, out DateTimeOffset leave)
    {
        TimeSpan stay = TimeSpan.FromMinutes(Math.Max(0, stayMinutes));
        leave = arrival + stay;
        if (!IsKnown) return true;

        DateTimeOffset now = arrival.ToOffset(VietnamOffset);
        List<(DateTimeOffset Start, DateTimeOffset End)> ranges = RangesAround(now);

        if (!ClosingKnown)
        {
            // "Mở cửa từ 07:00": while today's range is running there is no closing time to miss; before it, wait for it.
            var open = ranges.Where(r => r.Start <= now && now < r.End).OrderBy(r => r.End).FirstOrDefault();
            if (open.End > now)
            {
                leave = now + stay;
                return true;
            }
            DateTimeOffset? nextStart = ranges.Where(r => r.Start > now).Select(r => (DateTimeOffset?)r.Start).Min();
            if (nextStart.HasValue && nextStart.Value - now <= MaxWait)
            {
                leave = nextStart.Value + stay;
                return true;
            }
            return false;
        }

        // Still inside a range: a full-day range has no closing time to miss; a normal one must hold the whole visit.
        var current = ranges.Where(r => r.Start <= now && now < r.End).OrderBy(r => r.End).FirstOrDefault();
        if (current.End > now)
        {
            if (current.End - current.Start >= TimeSpan.FromHours(24))
            {
                leave = now + stay;
                return true;
            }
            if (current.End - now >= stay)
            {
                leave = now + stay;
                return true;
            }
            return false;   // already there but would have to leave before the visit ends: do not wait for the next day
        }

        // Not open yet: a short wait for the next range is useful; a wait into tomorrow is not.
        var next = ranges
            .Where(r => r.Start > now && r.End - r.Start >= stay)
            .OrderBy(r => r.Start)
            .FirstOrDefault();
        if (next.Start != default && next.Start - now <= MaxWait)
        {
            leave = next.Start + stay;
            return true;
        }
        return false;
    }

    private void OpenAllWeek(int start, int end)
    {
        for (int day = 0; day < Days.Length; day++) Days[day].Add(new OpeningRange(start, end));
        IsKnown = true;
    }

    private List<(DateTimeOffset Start, DateTimeOffset End)> RangesAround(DateTimeOffset now)
    {
        var result = new List<(DateTimeOffset, DateTimeOffset)>();
        for (int offset = -1; offset <= 8; offset++)
        {
            DateTime date = now.Date.AddDays(offset);
            int day = (int)date.DayOfWeek;   // Sunday = 0, the same order as Days
            DateTime baseDate = DateTime.SpecifyKind(date, DateTimeKind.Unspecified);
            foreach (OpeningRange range in Days[day])
            {
                result.Add((
                    new DateTimeOffset(baseDate.AddMinutes(range.StartMinutes), VietnamOffset),
                    new DateTimeOffset(baseDate.AddMinutes(range.EndMinutes), VietnamOffset)));
            }
        }
        return result;
    }

    private static int[] DaysOf(string first, string second)
    {
        int from = DayNumber(first);
        if (string.IsNullOrEmpty(second)) return [from];
        int to = DayNumber(second);
        var days = new List<int>();
        for (int day = from, guard = 0; guard < 7; day = (day + 1) % 7, guard++)
        {
            days.Add(day);
            if (day == to) break;
        }
        return days.ToArray();
    }

    private static int DayNumber(string text)
    {
        if (text.Equals("cn", StringComparison.OrdinalIgnoreCase)) return 0;
        return text.Length == 2 && int.TryParse(text[1..], out int number) && number is >= 2 and <= 7 ? number - 1 : 1;
    }

    private static int Minutes(string hour, string minute)
    {
        if (!int.TryParse(hour, out int h) || !int.TryParse(minute, out int m)) return -1;
        if (h == 24 && m == 0) return DayMinutes;
        return h is >= 0 and <= 23 && m is >= 0 and <= 59 ? h * 60 + m : -1;
    }

    private static string Fold(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        string normalized = text.Normalize(System.Text.NormalizationForm.FormD);
        var builder = new System.Text.StringBuilder(normalized.Length);
        foreach (char c in normalized)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.NonSpacingMark) continue;
            builder.Append(c);
        }
        return builder.ToString()
            .Replace('đ', 'd').Replace('Đ', 'D')
            .Replace('–', '-').Replace('—', '-').Replace('−', '-')
            .ToLowerInvariant()
            .Trim();
    }
}
