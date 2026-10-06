using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Hue_travel.Models;

namespace Hue_travel.Panels.SearchPoi
{
    /// <summary>Pure filtering logic, deliberately separated from the UserControl.</summary>
    public static class PoiSearchFilter
    {
        public static IReadOnlyList<Poi> Apply(IEnumerable<Poi> source, string keyword, PoiCategory? category)
        {
            var query = source ?? Enumerable.Empty<Poi>();

            if (category.HasValue)
            {
                query = query.Where(p => p.Category == category.Value);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                // CompareOptions.IgnoreNonSpace makes this diacritic-insensitive (e.g. "hue" matches "Huế"),
                // which plain OrdinalIgnoreCase does not — important for Vietnamese input.
                query = query.Where(p => CultureInfo.CurrentCulture.CompareInfo.IndexOf(
                    p.Name, keyword, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0);
            }

            return query.ToList();
        }
    }
}
