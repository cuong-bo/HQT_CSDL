using System.Collections.Generic;
using Hue_travel.Models;

namespace Hue_travel.SampleData
{
    public static class SampleTours
    {
        public static readonly IReadOnlyList<TourSuggestion> All = new List<TourSuggestion>
        {
            new TourSuggestion
            {
                Title = "Tour 1 ngày: Di Sản Cố Đô",
                Badge = "Khuyên dùng",
                Description = "Ga Huế → Đại Nội → Chùa Thiên Mụ → Lăng Tự Đức"
            },
            new TourSuggestion
            {
                Title = "Tour Đêm Sông Hương & Ca Huế",
                Badge = "19:00 - 21:00",
                Description = "Thuyền rồng Bến Tòa Khâm, thả hoa đăng ngắm cầu Trường Tiền"
            },
            new TourSuggestion
            {
                Title = "Tour Ẩm Thực Cố Đô Về Chiều",
                Badge = "Ẩm thực",
                Description = "Bún bò Mụ Rơi, bánh bèo nậm lọc Bà Đỏ, chè hẻm Hùng Vương"
            }
        };
    }
}
