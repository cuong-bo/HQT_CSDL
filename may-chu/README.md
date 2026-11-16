# Máy chủ Hue Travel

Phần chạy trên VPS Ubuntu: **Caddy** (cổng 80) → **API .NET 8** → **PostgreSQL 17 + PostGIS + pgRouting**, toàn bộ trong Docker Compose.
Thư mục này **tự đủ**: `git clone` kho mã, `cd may-chu`, rồi làm theo hướng dẫn. Không cần thư mục `may-khach/`.

| Đọc | Khi nào |
|---|---|
| [HUONG-DAN-CAI-DAT.md](HUONG-DAN-CAI-DAT.md) | Cài lần đầu trên VPS mới (từng bước, có kiểm tra) |
| [VAN-HANH.md](VAN-HANH.md) | Vận hành hằng ngày: cập nhật, sao lưu, bảo mật, bản đồ nền |
| [co-so-du-lieu/README.md](co-so-du-lieu/README.md) | Cấu trúc CSDL, nạp dữ liệu, đồ thị đường |

## Chức năng của API

| Nhóm | Đường dẫn chính (`/v1/...`) |
|---|---|
| Đăng nhập, tài khoản | `auth/register`, `auth/login`, `auth/refresh`, `auth/logout`, `me`, `me/password` |
| Địa điểm, ảnh, đánh giá | `pois`, `pois/{id}`, `pois/{id}/photos`, `pois/{id}/reviews`, `photos/{id}`, yêu thích, lịch trình |
| Tìm đường, thời tiết, khu vực | `route` (pgRouting, tối đa 12 điểm), `weather`, `area` |
| Chủ quản, quản trị | `owner/...` (nhận quản lý, thông báo, ảnh, chỉnh sửa), `admin/...` (người dùng, địa điểm, duyệt, báo cáo, nhật ký) |
| **Trợ lý** | `chat` (POST), `chat/status`: máy chủ gọi DeepSeek V4.1 Flash qua OpenCode API, mô hình dùng các công cụ tìm địa điểm / xem địa điểm / lập lộ trình / thời tiết / danh sách tour; hạn mức khách 20 và người dùng 100 câu mỗi ngày |
| **Khoảnh khắc** (cần `FEED_ENABLED=true`, nếu không trả 503) | `feed`, `feed/new-count`, `posts`, `posts/{id}/like`, `posts/{id}/comments`, `posts/{id}/reports`, `media` (ảnh lưu trong CSDL, nén lại), `me/feed-terms`, `admin/feed/pending`, `admin/feed/reports` |
| Cấu hình, bản đồ nền, sức khỏe | `config`, `basemap/manifest`, `/healthz` |

Bảng tin Khoảnh khắc: tài khoản dưới 24 giờ hoặc chưa có đủ 2 bài được duyệt thì bài vào hàng chờ duyệt; giới hạn mỗi ngày (bài, bình luận, ảnh); bài tự ẩn khi có 3 người khác nhau báo cáo. Chi tiết vận hành: [VAN-HANH.md](VAN-HANH.md).

Kiểm thử: `dotnet test kiem-thu` (153 test, cần runtime .NET 8; biến `HUE_TEST_DB` trỏ vào một CSDL đã nạp để chạy các test tích hợp).

## Bố cục

```
may-chu/
├── api/                  API .NET 8 (Dockerfile ở đây)
├── co-so-du-lieu/        migrations/, import/ (SQL dựng đường), run.sh (nạp shapefile)
├── docker-csdl/          Dockerfile PostgreSQL + PostGIS + pgRouting
├── du-lieu/shp/          shapefile máy chủ cần (địa điểm, mạng đường, nút giao thông, ranh giới)
├── du-lieu/nen-ban-do/   lớp chỉ để vẽ bản đồ nền (biển, nước ngoài, sông, suối); không nạp vào CSDL
├── scripts/pack-basemap.py  đóng gói dữ liệu bản đồ cho máy khách tự tải (chạy bằng `deploy.sh pack`)
├── scripts/              setup-vps.sh, deploy.sh, backup.sh, restore.sh, gen-signing-key.sh
├── kiem-thu/             test API (xUnit)
├── docker-compose.yml    db + api + caddy (+ job "tools" để nạp dữ liệu)
├── Caddyfile             proxy HTTP cổng 80
├── .env.example          mẫu bí mật; sao chép thành .env (không đưa lên Git)
└── dong-goi.ps1          (tùy chọn) đóng gói .tar.gz khi VPS không vào được GitHub
```

## Tóm tắt lệnh (đã cài xong môi trường, đã có `.env`)

```bash
cd /opt/hue-travel/may-chu
scripts/deploy.sh up       # dựng và chạy
scripts/deploy.sh import   # nạp dữ liệu, dựng đồ thị đường
scripts/deploy.sh status   # {"status":"ok"}
scripts/deploy.sh update   # git pull + dựng lại API, tự quay về bản cũ nếu lỗi
```
