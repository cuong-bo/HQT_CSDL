namespace Hue_travel.Models
{
    /// <summary>The kinds of object shown on the left rail; the order is the order of the rail.</summary>
    public enum PoiCategory
    {
        Heritage,   // Di tích & văn hóa: Đại Nội, lăng tẩm, chùa, bảo tàng
        Scenery,    // Danh thắng & cảnh quan: sông Hương, núi Ngự Bình, đầm phá, Bạch Mã, biển
        Food,       // Ẩm thực: bún bò, bánh bèo nậm lọc, cơm hến, chè Huế
        Stay,       // Lưu trú: khách sạn, resort, homestay
        Service,    // Dịch vụ du lịch: ca Huế, thuyền rồng, lữ hành, thuê xe, làng nghề
        Transport   // Giao thông: ga, bến xe, sân bay, bến thuyền
    }
}
