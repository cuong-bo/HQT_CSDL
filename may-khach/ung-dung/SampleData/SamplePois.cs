using System.Collections.Generic;
using Hue_travel.Models;

namespace Hue_travel.SampleData
{
    /// <summary>Hardcoded sample data matching GiaoDien/code.html's reference POIs. No persistence — Phase 1 UI-only.</summary>
    public static class SamplePois
    {
        public static readonly IReadOnlyList<Poi> All = new List<Poi>
        {
            new Poi
            {
                Id = "ga-hue",
                Name = "Ga Huế (Điểm xuất phát)",
                Category = PoiCategory.Transport,
                Address = "Đường Bùi Thị Xuân, TP. Huế",
                OpeningHours = "24/7",
                TicketPrice = "Miễn phí",
                Rating = 0,
                DistanceKm = 0,
                Description = "Điểm xuất phát mặc định cho lộ trình tham quan."
            },
            new Poi
            {
                Id = "dai-noi",
                Name = "Đại Nội - Hoàng Thành Huế",
                Category = PoiCategory.Heritage,
                Address = "Đường 23 Tháng 8, Phường Thuận Hòa, TP. Huế",
                OpeningHours = "07:00 - 17:30 (Mở cửa tất cả các ngày trong tuần)",
                TicketPrice = "200.000 VNĐ / người lớn (Miễn phí trẻ dưới 1m)",
                Rating = 4.8,
                DistanceKm = 3.2,
                Description = "Di tích lịch sử Hoàng Cung triều Nguyễn."
            },
            new Poi
            {
                Id = "thien-mu",
                Name = "Chùa Thiên Mụ",
                Category = PoiCategory.Heritage,
                Address = "Đường Nguyễn Phúc Nguyên, TP. Huế",
                OpeningHours = "06:00 - 18:00",
                TicketPrice = "Miễn phí",
                Rating = 4.7,
                DistanceKm = 4.6,
                Description = "Ngắm cảnh ven bờ sông Hương thơ mộng."
            },
            new Poi
            {
                Id = "khai-dinh",
                Name = "Lăng Khải Định",
                Category = PoiCategory.Heritage,
                Address = "Xã Thủy Bằng, TP. Huế",
                OpeningHours = "07:00 - 17:30",
                TicketPrice = "150.000 VNĐ / người lớn",
                Rating = 4.6,
                DistanceKm = 10.1,
                Description = "Kiến trúc lăng tẩm độc đáo Tây - Đông."
            },
            new Poi
            {
                Id = "bun-bo-mu-roi",
                Name = "Quán Bún Bò Huế Mụ Rơi",
                Category = PoiCategory.Food,
                Address = "40 Nguyễn Chí Diểu, Thuận Thành, TP. Huế",
                OpeningHours = "Mở cửa",
                TicketPrice = "35k - 50k",
                Rating = 4.8,
                DistanceKm = 1.2,
                Description = "Thưởng thức ẩm thực trưa xứ Cố Đô."
            },
            new Poi
            {
                Id = "bun-bo-ba-tuyet",
                Name = "Bún Bò Huế Bà Tuyết",
                Category = PoiCategory.Food,
                Address = "47 Nguyễn Công Trứ, Phú Hội, TP. Huế",
                OpeningHours = "Mở cửa",
                TicketPrice = "35k - 50k",
                Rating = 4.7,
                DistanceKm = 2.1,
                Description = "Quán bún bò Huế lâu năm được yêu thích."
            },
            new Poi
            {
                Id = "bun-bo-cam",
                Name = "Bún Bò Cẩm - Vị Cố Đô",
                Category = PoiCategory.Food,
                Address = "38 Trần Cao Vân, Phú Hội, TP. Huế",
                OpeningHours = "Mở cửa",
                TicketPrice = "35k - 50k",
                Rating = 4.6,
                DistanceKm = 2.6,
                Description = "Vị bún bò đậm đà chuẩn Cố Đô."
            }
        };
    }
}
