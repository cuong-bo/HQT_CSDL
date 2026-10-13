# hue-travel — dự án test (tách biệt hoàn toàn với ecoai)

Dự án thật là **Cố Đô GIS** (`cuong-bo/HQT_CSDL`): WinForms client cho khách du
lịch Huế + máy chủ API .NET 8 / PostgreSQL 17 (PostGIS + pgRouting) / Caddy,
chạy bằng Docker Compose. Phần máy khách (`may-khach/`) là ứng dụng Windows,
không chạy trên VPS. File này chỉ nói về phần **máy chủ đã cài trên VPS này**
và cách nó tách biệt hoàn toàn với `ecoai-thpt-thanhnua.com` đang chạy song
song, để có thể **dọn sạch bằng một lệnh** khi không cần nữa.

## Quy ước cách ly (đã áp dụng)

| Thành phần | ecoai (đang chạy) | hue-travel (dự án này) |
|---|---|---|
| Docker Compose project name | `ecoai` | `hue-travel` |
| Tiền tố container | `ecoai_*_prod` | `hue-travel-*-1` |
| Docker network | `ecoai_default` (172.18.0.0/16) | `hue-travel_internal` (172.30.0.0/24) |
| Docker volumes | `ecoai_ecoai_pgdata`, `ecoai_ecoai_uploads` | `hue-travel_pgdata`, `hue-travel_caddy_data` |
| Lối vào công khai | nginx trên host, cổng 80/443 | Caddy **trong container**, cổng host **8082** |
| Postgres | không expose port ra host | không expose port ra host |
| Thư mục | `/root/ecoai` | `/root/hue-travel` |
| Firewall (ufw) | 22, 80, 443 (đã có từ trước) | + rule riêng: `8082/tcp` (comment "hue-travel test API") |

Không có domain riêng cho dự án này nên **không đụng tới nginx/certbot của
ecoai**. Theo đúng thiết kế gốc của dự án (xem `may-chu/Caddyfile`: "Plain
HTTP only: there is no domain name"), Caddy chạy HTTP thuần, chỉ đổi cổng từ
80 (bị ecoai chiếm) sang **8082**.

## Những gì đã sửa so với bản gốc trên GitHub (chỉ để tránh xung đột cổng)

1. `may-chu/docker-compose.yml`: service `caddy`, cổng `"80:80"` → `"8082:80"`.
2. `may-chu/scripts/deploy.sh`: 3 chỗ gọi `http://localhost/healthz` (hàm
   `healthy()` và case `status`) → `http://localhost:8082/healthz`.

Không sửa gì khác — mã API, CSDL, migrations, dữ liệu shapefile giữ nguyên bản gốc.

**Không chạy** `scripts/setup-vps.sh`: script đó cài đặt ở mức toàn hệ thống
(ufw mặc định deny, fail2ban, tạo user `deploy`, unattended-upgrades) — không
phù hợp khi VPS đã có ecoai chạy sẵn với cấu hình riêng. Docker đã có sẵn nên
bỏ qua bước này; chỉ mở thêm đúng 1 rule ufw cho cổng 8082.

## Đã cài đặt xong, đang chạy

```
hue-travel-db-1     PostgreSQL 17 + PostGIS + pgRouting   (healthy, không expose port)
hue-travel-api-1    API .NET 8                            (nội bộ, không expose port)
hue-travel-caddy-1  Caddy — cổng host 0.0.0.0:8082 → container :80 → api:8080
```

Dữ liệu đã nạp xong (`docker compose --profile tools run --rm tools`): 190
địa điểm (food 80, heritage 34, scenery 15, service 12, stay 38, transport
11), đồ thị đường 61.808 cạnh. 2 cảnh báo dữ liệu đã biết (POI ngoài ranh
tỉnh / cách đường >150m) — không chặn cài đặt, là lỗi dữ liệu nguồn.

Kiểm tra nhanh:
```bash
curl http://<ip-vps>:8082/healthz
curl "http://<ip-vps>:8082/v1/pois?group=stay"
```

Bí mật đã tạo ngẫu nhiên và nằm trong `may-chu/.env` (không commit vào Git,
`chmod 600`): `POSTGRES_PASSWORD`, `APP_DB_PASSWORD`, `JWT_KEY`, `SIGNING_KEY`
(khoá ký phản hồi ECDSA P-256 — khoá công khai tương ứng đã đưa cho bạn ở hội
thoại cài đặt, cần dán vào cấu hình máy khách nếu muốn bật kiểm tra chữ ký).

## Việc còn lại (tuỳ chọn, chưa làm vì cần quyết định của bạn)

- **Tài khoản quản trị đầu tiên**: phải đăng ký qua ứng dụng Windows trước,
  sau đó chạy trên VPS: `docker compose exec db psql -U huegis -d huegis -c
  "update app_user set role='admin' where username='...'"` (xem mục 8 của
  `may-chu/HUONG-DAN-CAI-DAT.md`).
- **Sao lưu tự động** (mục 9 của hướng dẫn): chưa thêm crontab, vì đây là môi
  trường test.
- **Domain + HTTPS**: nếu sau này muốn domain riêng, tạo file nginx site mới
  (không sửa file của ecoai), `proxy_pass` vào `127.0.0.1:8082`, rồi chạy
  certbot cho domain đó — khi ấy nên đổi cổng Caddy về bind `127.0.0.1:8082`
  thay vì `0.0.0.0:8082` và đóng lại rule ufw 8082 (nginx sẽ là cửa duy nhất,
  giống ecoai).

## Dọn dẹp toàn bộ (khi xong việc test)

```bash
bash /root/hue-travel/teardown.sh
```

Script dừng/xoá container, network, volume của project `hue-travel` và gỡ
rule ufw cổng 8082 — **không đụng** tới bất kỳ thứ gì của `ecoai`. Xem chi
tiết trong `teardown.sh`.
