# Hue Travel - bản đồ du lịch Huế

Ứng dụng Windows (WinForms + MapWinGIS) cho khách tham quan Huế: bản đồ nền vẽ từ dữ liệu của dự án, tìm địa điểm theo nhóm,
sắp xếp lộ trình ngắn nhất theo đường bộ thật, xe chạy mô phỏng trên tuyến, thời tiết, đo khoảng cách/diện tích.
Dữ liệu và xử lý nặng (tìm đường, tài khoản) nằm trên một **máy chủ** (VPS Ubuntu); **máy khách** là ứng dụng Windows nhẹ.

```
Máy khách (Windows)                       Máy chủ (VPS Ubuntu, Docker Compose)
┌──────────────────────────┐  HTTP/JSON  ┌────────────────────────────────────┐
│ Hue_travel (WinForms,     │ ──────────► │ Caddy :80 → API .NET 8             │
│ MapWinGIS)                │ ◄────────── │       └─► PostgreSQL 17 + PostGIS  │
│ vẽ bản đồ nền cục bộ      │             │           + pgRouting              │
└──────────────────────────┘             └────────────────────────────────────┘
```

Hệ tọa độ thống nhất: **VN-2000 / UTM 48N (EPSG:3405)**, đơn vị mét.

## Kho mã chia làm hai phần độc lập

| Thư mục | Là gì | Ai cần | Cài đặt |
|---|---|---|---|
| [`may-chu/`](may-chu) | **Máy chủ**: API, cơ sở dữ liệu, dữ liệu shapefile, Docker Compose, script vận hành | Người quản trị VPS (chỉ cần `git clone` rồi `cd may-chu`) | [may-chu/HUONG-DAN-CAI-DAT.md](may-chu/HUONG-DAN-CAI-DAT.md) |
| [`may-khach/`](may-khach) | **Máy khách**: ứng dụng Windows, bộ cài Inno Setup, test | Người dùng cuối (nhận bộ cài `.exe`) và người dựng bộ cài | [may-khach/HUONG-DAN-CAI-DAT.md](may-khach/HUONG-DAN-CAI-DAT.md) |
| [`tai-lieu/`](tai-lieu) | Tài liệu chung: phân chia hai phần, kế hoạch triển khai, từ điển dữ liệu | Cả hai | [tai-lieu/phan-chia-may-chu-may-khach.md](tai-lieu/phan-chia-may-chu-may-khach.md) |

Hai phần chỉ giao tiếp qua HTTP/JSON; mỗi phần dựng, kiểm thử và cài riêng, không phần nào cần mã của phần kia.

### Bên trong `may-chu/`

| Mục | Nội dung |
|---|---|
| `api/` | API .NET 8 (đăng nhập JWT, địa điểm, tìm đường, ba vai trò) |
| `co-so-du-lieu/` | migration, nạp shapefile, đồ thị pgRouting, kiểm tra — xem [README](may-chu/co-so-du-lieu/README.md) |
| `du-lieu/shp/` | shapefile máy chủ cần (địa điểm, mạng đường, **nút giao thông**, ranh giới) |
| `docker-compose.yml`, `Caddyfile`, `.env.example`, `docker-csdl/` | dựng 3 dịch vụ: Caddy, API, PostgreSQL |
| `scripts/` | `setup-vps.sh` (cài VPS), `deploy.sh` (up/import/update/rollback), `backup.sh`, `restore.sh`, `gen-signing-key.sh` |
| `kiem-thu/` | test API (đơn vị + tích hợp với CSDL thật) |
| `dong-goi.ps1` | (tùy chọn) đóng gói `.tar.gz` khi VPS không vào được GitHub |
| `HUONG-DAN-CAI-DAT.md`, `VAN-HANH.md` | cài đặt từng bước; vận hành hằng ngày |

### Bên trong `may-khach/`

| Mục | Nội dung |
|---|---|
| `ung-dung/` | ứng dụng WinForms (.NET Framework 4.7.2, x86) kèm `Data/shp` (bản đồ nền và địa điểm dự phòng, **không** có nút giao thông) |
| `Hue_travel.sln` | solution Visual Studio |
| `bo-cai/` | bộ cài Inno Setup (`HueTravel.iss`) |
| `kiem-thu/` | test dịch vụ của ứng dụng (PowerShell 32-bit) |
| `dong-goi.ps1` | dựng bộ cài `HueTravel-Setup-<phiên bản>.exe` |
| `HUONG-DAN-CAI-DAT.md` | cài đặt cho người dùng và cho người làm bộ cài |

## Cài đặt nhanh

**Máy chủ (VPS Ubuntu 22.04/24.04, quyền root):**
```bash
git clone https://github.com/cuong-bo/HQT_CSDL.git /opt/hue-travel
bash /opt/hue-travel/may-chu/scripts/setup-vps.sh       # cài Docker, tường lửa, tạo người dùng deploy
# rồi đăng nhập bằng deploy:  cd /opt/hue-travel/may-chu && cp .env.example .env && nano .env
#   scripts/deploy.sh up && scripts/deploy.sh import && scripts/deploy.sh status
```
Chi tiết, kèm kiểm tra từng bước và xử lý sự cố: [may-chu/HUONG-DAN-CAI-DAT.md](may-chu/HUONG-DAN-CAI-DAT.md).

**Máy khách (Windows):** chạy `HueTravel-Setup-<phiên bản>.exe` và nhập địa chỉ máy chủ; hoặc dựng từ mã nguồn:
`powershell -ExecutionPolicy Bypass -File may-khach\dong-goi.ps1 -Version 1.0.0 -Server http://IP_VPS`
(xem [may-khach/HUONG-DAN-CAI-DAT.md](may-khach/HUONG-DAN-CAI-DAT.md)).

## Chạy thử trên máy phát triển

Cần: Visual Studio 2019 (hoặc MSBuild), MapWinGIS 5.x đã cài và đăng ký, .NET 8 SDK, PostgreSQL 17 với PostGIS và pgRouting.

```bash
# 1. CSDL (xem may-chu/co-so-du-lieu/README.md)
createdb huegis && PGDATABASE=huegis ./may-chu/co-so-du-lieu/run.sh

# 2. API + test
export ConnectionStrings__Default="Host=localhost;Database=huegis;Username=postgres;Password=..."
export Jwt__Key="$(openssl rand -base64 32)"
dotnet run --project may-chu/api
HUE_TEST_DB="Host=localhost;Database=huegis;Username=postgres;Password=..." dotnet test may-chu/kiem-thu

# 3. Ứng dụng (Windows): mở may-khach/Hue_travel.sln hoặc
msbuild may-khach/ung-dung/Hue_travel.csproj /restore /p:Configuration=Debug
set HUE_API_URL=http://localhost:5000   # bỏ trống để chạy ngoại tuyến
may-khach\ung-dung\bin\Debug\Hue_travel.exe
```

Địa chỉ máy chủ của ứng dụng lấy theo thứ tự: biến môi trường `HUE_API_URL`, tệp `%ProgramData%\HueTravel\server.txt` (bộ cài ghi), rồi
`ApiBaseUrl` trong `Hue_travel.exe.config`. Không có địa chỉ nào thì ứng dụng chạy **ngoại tuyến** (xem bản đồ và địa điểm; không
đăng nhập, không tìm đường).

Tài khoản quản trị đầu tiên: đăng ký trong ứng dụng rồi cấp quyền một lần bằng SQL (xem [may-chu/HUONG-DAN-CAI-DAT.md](may-chu/HUONG-DAN-CAI-DAT.md), mục 8).

## Nguồn và ghi công

- Dữ liệu đường và sông: © OpenStreetMap contributors (ODbL).
- Thời tiết: [Open-Meteo](https://open-meteo.com) (CC BY 4.0, phi thương mại).
- Bản đồ và hình học: [MapWinGIS](https://mapwingis.github.io/) (MPL).
