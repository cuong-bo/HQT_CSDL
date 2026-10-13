using System.Collections.Generic;

namespace Hue_travel.Panels.TravelGuide
{
    /// <summary>A suggested tour: places given by id (the first is where the tour starts), in the order they are best visited.</summary>
    internal sealed class GuideTour
    {
        public string Title { get; set; }
        public string Badge { get; set; }
        public string Duration { get; set; }
        public string Icon { get; set; }
        public string[] PlaceIds { get; set; }
    }

    /// <summary>One line of the tips or the phone numbers.</summary>
    internal sealed class GuideNote
    {
        public string Icon { get; set; }
        public string Title { get; set; }
        public string Text { get; set; }

        /// <summary>The text copied by the small button on the right; empty for no button.</summary>
        public string CopyText { get; set; }
    }

    /// <summary>What the guide says. The tours use the ids of the places in the data shipped with the app and the server.</summary>
    internal static class GuideContent
    {
        public const string Hotline = "0234.3828288";

        public static readonly IReadOnlyList<GuideTour> Tours = new List<GuideTour>
        {
            new GuideTour
            {
                Title = "Di sản Cố Đô trong một ngày", Badge = "Khuyên dùng", Duration = "Khoảng 1 ngày", Icon = "account_balance",
                PlaceIds = new[] { "GT002", "DL021", "DL022", "DL024", "DL029" },   // Ga Huế, Đại Nội, Bảo tàng Cổ vật Cung đình, chùa Thiên Mụ, lăng Tự Đức
            },
            new GuideTour
            {
                Title = "Lăng tẩm triều Nguyễn", Badge = "Nửa ngày", Duration = "Khoảng 5 giờ", Icon = "temple_buddhist",
                PlaceIds = new[] { "DL034", "DL029", "DL036", "DL033" },            // lăng Minh Mạng, lăng Tự Đức, chùa Từ Hiếu, lăng Khải Định
            },
            new GuideTour
            {
                Title = "Chùa và thiền viện xứ Huế", Badge = "Tâm linh", Duration = "Khoảng nửa ngày", Icon = "explore",
                PlaceIds = new[] { "DL024", "DL016", "DL017", "DL037" },            // Thiên Mụ, Báo Quốc, Diệu Đế, Từ Đàm
            },
            new GuideTour
            {
                Title = "Đêm bên bờ sông Hương", Badge = "Buổi tối", Duration = "Khoảng 3 giờ", Icon = "kayaking",
                PlaceIds = new[] { "GT007", "DL039", "DL012", "DL040", "AU017" },   // bến thuyền Tòa Khâm, cầu Trường Tiền, cầu gỗ lim, phố đi bộ, nhà hàng nổi
            },
            new GuideTour
            {
                Title = "Ẩm thực Cố Đô", Badge = "Ẩm thực", Duration = "Khoảng nửa ngày", Icon = "restaurant",
                PlaceIds = new[] { "DL038", "AU035", "AU003", "AU008", "AU009" },   // chợ Đông Ba, bánh bèo nậm lọc, bún bò Mụ Rơi, cơm hến, chè Mợ Tôn Đích
            },
        };

        public static readonly IReadOnlyList<GuideNote> Tips = new List<GuideNote>
        {
            new GuideNote { Icon = "lightbulb", Title = "Mặc kín đáo khi vào nơi thờ tự", Text = "Chùa, lăng tẩm và điện thờ nên mặc áo che vai, quần hoặc váy qua gối; đi nhẹ, nói khẽ." },
            new GuideNote { Icon = "wb_sunny", Title = "Đi sớm hoặc cuối chiều", Text = "Các di tích ngoài trời rất nắng vào buổi trưa. Riêng Đại Nội nên dành ít nhất nửa buổi." },
            new GuideNote { Icon = "campaign", Title = "Mùa mưa", Text = "Huế mưa nhiều khoảng từ tháng 9 đến tháng 12. Mang áo mưa và xem thời tiết trong ứng dụng trước khi đi." },
            new GuideNote { Icon = "check_circle", Title = "Hỏi giá trước", Text = "Thuê thuyền, xích lô hay xe ôm: thoả thuận giá trước khi đi. Vé di tích mua ở quầy chính thức tại cổng." },
            new GuideNote { Icon = "place", Title = "Kiểm tra giờ mở cửa", Text = "Giờ mở cửa và giá vé có thể đổi. Xem trong thẻ thông tin địa điểm hoặc gọi hotline trước khi đi." },
        };

        public static readonly IReadOnlyList<GuideNote> Contacts = new List<GuideNote>
        {
            new GuideNote { Icon = "support_agent", Title = "Hotline hỗ trợ du khách", Text = Hotline + " (khẩn cấp, phản ánh giá cả dịch vụ, tìm đồ thất lạc)", CopyText = Hotline },
            new GuideNote { Icon = "call", Title = "Công an", Text = "113", CopyText = "113" },
            new GuideNote { Icon = "call", Title = "Cứu hoả", Text = "114", CopyText = "114" },
            new GuideNote { Icon = "call", Title = "Cấp cứu", Text = "115", CopyText = "115" },
        };
    }
}
