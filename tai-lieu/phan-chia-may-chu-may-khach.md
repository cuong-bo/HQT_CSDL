# Phân chia máy chủ và máy khách

Hue Travel gồm **hai phần cài riêng**. Phần nặng (dữ liệu, tìm đường, tài khoản) nằm trên máy chủ; máy khách chỉ là ứng dụng
Windows nhẹ, hiển thị bản đồ và gọi máy chủ khi cần.

```
 Máy khách (Windows)                         Máy chủ (VPS Ubuntu)
 ┌───────────────────────────┐   HTTP     ┌──────────────────────────────────────────────┐
 │ Ứng dụng Hue Travel        │──cổng 80──▶│ Caddy ─▶ API .NET 8 ─▶ PostgreSQL            │
 │  - bản đồ nền (vẽ cục bộ) │            │                         + PostGIS + pgRouting│
 │  - hiển thị địa điểm      │◀───────────│  - địa điểm, tài khoản, yêu thích            │
 │  - xe chạy trên tuyến     │  JSON      │  - tìm đường, xếp thứ tự điểm đến            │
 └───────────────────────────┘            └──────────────────────────────────────────────┘
```

## Việc nào do bên nào làm

| Việc | Máy chủ | Máy khách |
|---|---|---|
| Tìm đường (đường bộ thật, từng chặng) | ✔ (pgRouting) | chỉ gửi yêu cầu và vẽ kết quả |
| Xếp thứ tự điểm đến ngắn nhất | ✔ (Held-Karp, tối đa 12 điểm) | chỉ bấm nút |
| Gắn điểm bắt đầu chọn trên bản đồ vào đường; thời gian từng đoạn theo loại đường; nhớ tạm kết quả 5 phút | ✔ | xe chạy theo thời gian máy chủ trả về nên chậm trên đường nhỏ, nhanh trên đường lớn |
| Danh sách địa điểm, tìm kiếm, nhóm | ✔ (nguồn chính) | hiển thị; dùng bản dữ liệu đã tải về để xem khi không có mạng |
| Tài khoản, đăng nhập, yêu thích, lịch trình đã lưu, báo sai, chủ quản | ✔ | giao diện |
| Thời tiết, khu vực đang xem | ✔ (trung chuyển) | hiển thị |
| Cung cấp dữ liệu bản đồ nền (gói shapefile + bản kê phiên bản) | ✔ (`deploy.sh pack`, phục vụ qua Caddy) | tự tải ở lần chạy đầu và khi có bản mới |
| Vẽ bản đồ nền (xã, tỉnh, sông, đường) | – | ✔ (từ dữ liệu đã tải về máy) |
| Xe giả lập chạy trên tuyến | – | ✔ |

> Khi **không có máy chủ**, ứng dụng vẫn mở được bản đồ và xem địa điểm, nhưng **không tìm được đường** (hiện thông báo
> "Cần kết nối máy chủ để tìm đường"), không đăng nhập, không lưu lịch trình.

## Tệp nào đi đâu

### Máy chủ: thư mục `may-chu/` (VPS chỉ cần `git clone`)

| Thư mục / tệp | Dùng để |
|---|---|
| `may-chu/docker-compose.yml`, `Caddyfile`, `scripts/` | Docker Compose, Caddy, script cài và vận hành (`setup-vps.sh`, `deploy.sh`, sao lưu, khóa ký) |
| `may-chu/co-so-du-lieu/` | Cấu trúc cơ sở dữ liệu (`migrations/`), nạp dữ liệu và dựng đồ thị đường (`import/`, `run.sh`) |
| `may-chu/api/` | Mã nguồn API; Docker dựng thành container trên VPS |
| `may-chu/du-lieu/shp/` | Dữ liệu địa điểm, mạng đường, nút giao thông, ranh giới, nạp vào PostgreSQL |
| `may-chu/HUONG-DAN-CAI-DAT.md`, `VAN-HANH.md` | Hướng dẫn từng bước; vận hành |

Các lớp dữ liệu máy chủ cần: `diemDuLich`, `diemAnUong`, `diemLuuTru`, `diemDVDuLich`, `diemDMGiaoThong`, `diemHanhChinh`,
`rgXa`, `MangGiaoThong`, **`NutMangGiaoThong`** (chỉ phục vụ tính đường, không vẽ), `DiaPhan_Tinh_2025`.

Cài đặt: `git clone` rồi làm theo [may-chu/HUONG-DAN-CAI-DAT.md](../may-chu/HUONG-DAN-CAI-DAT.md). Nếu VPS không vào được GitHub: `powershell -ExecutionPolicy Bypass -File may-chu/dong-goi.ps1 -Version 1.0.0` tạo gói `.tar.gz`.

### Máy khách: thư mục `may-khach/` (bộ cài `HueTravel-Setup-<phiên bản>.exe`)

| Thành phần | Ghi chú |
|---|---|
| `Hue_travel.exe` và thư viện | Dựng bản Release, 32-bit, .NET Framework 4.7.2 |
| **MapWinGIS** (kèm GDAL) | Bộ cài tự đăng ký `MapWinGIS.ocx`; máy khách không phải cài riêng |
| Dữ liệu bản đồ (không có trong bộ cài, **tự tải từ máy chủ**) | Lưu ở `%LocalAppData%\HueTravel\Data\shp\` (mỗi người dùng Windows một bản, vì thư mục cài đặt không ghi được). Gồm `NenBien`, `DL_Nc_Ngoai`, `rgXa`, `song_1n`, `song_2n`, `MangGiaoThong`, `DiaPhan_Tinh_2025`, `diem*` và tệp `version.txt`. **Không** có `NutMangGiaoThong` |
| `Data\Photos\` | Ảnh địa điểm (nếu có), theo mã địa điểm |
| Địa chỉ máy chủ | Bộ cài hỏi và lưu vào `%ProgramData%\HueTravel\server.txt` |
| Khóa công khai (không bắt buộc) | Lưu vào `%ProgramData%\HueTravel\public.key` để kiểm tra phản hồi của máy chủ |

Tạo bộ cài: `powershell -ExecutionPolicy Bypass -File may-khach/dong-goi.ps1 -Version 1.0.0 -Server http://IP_VPS` (xem [may-khach/HUONG-DAN-CAI-DAT.md](../may-khach/HUONG-DAN-CAI-DAT.md)).

### Dùng chung, không đóng gói riêng

Kho mã trên GitHub chứa cả hai phần. Dữ liệu shapefile chỉ có **một nguồn**, nằm ở máy chủ: `may-chu/du-lieu/shp/` (địa điểm, đường, nút giao thông, ranh giới, nạp vào PostgreSQL) và `may-chu/du-lieu/nen-ban-do/` (các lớp chỉ để vẽ bản đồ nền: biển, nước ngoài, sông, suối). Máy khách không giữ bản sao trong kho mã.

### Tự tải dữ liệu bản đồ

1. `may-chu/scripts/deploy.sh pack` (cũng chạy ở cuối `deploy.sh import`) gom các lớp máy khách cần (trừ `NutMangGiaoThong`) thành `package.zip`, ghi bản kê `manifest.json` (tên, kích thước, sha256 từng tệp) vào volume `basemap_dist`. Phiên bản = băm nội dung nên dữ liệu không đổi thì phiên bản không đổi.
2. API trả bản kê ở `GET /v1/basemap/manifest` (có chữ ký như mọi phản hồi `/v1`); Caddy phục vụ gói ở `/static/basemap/<phiên bản>/package.zip` (tệp tĩnh, không qua API).
3. Máy khách, trước màn hình đăng nhập: chưa có dữ liệu thì **bắt buộc tải** (hiện tiến độ, có nút thử lại); đã có thì hỏi máy chủ tối đa 4 giây, nếu phiên bản khác thì hỏi người dùng có tải ngay không. Tải về `basemap.download`, kiểm tra sha256 của gói và của từng tệp, giải nén vào `shp.new`, rồi đổi tên thư mục nguyên tử (`shp` cũ thành `shp.old`). Bản đang dùng không bị đụng tới cho tới khi bản mới đủ và đã kiểm tra; mất mạng, ổ đầy hay tắt app giữa chừng đều giữ nguyên bản cũ.

Khi **cập nhật dữ liệu**: chỉ cần đưa dữ liệu mới vào `may-chu/du-lieu` và chạy `deploy.sh import` trên máy chủ. **Không cần** phát hành lại bộ cài.

## Phiên bản và tương thích

- Máy chủ báo phiên bản tối thiểu của máy khách (`MIN_CLIENT_VERSION` trong `may-chu/.env`); máy khách cũ hơn được nhắc cập nhật.
- Đổi cấu trúc dữ liệu (migration `may-chu/co-so-du-lieu/migrations/V00N__*.sql`) chỉ áp dụng khi chạy `scripts/deploy.sh import` trên máy chủ.
- Máy chủ và máy khách nói chuyện qua API `/v1/...`; thêm trường mới là tương thích ngược, đổi nghĩa trường thì phải nâng
  `MIN_CLIENT_VERSION`.
