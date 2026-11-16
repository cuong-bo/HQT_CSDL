# Máy khách Hue Travel (Windows)

Ứng dụng WinForms (.NET Framework 4.7.2, x86) dùng MapWinGIS. Vẽ bản đồ nền từ shapefile mà nó **tự tải từ máy chủ** (lần đầu mở app, và khi máy chủ có bản mới); tìm đường và tài khoản
gọi máy chủ qua HTTP. Thư mục này **không cần** `may-chu/`.

| Đọc | Khi nào |
|---|---|
| [HUONG-DAN-CAI-DAT.md](HUONG-DAN-CAI-DAT.md) | Phần A: người dùng cài ứng dụng. Phần B: người làm bộ cài |
| [bo-cai/README.md](bo-cai/README.md) | Bộ cài Inno Setup |

## Bố cục

```
may-khach/
├── ung-dung/        mã nguồn ứng dụng (Hue_travel.csproj); dữ liệu bản đồ không nằm trong kho này mà tải từ máy chủ
├── bo-cai/          HueTravel.iss (Inno Setup)
├── kiem-thu/        test_services.ps1 (chạy bằng PowerShell 32-bit sau khi dựng bản Debug)
├── Hue_travel.sln   solution Visual Studio
└── dong-goi.ps1     dựng bộ cài HueTravel-Setup-<phiên bản>.exe
```

## Bên trong `ung-dung/`

| Thư mục | Nội dung |
|---|---|
| `Shell/` | cửa sổ chính (`MainForm`), thanh tiêu đề, thanh điều hướng `NavRail` (Khám phá, Lộ trình, Trợ lý, Khoảnh khắc, Hồ sơ), ô tìm kiếm, thanh lọc, thanh trạng thái |
| `Panels/Explore`, `Layers`, `SearchPoi`, `PoiIdentify`, `RouteOptimizer` | khám phá, lớp bản đồ, tìm kiếm, thẻ chi tiết địa điểm (có nút "Khoảnh khắc ở đây"), lộ trình |
| `Panels/Assistant` | **Trợ lý**: `ChatPanel` (khung chat, gợi ý nhanh, bấm địa điểm/lộ trình để mở trên bản đồ; cẩm nang tĩnh khi ngoại tuyến hoặc máy chủ chưa bật) |
| `Panels/Feed` | **Khoảnh khắc**: `FeedPanel`, `FeedView` (vẽ bài, ảnh, thích), `ComposerDialog` (đăng bài, tối đa 4 ảnh, lộ trình, địa điểm, điều khoản), `CommentsDialog` |
| `Panels/Shared`, `Panels/TravelGuide` | danh sách địa điểm có ảnh thu nhỏ; các hàng thông tin dùng chung |
| `Account/`, `Admin/`, `Auth/`, `Welcome/` | hồ sơ, yêu thích, lịch trình, đánh giá; bảng quản trị (người dùng, địa điểm, duyệt, kiểm duyệt gồm bài Khoảnh khắc và báo cáo, nhật ký); đăng nhập; màn hình chào |
| `Services/Online` | `ApiClient` (thời gian chờ riêng từng yêu cầu: 15 giây; chat 90 giây; tải ảnh 60 giây), `ApiChatService`, `ApiFeedService`, `ApiRouteService`, `AuthService`, `BaseMapUpdater`, `PhotoSync`, `ResponseVerifier` |
| `Services/Data`, `Routing`, `Weather`, `Area` | đọc shapefile, danh mục địa điểm, bộ lập lộ trình, thời tiết, khu vực |
| `MapEngine/` | MapWinGIS: bản đồ nền, điểm đánh dấu, tuyến, xe mô phỏng |
| `Theme/` | màu, chữ (Plus Jakarta Sans nhúng; Georgia cho tiêu đề), biểu tượng, thẻ nổi, `Toast`, các điều khiển tự vẽ |

Ứng dụng **chạy được ngoại tuyến** (xem bản đồ và địa điểm). Các tính năng cần máy chủ (tìm đường, tài khoản, Trợ lý, Khoảnh khắc) báo rõ khi không có kết nối.

## Dựng và thử

```powershell
msbuild may-khach\ung-dung\Hue_travel.csproj /restore /p:Configuration=Debug
& "$env:WINDIR\SysWOW64\WindowsPowerShell\v1.0\powershell.exe" -File may-khach\kiem-thu\test_services.ps1
powershell -ExecutionPolicy Bypass -File may-khach\dong-goi.ps1 -Version 1.0.0 -Server http://IP_VPS
```

Bộ kiểm thử `test_services.ps1` hiện có **247** kiểm thử (đọc dữ liệu, lập lộ trình, thời tiết, phân tích câu trả lời của Trợ lý, bài Khoảnh khắc, xử lý ảnh trước khi tải lên).
Chạy thử ứng dụng với máy chủ cục bộ: `set HUE_API_URL=http://127.0.0.1:5080` rồi mở `ung-dung\bin\Debug\Hue_travel.exe`; chọn "Khách" để xem Trợ lý, đăng nhập để đăng bài Khoảnh khắc.
