using System;
using System.Collections.Generic;
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
                query = query.Where(p => p.Name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            return query.ToList();
        }
    }
}
