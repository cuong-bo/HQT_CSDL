# Phân chia máy chủ và máy khách

Cố Đô GIS gồm **hai phần cài riêng**. Phần nặng (dữ liệu, tìm đường, tài khoản) nằm trên máy chủ; máy khách chỉ là ứng dụng
Windows nhẹ, hiển thị bản đồ và gọi máy chủ khi cần.

```
 Máy khách (Windows)                         Máy chủ (VPS Ubuntu)
 ┌───────────────────────────┐   HTTP     ┌──────────────────────────────────────────────┐
 │ Ứng dụng Cố Đô GIS        │──cổng 80──▶│ Caddy ─▶ API .NET 8 ─▶ PostgreSQL            │
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
| Danh sách địa điểm, tìm kiếm, nhóm | ✔ (nguồn chính) | hiển thị; có bản dữ liệu đi kèm để xem khi không có mạng |
| Tài khoản, đăng nhập, yêu thích, lịch trình đã lưu, báo sai, chủ quản | ✔ | giao diện |
| Thời tiết, khu vực đang xem | ✔ (trung chuyển) | hiển thị |
| Vẽ bản đồ nền (xã, tỉnh, sông, đường) | – | ✔ (từ dữ liệu đi kèm bộ cài) |
| Xe giả lập chạy trên tuyến | – | ✔ |

> Khi **không có máy chủ**, ứng dụng vẫn mở được bản đồ và xem địa điểm, nhưng **không tìm được đường** (hiện thông báo
> "Cần kết nối máy chủ để tìm đường"), không đăng nhập, không lưu lịch trình.

## Tệp nào đi đâu

### Đưa lên máy chủ (gói `CoDoGIS-MayChu-<phiên bản>.tar.gz`)

| Thư mục / tệp | Dùng để |
|---|---|
| `deploy/` | Docker Compose, Caddy, script cài và vận hành (`setup-vps.sh`, `deploy.sh`, sao lưu, khóa ký) |
| `db/` | Cấu trúc cơ sở dữ liệu (`migrations/`), nạp dữ liệu và dựng đồ thị đường (`import/`, `run.sh`) |
| `src/Hue_travel.Api/` | Mã nguồn API; Docker dựng thành container trên VPS |
| `Hue_travel/Data/shp/` (chỉ các lớp máy chủ cần) | Dữ liệu địa điểm, mạng đường, nút giao thông, ranh giới, nạp vào PostgreSQL |
| `HUONG-DAN-CAI-DAT.md` | Hướng dẫn từng bước |

Các lớp dữ liệu máy chủ cần: `diemDuLich`, `diemAnUong`, `diemLuuTru`, `diemDVDuLich`, `diemDMGiaoThong`, `diemHanhChinh`,
`rgXa`, `MangGiaoThong`, **`NutMangGiaoThong`** (chỉ phục vụ tính đường, không vẽ), `DiaPhan_Tinh_2025`.

Tạo gói: `powershell -ExecutionPolicy Bypass -File scripts\dong-goi-may-chu.ps1 -Version 1.0.0`

### Đóng gói cho máy khách (bộ cài `CoDoGIS-Setup-<phiên bản>.exe`)

| Thành phần | Ghi chú |
|---|---|
| `Hue_travel.exe` và thư viện | Dựng bản Release, 32-bit, .NET Framework 4.7.2 |
| **MapWinGIS** (kèm GDAL) | Bộ cài tự đăng ký `MapWinGIS.ocx`; máy khách không phải cài riêng |
| `Data\shp\` (bản đồ nền + địa điểm dự phòng) | `NenBien`, `DL_Nc_Ngoai`, `rgXa`, `song_1n`, `song_2n`, `MangGiaoThong`, `DiaPhan_Tinh_2025`, `diem*`. **Không** có `NutMangGiaoThong` |
| `Data\Photos\` | Ảnh địa điểm (nếu có), theo mã địa điểm |
| Địa chỉ máy chủ | Bộ cài hỏi và lưu vào `%ProgramData%\CoDoGIS\server.txt` |
| Khóa công khai (không bắt buộc) | Lưu vào `%ProgramData%\CoDoGIS\public.key` để kiểm tra phản hồi của máy chủ |

Tạo bộ cài: `powershell -ExecutionPolicy Bypass -File scripts\dong-goi-may-khach.ps1 -Version 1.0.0 -Server http://IP_VPS`

### Dùng chung, không đóng gói riêng

Kho mã trên GitHub chứa cả hai phần. Dữ liệu gốc nằm ở `Hue_travel/Data/shp/`; mỗi gói chỉ lấy phần mình cần. Khi **cập nhật dữ liệu**
cần làm cả hai việc: nạp lại vào máy chủ (`deploy.sh import`) và phát hành lại bộ cài (dữ liệu bản đồ nền nằm trong bộ cài).

## Phiên bản và tương thích

- Máy chủ báo phiên bản tối thiểu của máy khách (`MIN_CLIENT_VERSION` trong `deploy/.env`); máy khách cũ hơn được nhắc cập nhật.
- Đổi cấu trúc dữ liệu (migration `db/migrations/V00N__*.sql`) chỉ áp dụng khi chạy `deploy.sh import` trên máy chủ.
- Máy chủ và máy khách nói chuyện qua API `/v1/...`; thêm trường mới là tương thích ngược, đổi nghĩa trường thì phải nâng
  `MIN_CLIENT_VERSION`.
