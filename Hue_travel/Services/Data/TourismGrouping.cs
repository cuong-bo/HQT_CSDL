using System;
using System.Collections.Generic;
using System.Text;
using Hue_travel.Models;

namespace Hue_travel.Services.Data
{
    /// <summary>Maps the free-text tourist-site type (LoaiDL, e.g. "Sinh thái - hồ nước") to a rail group.</summary>
    public static class TourismGrouping
    {
        private const string TypeSeparator = " - ";

        // The first word of the type decides: these lead types are scenery; everything else is heritage / culture.
        private static readonly HashSet<string> SceneryLeads = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Biển", "Cảnh quan", "Sinh thái", "Nghỉ dưỡng", "Đầm phá", "Phố đi bộ"
        };

        public static PoiCategory GroupOf(string siteType)
        {
            if (string.IsNullOrWhiteSpace(siteType)) return PoiCategory.Heritage;

            string normalized = siteType.Normalize(NormalizationForm.FormC).Trim();
            int cut = normalized.IndexOf(TypeSeparator, StringComparison.Ordinal);
            string lead = cut < 0 ? normalized : normalized.Substring(0, cut);
            return SceneryLeads.Contains(lead) ? PoiCategory.Scenery : PoiCategory.Heritage;
        }
    }
}
