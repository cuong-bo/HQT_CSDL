namespace Hue_travel.Api.Chat;

/// <summary>A suggested tour, copied from the desktop guide. The two copies can drift; the clean fix is a shared data file/table
/// (noted in both files). The first stop is where the tour starts.</summary>
public sealed record ChatTour(string Title, string Badge, string Duration, IReadOnlyList<ChatTourStop> Stops);
public sealed record ChatTourStop(string Id, string Name);

public static class ChatKnowledge
{
    public const string Hotline = "0234.3828288";

    public static readonly IReadOnlyList<ChatTour> Tours =
    [
        new("Di sản Cố Đô trong một ngày", "Khuyên dùng", "Khoảng 1 ngày",
        [
            new("GT002", "Ga Huế"), new("DL021", "Đại Nội"), new("DL022", "Bảo tàng Cổ vật Cung đình Huế"),
            new("DL024", "Chùa Thiên Mụ"), new("DL029", "Lăng Tự Đức"),
        ]),
        new("Lăng tẩm triều Nguyễn", "Nửa ngày", "Khoảng 5 giờ",
        [
            new("DL034", "Lăng Minh Mạng"), new("DL029", "Lăng Tự Đức"), new("DL036", "Chùa Từ Hiếu"),
            new("DL033", "Lăng Khải Định"),
        ]),
        new("Chùa và thiền viện xứ Huế", "Tâm linh", "Khoảng nửa ngày",
        [
            new("DL024", "Chùa Thiên Mụ"), new("DL016", "Chùa Báo Quốc"), new("DL017", "Chùa Diệu Đế"),
            new("DL037", "Chùa Từ Đàm"),
        ]),
        new("Đêm bên bờ sông Hương", "Buổi tối", "Khoảng 3 giờ",
        [
            new("GT007", "Bến thuyền Tòa Khâm"), new("DL039", "Cầu Trường Tiền"), new("DL012", "Cầu gỗ lim"),
            new("DL040", "Phố đi bộ Nguyễn Đình Chiểu"), new("AU017", "Nhà hàng nổi sông Hương"),
        ]),
        new("Ẩm thực Cố Đô", "Ẩm thực", "Khoảng nửa ngày",
        [
            new("DL038", "Chợ Đông Ba"), new("AU035", "Bánh bèo nậm lọc"), new("AU003", "Bún bò Mụ Rơi"),
            new("AU008", "Cơm hến"), new("AU009", "Chè Mợ Tôn Đích"),
        ]),
    ];
}
