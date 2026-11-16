# Hue Travel - bản đồ du lịch Huế

Ứng dụng Windows (WinForms + MapWinGIS) cho khách tham quan Huế, kèm một **máy chủ** (ASP.NET Core + PostgreSQL/PostGIS/pgRouting).
Toàn bộ giao diện bằng tiếng Việt.

**Tính năng chính**

- **Bản đồ nền** vẽ từ dữ liệu của dự án (xã, tỉnh, sông, đường), 6 nhóm địa điểm: di tích, cảnh quan, ẩm thực, lưu trú, dịch vụ, giao thông.
- **Khám phá và tìm kiếm** địa điểm theo nhóm, thẻ chi tiết (giờ mở cửa, giá vé, ảnh, đánh giá), danh sách gần khu vực đang xem.
- **Lộ trình**: xếp thứ tự điểm đến ngắn nhất theo đường bộ thật (pgRouting, tối đa 12 điểm), xe chạy mô phỏng trên tuyến theo thời gian từng đoạn đường.
- **Trợ lý** (chat): hỏi địa điểm, giờ mở cửa, thời tiết, gợi ý lộ trình. Máy chủ gọi **DeepSeek V4.1 Flash qua OpenCode API**; khóa chỉ nằm ở máy chủ. Khi ngoại tuyến hoặc chưa bật, hiện cẩm nang tĩnh (số khẩn cấp, lưu ý tham quan).
- **Khoảnh khắc**: bản tin cộng đồng chia sẻ ảnh, địa điểm và lộ trình; thích, bình luận, báo cáo; kiểm duyệt bởi quản trị viên. **Mặc định tắt** (`FEED_ENABLED=false`).
- **Tài khoản và ba vai trò**: người dùng (yêu thích, lịch trình, đánh giá, báo sai), chủ quản địa điểm (nhận quản lý, thông báo, ảnh), quản trị viên (duyệt ảnh, yêu cầu chỉnh sửa, bài Khoảnh khắc, báo cáo, nhật ký).
- Thời tiết (Open-Meteo), đo khoảng cách/diện tích, chế độ ngoại tuyến khi không có máy chủ.

```
Máy khách (Windows)                         Máy chủ (VPS Ubuntu, Docker Compose)
┌───────────────────────────┐  HTTP/JSON   ┌─────────────────────────────────────┐
│ Hue_travel (WinForms,      │ ───────────► │ Caddy :80 → API .NET 8              │
│ MapWinGIS)                 │ ◄─────────── │   ├─► PostgreSQL 17 + PostGIS       │
│ vẽ bản đồ nền cục bộ       │              │   │       + pgRouting               │
└───────────────────────────┘              │   └─► OpenCode API (DeepSeek V4.1   │
                                           │        Flash) cho Trợ lý            │
                                           └─────────────────────────────────────┘
```

Hệ tọa độ thống nhất: **VN-2000 / UTM 48N (EPSG:3405)**, đơn vị mét.

## Xem nhanh bằng sơ đồ

Muốn nắm hệ thống trong vài phút, mở **[tai-lieu/so-do.md](tai-lieu/so-do.md)** (GitHub tự vẽ các sơ đồ):

| Sơ đồ | Cho biết |
|---|---|
| [Kiến trúc hệ thống](tai-lieu/so-do.md#1-kiến-trúc-hệ-thống) | máy khách, máy chủ, cơ sở dữ liệu, OpenCode API nói chuyện với nhau thế nào |
| [Phân rã chức năng](tai-lieu/so-do.md#2-sơ-đồ-phân-rã-chức-năng) | toàn bộ chức năng chia theo nhóm: bản đồ, lộ trình, trợ lý, Khoảnh khắc, tài khoản, chủ quản, quản trị, máy chủ |
| [Ai làm được gì](tai-lieu/so-do.md#3-ai-làm-được-gì) | bảng chức năng theo ba vai trò: người dùng, chủ quản, quản trị viên |
| [Cơ sở dữ liệu](tai-lieu/so-do.md#4-sơ-đồ-cơ-sở-dữ-liệu) | các bảng và quan hệ giữa chúng |
| [Vòng đời yêu cầu duyệt](tai-lieu/so-do.md#5-vòng-đời-các-yêu-cầu-cần-duyệt) | một yêu cầu của chủ quản đi qua những trạng thái nào |
| [Thư viện sử dụng](tai-lieu/thu-vien-su-dung.md) | mọi thư viện, công cụ, dữ liệu bên ngoài và vai trò của từng thứ |
| [pgRouting](tai-lieu/pgrouting.md) | tìm đường hoạt động thế nào, kèm ví dụ chạy thật trên Đại Nội, Thiên Mụ, Tự Đức |

## Kho mã chia làm hai phần độc lập

| Thư mục | Là gì | Ai cần | Cài đặt |
|---|---|---|---|
| [`may-chu/`](may-chu) | **Máy chủ**: API, cơ sở dữ liệu, dữ liệu shapefile, Docker Compose, script vận hành | Người quản trị VPS (chỉ cần `git clone` rồi `cd may-chu`) | [may-chu/HUONG-DAN-CAI-DAT.md](may-chu/HUONG-DAN-CAI-DAT.md) |
| [`may-khach/`](may-khach) | **Máy khách**: ứng dụng Windows, bộ cài Inno Setup, test | Người dùng cuối (nhận bộ cài `.exe`) và người dựng bộ cài | [may-khach/HUONG-DAN-CAI-DAT.md](may-khach/HUONG-DAN-CAI-DAT.md) |
| [`tai-lieu/`](tai-lieu) | Tài liệu chung: sơ đồ, phân chia hai phần, kế hoạch (giao diện, Trợ lý, Khoảnh khắc, VPS), từ điển dữ liệu | Cả hai | [tai-lieu/phan-chia-may-chu-may-khach.md](tai-lieu/phan-chia-may-chu-may-khach.md) |

Hai phần chỉ giao tiếp qua HTTP/JSON; mỗi phần dựng, kiểm thử và cài riêng, không phần nào cần mã của phần kia.

### Bên trong `may-chu/`

| Mục | Nội dung |
|---|---|
| `api/` | API .NET 8: `Auth` (JWT), `Pois`, `Routing`, `Weather`, `Area`, `Photos`, `Owner`, `Admin`, `Social`, `Audit`, `Meta` (cấu hình, bản đồ nền), **`Chat`** (Trợ lý: công cụ, hạn mức, gọi mô hình), **`Feeds`** (Khoảnh khắc: bài, ảnh, bình luận, kiểm duyệt) |
| `co-so-du-lieu/` | migration `V001..V012` (V012: Khoảnh khắc), nạp shapefile, đồ thị pgRouting, kiểm tra — xem [README](may-chu/co-so-du-lieu/README.md) |
| `du-lieu/shp/` | shapefile máy chủ cần (địa điểm, mạng đường, **nút giao thông**, ranh giới) |
| `docker-compose.yml`, `Caddyfile`, `.env.example`, `docker-csdl/` | dựng 3 dịch vụ: Caddy, API, PostgreSQL |
| `scripts/` | `setup-vps.sh` (cài VPS), `deploy.sh` (up/import/update/rollback), `backup.sh`, `restore.sh`, `gen-signing-key.sh` |
| `kiem-thu/` | test API: đơn vị và tích hợp với CSDL thật (153 test) |
| `dong-goi.ps1` | (tùy chọn) đóng gói `.tar.gz` khi VPS không vào được GitHub |
| `HUONG-DAN-CAI-DAT.md`, `VAN-HANH.md` | cài đặt từng bước; vận hành hằng ngày (gồm mục Trợ lý và Khoảnh khắc) |

### Bên trong `may-khach/`

| Mục | Nội dung |
|---|---|
| `ung-dung/` | ứng dụng WinForms (.NET Framework 4.7.2, x86) tự tải dữ liệu bản đồ (`Data/shp`) từ máy chủ ở lần chạy đầu và khi có bản mới |
| `Hue_travel.sln` | solution Visual Studio |
| `bo-cai/` | bộ cài Inno Setup (`HueTravel.iss`) |
| `kiem-thu/` | test dịch vụ của ứng dụng (PowerShell 32-bit, 247 test) |
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

## Cấu hình máy chủ (`may-chu/.env`)

Sao chép từ `.env.example`; `.env` không vào git.

| Biến | Ý nghĩa |
|---|---|
| `POSTGRES_PASSWORD`, `APP_DB_PASSWORD`, `JWT_KEY` | mật khẩu cơ sở dữ liệu, mật khẩu tài khoản giới hạn của API, khóa ký phiên đăng nhập (≥ 32 ký tự) |
| `SIGNING_KEY` | (khuyên dùng khi chạy HTTP) khóa ECDSA ký mọi phản hồi; tạo bằng `scripts/gen-signing-key.sh` |
| `OPENCODE_API_KEY` | khóa OpenCode API cho **Trợ lý**; để trống thì Trợ lý tắt (app hiện cẩm nang tĩnh) |
| `OPENCODE_MODEL` | mô hình, mặc định `deepseek-v4.1-flash` (bản miễn phí: `deepseek-v4.1-flash-free`) |
| `FEED_ENABLED` | `true` để bật **Khoảnh khắc**; mặc định `false` (nên bật khi đã có HTTPS và người xử lý báo cáo) |
| `MIN_CLIENT_VERSION`, `TILE_*` | phiên bản app tối thiểu; địa chỉ ô bản đồ nền (tùy chọn) |

## Chạy thử trên máy phát triển

Cần: Visual Studio 2019+ (hoặc MSBuild), MapWinGIS 5.x đã cài và đăng ký, .NET 8 SDK, PostgreSQL 17 với PostGIS và pgRouting (hoặc Docker).

```bash
# 1. CSDL (xem may-chu/co-so-du-lieu/README.md)
createdb huegis && PGDATABASE=huegis ./may-chu/co-so-du-lieu/run.sh

# 2. API + test
export ConnectionStrings__Default="Host=localhost;Database=huegis;Username=postgres;Password=..."
export Jwt__Key="$(openssl rand -base64 32)"
export Feed__Enabled=true                 # tùy chọn: bật Khoảnh khắc
export Chat__ApiKey="khóa OpenCode"       # tùy chọn: bật Trợ lý
dotnet run --project may-chu/api
HUE_TEST_DB="Host=localhost;Database=huegis;Username=postgres;Password=..." dotnet test may-chu/kiem-thu

# 3. Ứng dụng (Windows): mở may-khach/Hue_travel.sln hoặc
msbuild may-khach/ung-dung/Hue_travel.csproj /restore /p:Configuration=Debug
set HUE_API_URL=http://localhost:5000   # bỏ trống để chạy ngoại tuyến
may-khach\ung-dung\bin\Debug\Hue_travel.exe

# 4. Test máy khách (PowerShell 32-bit)
& "$env:WINDIR\SysWOW64\WindowsPowerShell\v1.0\powershell.exe" -File may-khach\kiem-thu\test_services.ps1
```

Test máy chủ cần runtime **.NET 8** (máy chỉ có .NET 10 sẽ có vài test về môi trường báo lỗi); có thể chạy trong container `mcr.microsoft.com/dotnet/sdk:8.0`.

Địa chỉ máy chủ của ứng dụng lấy theo thứ tự: biến môi trường `HUE_API_URL`, tệp `%ProgramData%\HueTravel\server.txt` (bộ cài ghi), rồi
`ApiBaseUrl` trong `Hue_travel.exe.config`. Không có địa chỉ nào thì ứng dụng chạy **ngoại tuyến** (xem bản đồ và địa điểm; không
đăng nhập, không tìm đường, Trợ lý hiện cẩm nang tĩnh).

Tài khoản quản trị đầu tiên: đăng ký trong ứng dụng rồi cấp quyền một lần bằng SQL (xem [may-chu/HUONG-DAN-CAI-DAT.md](may-chu/HUONG-DAN-CAI-DAT.md), mục 8).

## Tài liệu

| Tài liệu | Nội dung |
|---|---|
| [so-do.md](tai-lieu/so-do.md), [thu-vien-su-dung.md](tai-lieu/thu-vien-su-dung.md), [pgrouting.md](tai-lieu/pgrouting.md) | sơ đồ, thư viện, tìm đường |
| [phan-chia-may-chu-may-khach.md](tai-lieu/phan-chia-may-chu-may-khach.md) | việc nào của máy chủ, việc nào của máy khách |
| [ke-hoach-giao-dien.md](tai-lieu/ke-hoach-giao-dien.md), [huong-dan-trien-khai-giao-dien.md](tai-lieu/huong-dan-trien-khai-giao-dien.md) | thiết kế lại giao diện (đã thực hiện; màu và kiểu bản đồ giữ nguyên) |
| [ke-hoach-chatbot-deepseek.md](tai-lieu/ke-hoach-chatbot-deepseek.md) | kế hoạch Trợ lý (đã thực hiện; nhà cung cấp nay là OpenCode API) |
| [ke-hoach-feeds.md](tai-lieu/ke-hoach-feeds.md) | kế hoạch Khoảnh khắc (đã thực hiện) |
| [ke-hoach-trien-khai-vps.md](tai-lieu/ke-hoach-trien-khai-vps.md), [ke-hoach-chinh-sua.md](tai-lieu/ke-hoach-chinh-sua.md) | kế hoạch đưa lên VPS; chỉnh sửa đăng nhập/khách, bỏ Google, giờ mở cửa, xe mô phỏng |
| [may-chu/VAN-HANH.md](may-chu/VAN-HANH.md) | vận hành hằng ngày, Trợ lý, Khoảnh khắc |

## Nguồn và ghi công

- Dữ liệu đường và sông: © OpenStreetMap contributors (ODbL).
- Thời tiết: [Open-Meteo](https://open-meteo.com) (CC BY 4.0, phi thương mại).
- Bản đồ và hình học: [MapWinGIS](https://mapwingis.github.io/) (MPL).
- Phông chữ: Plus Jakarta Sans (SIL OFL 1.1) nhúng trong ứng dụng; biểu tượng Material Symbols (Apache 2.0); Georgia (có sẵn trên Windows) cho tiêu đề.
- Trợ lý: DeepSeek V4.1 Flash qua [OpenCode API](https://opencode.ai/docs/zen/).
