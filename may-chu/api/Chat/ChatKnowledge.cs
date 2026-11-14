namespace Hue_travel.Api.Chat;

/// <summary>A suggested tour: places by id, in the order they are best visited (the first is where the tour starts).</summary>
public sealed record ChatTour(string Title, string Duration, string[] PlaceIds);

/// <summary>
/// What the assistant knows besides the database: the suggested tours and a few lines about the app. The tours repeat the ones in the
/// desktop app (Panels/TravelGuide/GuideContent.cs); the places are looked up by id when the tool runs, so a tour that points at a place
/// that no longer exists simply loses that stop.
/// </summary>
public static class ChatKnowledge
{
    public const string Hotline = "0234.3828288";

    public static readonly IReadOnlyList<ChatTour> Tours =
    [
        new("Di sản Cố Đô trong một ngày", "Khoảng 1 ngày", ["GT002", "DL021", "DL022", "DL024", "DL029"]),
        new("Lăng tẩm triều Nguyễn", "Khoảng 5 giờ", ["DL034", "DL029", "DL036", "DL033"]),
        new("Chùa và thiền viện xứ Huế", "Khoảng nửa ngày", ["DL024", "DL016", "DL017", "DL037"]),
        new("Đêm bên bờ sông Hương", "Khoảng 3 giờ", ["GT007", "DL039", "DL012", "DL040", "AU017"]),
        new("Ẩm thực Cố Đô", "Khoảng nửa ngày", ["DL038", "AU035", "AU003", "AU008", "AU009"]),
    ];

    /// <summary>How to use the app, in the words the assistant may repeat. Short on purpose: it is paid for in every request.</summary>
    public const string AppHelp =
        "Ứng dụng có thanh tìm kiếm trên bản đồ và bốn nhóm công cụ ở thanh bên trái: Khám phá (danh sách địa điểm quanh khu vực đang xem), " +
        "Lộ trình (chọn điểm xuất phát và các điểm dừng rồi bấm Tìm đường; xe chạy thử trên bản đồ), Trợ lý (chính là bạn), " +
        "Khoảnh khắc (ảnh và chia sẻ của cộng đồng) và Hồ sơ (yêu thích, lộ trình đã lưu, đánh giá). Bấm vào một địa điểm trên bản đồ để xem thẻ thông tin; " +
        "trong thẻ có nút Thêm vào lộ trình. Khách xem được bản đồ và lập lộ trình; yêu thích, đánh giá, báo sai và đăng khoảnh khắc cần đăng nhập.";
}
