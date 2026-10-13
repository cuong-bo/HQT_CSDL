# Máy chủ Hue Travel

Phần chạy trên VPS Ubuntu: **Caddy** (cổng 80) → **API .NET 8** → **PostgreSQL 17 + PostGIS + pgRouting**, toàn bộ trong Docker Compose.
Thư mục này **tự đủ**: `git clone` kho mã, `cd may-chu`, rồi làm theo hướng dẫn. Không cần thư mục `may-khach/`.

| Đọc | Khi nào |
|---|---|
| [HUONG-DAN-CAI-DAT.md](HUONG-DAN-CAI-DAT.md) | Cài lần đầu trên VPS mới (từng bước, có kiểm tra) |
| [VAN-HANH.md](VAN-HANH.md) | Vận hành hằng ngày: cập nhật, sao lưu, bảo mật, bản đồ nền |
| [co-so-du-lieu/README.md](co-so-du-lieu/README.md) | Cấu trúc CSDL, nạp dữ liệu, đồ thị đường |

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
