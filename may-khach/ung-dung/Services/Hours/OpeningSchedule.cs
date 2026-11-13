using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Hue_travel.Models;

namespace Hue_travel.Services.Hours
{
    /// <summary>The parts of the day a place is open; minutes from midnight, the end may pass 24:00 for a place open overnight.</summary>
    public sealed class TimeRange
    {
        public TimeRange(int startMinutes, int endMinutes)
        {
            StartMinutes = startMinutes;
            EndMinutes = endMinutes;
        }

        public int StartMinutes { get; }

        public int EndMinutes { get; }
    }

    public enum OpenState
    {
        Unknown,
        Open,
        ClosingSoon,
        Closed,
    }

    /// <summary>What the schedule says at one moment, and when that answer next changes (null when it never does or is unknown).</summary>
    public sealed class OpenStatus
    {
        public OpenStatus(OpenState state, DateTime? nextChange)
        {
            State = state;
            NextChange = nextChange;
        }

        public OpenState State { get; }

        /// <summary>The next opening or closing time, in the same (Vietnam) clock as the moment asked about.</summary>
        public DateTime? NextChange { get; }
    }

    /// <summary>
    /// Turns the free-text opening hours of the source data into a weekly schedule and answers "is it open now?" and "can I finish
    /// a visit that starts then?". Rules:
    ///   - "Cả ngày", "24/7", "24h": open all week.
    ///   - "Không cố định", "Nên tham quan ban ngày", "Đang cập nhật", empty: unknown, never reported as closed.
    ///   - "T2-T6 08:00-22:00; T7-CN 08:00-23:00": day groups, with several ranges per group.
    ///   - "18:00-02:00": a range that passes midnight.
    ///   - a stay's check-in/check-out text is not an opening schedule: unknown.
    /// </summary>
    public sealed class OpeningSchedule
    {
        private static readonly Regex DayPrefix = new Regex(
            @"^\s*(?:(t[2-7]|cn)(?:\s*-\s*(t[2-7]|cn))?)\s*:?\s*(.*)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Regex HourRange = new Regex(
            @"(\d{1,2})\s*[:h]\s*(\d{1,2})\s*-\s*(\d{1,2})\s*[:h]\s*(\d{1,2})", RegexOptions.CultureInvariant);

        private static readonly Regex OpenFrom = new Regex(
            @"^mo cua (?:tu|luc)\s+(\d{1,2})\s*[:h]\s*(\d{1,2})", RegexOptions.CultureInvariant);

        private static readonly Regex CloseAt = new Regex(
            @"^dong cua (?:tu|luc)\s+(\d{1,2})\s*[:h]\s*(\d{1,2})", RegexOptions.CultureInvariant);

        private static readonly Regex NightHour = new Regex(@"\b24\s*(?:h|gio)\b", RegexOptions.CultureInvariant);

        private static readonly Lazy<TimeZoneInfo> VietnamZone = new Lazy<TimeZoneInfo>(FindVietnamZone);

        public OpeningSchedule()
        {
            Days = new List<TimeRange>[7];
            for (int i = 0; i < Days.Length; i++) Days[i] = new List<TimeRange>();
        }

        /// <summary>False for "Đang cập nhật", "Không cố định", an empty text, or a stay's check-in/check-out text.</summary>
        public bool IsKnown { get; private set; }

        /// <summary>False when only the opening time was given ("Mở cửa từ 07:00"): the place never reports as closed.</summary>
        public bool ClosingKnown { get; private set; } = true;

        /// <summary>[0] = Sunday .. [6] = Saturday; an empty list means closed all day.</summary>
        public List<TimeRange>[] Days { get; }

        /// <summary>The current time in Vietnam, independent of the clock's time zone (a visitor may travel from abroad).</summary>
        public static DateTime NowInVietnam => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamZone.Value);

        public static OpeningSchedule Parse(string text, PoiCategory category)
        {
            var schedule = new OpeningSchedule();
            if (category == PoiCategory.Stay) return schedule;   // check-in / check-out is not an opening time

            string folded = Fold(text);
            if (folded.Length == 0) return schedule;
            if (folded.StartsWith("khong") || folded.StartsWith("nen") || folded.Contains("dang cap nhat")) return schedule;

            // "Cả ngày", "24/7", "24h": every day, all day.
            if (folded.Contains("ca ngay") || folded.Contains("24/7") || NightHour.IsMatch(folded))
            {
                schedule.OpenAllWeek(0, 24 * 60);
                return schedule;
            }

            // Services often store the two times separately; ShapefilePoiRepository makes "07:00 - 21:00", "Mở cửa từ 07:00"
            // or "Đóng cửa lúc 21:00" from them.
            Match openFrom = OpenFrom.Match(folded);
            if (openFrom.Success)
            {
                int minutes = Minutes(openFrom.Groups[1].Value, openFrom.Groups[2].Value);
                if (minutes >= 0)
                {
                    schedule.OpenAllWeek(minutes, 24 * 60);
                    schedule.ClosingKnown = false;
                }
                return schedule;
            }
            if (CloseAt.IsMatch(folded)) return schedule;   // a closing time alone says nothing about when it opens

            // Segments split by ";" share the last day group: "T3-CN: 07:30-11:30; 13:00-17:00".
            int[] currentDays = null;
            bool anyRange = false;
            foreach (string raw in folded.Split(';'))
            {
                string segment = raw.Trim();
                if (segment.Length == 0) continue;

                Match dayMatch = DayPrefix.Match(segment);
                if (dayMatch.Success)
                {
                    currentDays = DaysOf(dayMatch.Groups[1].Value, dayMatch.Groups[2].Value);
                    segment = dayMatch.Groups[3].Value.Trim();
                }
                else if (currentDays == null)
                {
                    currentDays = new[] { 0, 1, 2, 3, 4, 5, 6 };
                }

                foreach (Match m in HourRange.Matches(segment))
                {
                    int start = Minutes(m.Groups[1].Value, m.Groups[2].Value);
                    int end = Minutes(m.Groups[3].Value, m.Groups[4].Value);
                    if (start < 0 || end < 0) continue;
                    if (end == 0 && start > 0) end = 24 * 60;              // "18:00-00:00"
                    if (end <= start) end += 24 * 60;                     // passes midnight
                    foreach (int day in currentDays) schedule.Days[day].Add(new TimeRange(start, end));
                    anyRange = true;
                }
            }

            schedule.IsKnown = anyRange;
            return schedule;
        }

        /// <summary>The state at a given moment of the Vietnam clock.</summary>
        public OpenStatus StatusAt(DateTime vietnamTime)
        {
            if (!IsKnown) return new OpenStatus(OpenState.Unknown, null);

            List<AbsoluteRange> ranges = RangesAround(vietnamTime);
            AbsoluteRange current = ranges.FirstOrDefault(r => r.Start <= vietnamTime && vietnamTime < r.End);
            if (current != null)
            {
                // "Cả ngày" has no meaningful closing time: it stays open right through midnight.
                if (current.End - current.Start >= TimeSpan.FromHours(24)) return new OpenStatus(OpenState.Open, null);
                TimeSpan left = current.End - vietnamTime;
                OpenState state = ClosingKnown && left <= TimeSpan.FromMinutes(60) ? OpenState.ClosingSoon : OpenState.Open;
                return new OpenStatus(state, ClosingKnown ? current.End : (DateTime?)null);
            }

            AbsoluteRange next = ranges.Where(r => r.Start > vietnamTime).OrderBy(r => r.Start).FirstOrDefault();
            return new OpenStatus(OpenState.Closed, next?.Start);
        }

        /// <summary>
        /// True when a visit that starts at <paramref name="arrive"/> and lasts <paramref name="stay"/> can finish inside one
        /// opening range. Unknown hours answer true: the app must never claim a place is closed when it does not know.
        /// </summary>
        public bool IsOpenForVisit(DateTime arrive, TimeSpan stay)
        {
            if (!IsKnown) return true;
            OpenStatus status = StatusAt(arrive);
            if (status.State != OpenState.Open && status.State != OpenState.ClosingSoon) return false;
            if (status.NextChange == null) return true;
            return arrive + stay <= status.NextChange.Value;
        }

        private void OpenAllWeek(int start, int end)
        {
            for (int day = 0; day < Days.Length; day++) Days[day].Add(new TimeRange(start, end));
            IsKnown = true;
        }

        private List<AbsoluteRange> RangesAround(DateTime time)
        {
            var result = new List<AbsoluteRange>();
            // Yesterday catches a range that passed midnight; a week ahead is enough to name the next opening.
            for (int offset = -1; offset <= 8; offset++)
            {
                DateTime date = time.Date.AddDays(offset);
                int day = (int)date.DayOfWeek;   // Sunday = 0, the same order as Days
                foreach (TimeRange range in Days[day])
                {
                    result.Add(new AbsoluteRange(date.AddMinutes(range.StartMinutes), date.AddMinutes(range.EndMinutes)));
                }
            }
            return result;
        }

        private sealed class AbsoluteRange
        {
            public AbsoluteRange(DateTime start, DateTime end)
            {
                Start = start;
                End = end;
            }

            public DateTime Start { get; }
            public DateTime End { get; }
        }

        // "T7-CN" wraps over Sunday; "T2-T6" is the usual Monday..Friday.
        private static int[] DaysOf(string first, string second)
        {
            int from = DayNumber(first);
            if (string.IsNullOrEmpty(second)) return new[] { from };
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
            int number;
            if (text.Length == 2 && int.TryParse(text.Substring(1), out number) && number >= 2 && number <= 7) return number - 1;
            return 1;
        }

        // "7", "07" hour and minute as minutes after midnight; -1 when it is not a real time.
        private static int Minutes(string hour, string minute)
        {
            int h, m;
            if (!int.TryParse(hour, NumberStyles.Integer, CultureInfo.InvariantCulture, out h)) return -1;
            if (!int.TryParse(minute, NumberStyles.Integer, CultureInfo.InvariantCulture, out m)) return -1;
            if (h == 24 && m == 0) return 24 * 60;
            if (h < 0 || h > 23 || m < 0 || m > 59) return -1;
            return h * 60 + m;
        }

        private static TimeZoneInfo FindVietnamZone()
        {
            foreach (string id in new[] { "SE Asia Standard Time", "Asia/Ho_Chi_Minh" })
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(id);
                }
                catch (TimeZoneNotFoundException)
                {
                }
                catch (InvalidTimeZoneException)
                {
                }
            }
            return TimeZoneInfo.Utc;
        }

        // Lower case without Vietnamese accents, and with every dash variant turned into "-", so one set of patterns reads the data.
        internal static string Fold(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            string normalized = text.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);
            foreach (char c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
                builder.Append(c);
            }
            return builder.ToString()
                .Replace('đ', 'd').Replace('Đ', 'D')
                .Replace('–', '-').Replace('—', '-').Replace('−', '-')
                .ToLowerInvariant()
                .Trim();
        }
    }
}
