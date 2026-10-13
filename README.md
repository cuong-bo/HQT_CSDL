# Cố Đô GIS - bản đồ du lịch Huế

Ứng dụng Windows (WinForms + MapWinGIS) cho khách tham quan Huế: bản đồ vệ tinh/đường phố, tìm địa điểm theo nhóm,
sắp xếp lộ trình ngắn nhất theo đường bộ thật, thời tiết, đo khoảng cách/diện tích. Dữ liệu và xử lý nằm trên một máy chủ
(VPS Ubuntu); app vẫn chạy được ngoại tuyến bằng dữ liệu đi kèm.

Ba nhóm người dùng: **người dùng** (yêu thích, lộ trình đã lưu, đánh giá, báo sai), **chủ quản địa điểm** (gửi yêu cầu
chỉnh sửa, thông báo, trả lời đánh giá, xem thống kê) và **quản trị viên** (duyệt, kiểm duyệt, quản lý tài khoản và dữ liệu, nhật ký).

```
Máy người dùng (Windows)                  VPS Ubuntu (Docker Compose)
┌──────────────────────────┐  HTTP/JSON  ┌────────────────────────────────────┐
│ Hue_travel (WinForms,     │ ──────────► │ Caddy :80 → API .NET 8             │
│ MapWinGIS)                │ ◄────────── │       └─► PostgreSQL 17 + PostGIS  │
│ ngoại tuyến: shapefile    │             │           + pgRouting              │
└──────────────────────────┘             └────────────────────────────────────┘
```

Hệ tọa độ thống nhất: **VN-2000 / UTM 48N (EPSG:3405)**, đơn vị mét.

## Cấu trúc

| Thư mục | Nội dung |
|---|---|
| `Hue_travel/` | app Windows (.NET Framework 4.7.2, x86) |
| `src/Hue_travel.Api/` | API .NET 8 (đăng nhập JWT, địa điểm, tìm đường, ba vai trò) |
| `tests/Hue_travel.Api.Tests/` | test API (đơn vị + tích hợp với CSDL thật) |
| `tests/client/` | test các dịch vụ của app (PowerShell, chạy bằng PowerShell 32-bit) |
| `db/` | migration, nạp shapefile, đồ thị pgRouting, kiểm tra — xem `db/README.md` |
| `deploy/` | Docker Compose, Caddy (HTTP), script cài VPS / cập nhật / sao lưu — xem `deploy/README.md` |
| `installer/` | bộ cài Inno Setup — xem `installer/README.md` |
| `scripts/` | đóng gói: `dong-goi-may-chu.ps1` (gói đưa lên VPS), `dong-goi-may-khach.ps1` (bộ cài Windows) |
| `docs/` | hướng dẫn cài đặt máy chủ và máy khách, phân chia hai phần, kế hoạch, từ điển dữ liệu |
| `Hue_travel/Data/shp/` | dữ liệu shapefile (điểm du lịch, ẩm thực, lưu trú, mạng giao thông, nút giao thông, ranh giới; khoảng 75 MB, lưu thẳng trong Git) |

## Cài đặt (máy chủ và máy khách tách riêng)

| Tài liệu | Dành cho |
|---|---|
| [`docs/phan-chia-may-chu-may-khach.md`](docs/phan-chia-may-chu-may-khach.md) | Hiểu phần nào chạy ở máy chủ, phần nào ở máy khách, tệp nào đi đâu |
| [`docs/huong-dan-cai-dat-may-chu.md`](docs/huong-dan-cai-dat-may-chu.md) | Cài VPS Ubuntu từng bước (đóng gói `scripts\dong-goi-may-chu.ps1`) |
| [`docs/huong-dan-cai-dat-may-khach.md`](docs/huong-dan-cai-dat-may-khach.md) | Cài ứng dụng Windows; làm bộ cài (`scripts\dong-goi-may-khach.ps1`) |

## Chạy thử nhanh

Cần: Visual Studio 2019 (hoặc MSBuild), MapWinGIS 5.x đã cài và đăng ký, .NET 8 SDK, PostgreSQL 17 với PostGIS và pgRouting.

```bash
# 1. CSDL (xem db/README.md)
createdb huegis && PGDATABASE=huegis ./db/run.sh

# 2. API + test
export ConnectionStrings__Default="Host=localhost;Database=huegis;Username=postgres;Password=..."
export Jwt__Key="$(openssl rand -base64 32)"
dotnet run --project src/Hue_travel.Api
HUE_TEST_DB="Host=localhost;Database=huegis;Username=postgres;Password=..." dotnet test tests/Hue_travel.Api.Tests

# 3. App (Windows): mở Hue_travel.sln hoặc
msbuild Hue_travel/Hue_travel.csproj /restore /p:Configuration=Debug
set HUE_API_URL=http://localhost:5000   # bỏ trống để chạy ngoại tuyến
Hue_travel\bin\Debug\Hue_travel.exe
```

Địa chỉ máy chủ của app lấy theo thứ tự: biến môi trường `HUE_API_URL`, file `%ProgramData%\CoDoGIS\server.txt` (bộ cài ghi), rồi
`ApiBaseUrl` trong `Hue_travel.exe.config`. Không có địa chỉ nào thì app chạy **ngoại tuyến** bằng dữ liệu đi kèm (đăng nhập tắt).

Tài khoản quản trị đầu tiên: đăng ký trong app rồi cấp quyền một lần bằng SQL (xem `deploy/README.md`, mục 4).

## Nguồn và ghi công

- Bản đồ nền: Google (tạm thời, dùng cho phát triển; cần nhà cung cấp hợp lệ khi phát hành rộng rãi, ví dụ MapTiler hoặc Esri). Máy chủ có thể thông báo địa chỉ tile khác cho app, không cần phát hành lại: xem `deploy/README.md`, mục "Bản đồ nền".
- Dữ liệu đường và sông: © OpenStreetMap contributors (ODbL).
- Thời tiết: [Open-Meteo](https://open-meteo.com) (CC BY 4.0, phi thương mại).
- Bản đồ và hình học: [MapWinGIS](https://mapwingis.github.io/) (MPL).
