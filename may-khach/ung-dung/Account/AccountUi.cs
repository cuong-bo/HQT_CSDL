using Hue_travel.Models;

namespace Hue_travel.Account
{
    /// <summary>Names the account screens share. (The look of the screens is in <see cref="AccountKit"/>.)</summary>
    internal static class AccountUi
    {
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
    }
}
