using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Hue_travel.Models;
using Hue_travel.Theme;

namespace Hue_travel.Account
{
    /// <summary>Small factories so the account screens look alike (design pixels).</summary>
    internal static class AccountUi
    {
        public static IconButton Button(string text, bool primary, int width, string icon = "")
        {
            return new IconButton
            {
                AutoWidth = false, Text = text, IconName = icon, IconSize = 14, Font = ThemeFonts.Medium(11), Radius = 4, Gap = 4, HorizontalPadding = 8,
                ForeColor = primary ? Color.White : ThemeColors.Slate700,
                IconColor = primary ? Color.White : ThemeColors.Primary,
                FillColor = primary ? ThemeColors.Primary : Color.White,
                HoverFillColor = primary ? ThemeColors.Sky700 : ThemeColors.Slate100,
                BorderColor = primary ? ThemeColors.Primary : ThemeColors.Slate200,
                Size = new Size(width, 28),
            };
        }

        public static ListView List(params KeyValuePair<string, int>[] columns)
        {
            var list = new ListView
            {
                Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, HideSelection = false, MultiSelect = false, GridLines = false,
                BorderStyle = BorderStyle.FixedSingle, Font = ThemeFonts.Regular(11), HeaderStyle = ColumnHeaderStyle.Nonclickable,
            };
            foreach (KeyValuePair<string, int> c in columns) list.Columns.Add(c.Key, c.Value);
            return list;
        }

        public static KeyValuePair<string, int> Col(string title, int width) => new KeyValuePair<string, int>(title, width);

        public static Label Caption(string text, bool bold = false)
        {
            return new Label { Text = text, AutoSize = true, Font = bold ? ThemeFonts.SemiBold(12) : ThemeFonts.Regular(11), ForeColor = bold ? ThemeColors.Slate800 : ThemeColors.Slate600 };
        }

        public static string GroupName(PoiCategory category)
        {
            switch (category)
            {
                case PoiCategory.Heritage: return "Di tích";
                case PoiCategory.Scenery: return "Cảnh quan";
                case PoiCategory.Food: return "Ẩm thực";
                case PoiCategory.Stay: return "Lưu trú";
                case PoiCategory.Service: return "Dịch vụ";
                default: return "Giao thông";
            }
        }

        public static string ClaimStatus(string status)
        {
            switch (status)
            {
                case "approved": return "Đã duyệt";
                case "rejected": return "Bị từ chối";
                default: return "Chờ duyệt";
            }
        }

        /// <summary>First 10 characters of an ISO timestamp, e.g. 2026-10-13.</summary>
        public static string Day(string iso) => string.IsNullOrEmpty(iso) ? "" : (iso.Length > 10 ? iso.Substring(0, 10) : iso);
    }
}
