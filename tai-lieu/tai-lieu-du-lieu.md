# Từ điển dữ liệu

Hệ tọa độ: EPSG:3405 (mét). Nhóm địa điểm (`poi.nhom`): `heritage` (di tích, văn hóa), `scenery` (cảnh quan), `food` (ẩm thực,
chưa có dữ liệu), `stay` (lưu trú), `service` (dịch vụ du lịch), `transport` (giao thông).

## Địa điểm

| Bảng | Nội dung |
|---|---|
| `poi` | một bảng chung cho 4 lớp điểm; `gio_mo_cua` và `gia_text` là chữ gốc, `gio_mo`, `gio_dong`, `gia_thap`, `gia_cao` là giá trị tách được (rỗng nếu chữ không theo mẫu); `status` (`visible` / `hidden` / `closed`), `version`, `updated_by`, `deleted_at` (xóa mềm) |
| `poi_snap` | mỗi địa điểm bám vào đoạn đường gần nhất của mạng liên thông: `dist_m` (tới đường), `vertex_dist_m`, `xa_duong` (xa đường hơn 150 m) |
| `hanh_chinh_diem` | 20 điểm hành chính |
| `ranh_tinh`, `ranh_xa` | ranh giới tỉnh và 40 xã/phường (MultiPolygon hợp lệ) |

## Đường

| Bảng | Nội dung |
|---|---|
| `ways` | đoạn đường: `source`, `target`, `cost` (mét), `reverse_cost` (-1 = một chiều), `cost_time` (giây theo tốc độ), `cho_xe`, `lien_thong` |
| `ways_vertices` | nút giao, `component`, `lien_thong` |

Hàm: `route_cost_matrix(ids, by_time)` (tối đa 50 địa điểm), `route_path(từ, đến, by_time)`, `snap_poi(id)`.

## Tài khoản và vai trò

| Bảng | Nội dung |
|---|---|
| `app_user` | `username` (không phân biệt hoa thường), `password_hash` (bcrypt), `role` (`user` / `owner` / `admin`), `status` (`active` / `locked` / `must_change_password`), số lần sai và thời hạn khóa |
| `refresh_token` | chỉ lưu SHA-256 của mã làm mới; dùng một lần |
| `place_owner` | yêu cầu và quyền chủ quản (`pending` / `approved` / `rejected`), một địa điểm có thể có nhiều chủ quản |
| `poi_edit_request` | thay đổi chủ quản đề xuất (JSON các cột được phép: `dia_chi`, `gio_mo_cua`, `gia_text`, `sdt`, `website`, `mo_ta`), chờ quản trị viên duyệt |
| `favorite`, `itinerary`, `itinerary_item` | yêu thích và lộ trình đã lưu |
| `review` | điểm 1 đến 5, nhận xét, ẩn bởi quản trị viên, phản hồi của chủ quản (mỗi người một đánh giá cho mỗi địa điểm) |
| `report` | báo sai thông tin (`wrong_info`, `closed`, `wrong_location`, `other`) |
| `announcement` | thông báo của chủ quản, có thời hạn |
| `poi_stats_daily` | lượt xem và lượt đưa vào lộ trình mỗi ngày |
| `audit_log` | ai, khi nào, làm gì, trên bản ghi nào, giá trị cũ và mới (mọi thay đổi dữ liệu dùng chung) |

## API (tiền tố `/v1`)

Công khai: `GET /pois` (`group` nhiều lần, `q` không dấu, `bbox`, `page`, `pageSize`), `GET /pois/{id}`, `GET /pois/{id}/reviews`,
`GET /pois/{id}/announcements`, `GET /area?lon&lat`, `POST /route`, `GET /weather`, `GET /config`, `GET /healthz`.
Tài khoản: `POST /auth/register|login|refresh`, `GET|PUT /me`, `POST /me/password`.
Người dùng: `/favorites`, `/itineraries`, `POST /pois/{id}/reviews|reports`.
Chủ quản: `/owner/claims`, `/owner/places`, `POST /owner/places/{id}/edit-requests|announcements`, `.../reviews/{id}/reply`, `GET .../stats`.
Quản trị: `/admin/users`, `/admin/claims`, `/admin/edit-requests`, `/admin/reviews`, `/admin/reports`, `/admin/pois`, `/admin/audit`, `/admin/stats`.

Lỗi trả theo ProblemDetails (`application/problem+json`). Mọi phản hồi có `Cache-Control: no-store`.
