using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Hue_travel.Models;
using Hue_travel.Services.Hours;

namespace Hue_travel.Panels.SearchPoi
{
    /// <summary>Pure filtering logic, deliberately separated from the UserControl.</summary>
    public static class PoiSearchFilter
    {
        /// <summary>Lower case without diacritics, with đ written as d: the form in which search text and names are compared.</summary>
        public static string Fold(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            string decomposed = text.Trim().ToLowerInvariant().Replace('đ', 'd').Normalize(System.Text.NormalizationForm.FormD);
            var result = new System.Text.StringBuilder(decomposed.Length);
            foreach (char c in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) result.Append(c);
            }
            return result.ToString();
        }

        /// <summary>
        /// Keeps the POIs whose category is in <paramref name="categories"/> (null or empty = every category)
        /// and whose name contains <paramref name="keyword"/>.
        /// </summary>
        public static IReadOnlyList<Poi> Apply(IEnumerable<Poi> source, string keyword, IReadOnlyCollection<PoiCategory> categories)
        {
            return Apply(source, keyword, categories, false);
        }

        /// <summary>
        /// As above; <paramref name="openOnly"/> also drops the places the schedule says are certainly closed. A place whose
        /// hours are unknown is kept: the app must not hide it merely because the source data does not say.
        /// </summary>
        public static IReadOnlyList<Poi> Apply(IEnumerable<Poi> source, string keyword, IReadOnlyCollection<PoiCategory> categories, bool openOnly)
        {
            var query = source ?? Enumerable.Empty<Poi>();

            if (categories != null && categories.Count > 0)
            {
                query = query.Where(p => categories.Contains(p.Category));
            }

            if (openOnly)
            {
                DateTime now = OpeningSchedule.NowInVietnam;
                query = query.Where(p => p.Schedule.StatusAt(now).State != OpenState.Closed);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                // Both sides are folded (lower case, no diacritics, đ = d), so "dai noi" finds "Đại Nội Huế" and "hue" finds "Huế".
                string wanted = Fold(keyword);
                query = query.Where(p => Fold(p.Name).Contains(wanted));
            }

            return query.ToList();
        }
    }
}
