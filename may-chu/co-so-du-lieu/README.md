# Cơ sở dữ liệu Hue Travel (PostgreSQL 17 + PostGIS + pgRouting)

Hệ tọa độ duy nhất: **VN-2000 / UTM zone 48N (EPSG:3405)**, đơn vị mét. Mọi cột hình học đều có SRID 3405.

## Chạy

Cần `psql` và `shp2pgsql` (gói `postgresql-client` và `postgis`), CSDL đã tạo sẵn, có các extension `postgis`,
`pgrouting`, `unaccent`, `pg_trgm`, `citext` (người dùng có quyền tạo extension).

```bash
export PGHOST=127.0.0.1 PGPORT=5432 PGUSER=postgres PGPASSWORD=... PGDATABASE=huegis
./co-so-du-lieu/run.sh   # đọc shapefile ở ./du-lieu/shp
./co-so-du-lieu/run.sh /duong/dan/shp   # hoặc chỉ thư mục khác
```

Chạy lặp lại được. Mỗi lần: áp dụng các migration chưa chạy → nạp shapefile vào schema tạm `stg` → `transform.sql`
(upsert theo mã; POI đã được sửa qua app (`updated_by` có giá trị) **không bị ghi đè**; POI biến mất khỏi dữ liệu chỉ bị đánh dấu xóa mềm `deleted_at`, không xóa thật; trạng thái, phiên bản và liên kết chủ quản được giữ) → `build_routing.sql` (dựng lại
toàn bộ đồ thị đường) → `verify.sql` (dừng với mã lỗi nếu sai số liệu, in cảnh báo cho lỗi dữ liệu đã biết).

## Cấu trúc

| Thư mục / file | Nội dung |
|---|---|
| `migrations/V001..V010` | extension và hàm hỗ trợ; `app_user`, `audit_log`; `poi`, `hanh_chinh_diem`, `ranh_tinh`, `ranh_xa`, `place_owner`; `ways`, `ways_vertices`, `poi_snap`, `route_cost_matrix()`, `route_path()`; V010: ảnh địa điểm `poi_photo` (ảnh nén lại thành JPEG, lưu trong CSDL, admin duyệt); V008-V009: tìm đường từ một điểm tự do (V009 gắn điểm vào đúng đoạn đường gần nhất, `snap_point()`, `route_path_from_point()`) |
| `import/transform.sql` | từ `stg` sang bảng ứng dụng (gộp 4 lớp điểm thành `poi`) |
| `import/build_routing.sql` | đồ thị: bỏ đoạn tự nối, tốc độ theo loại đường, chỉ giữ mạng liên thông mạnh lớn nhất (tính cả đường một chiều), bám POI vào đoạn đường gần nhất rồi vào đầu gần hơn của đoạn đó |
| `import/verify.sql` | kiểm tra số bản ghi, SRID, hình học hợp lệ, điểm trong tỉnh, khoảng cách bám đường, tuyến mẫu Ga Huế đến Đại Nội |

Thêm migration mới bằng cách tạo file `V005__ten.sql`; không sửa file đã chạy.

## Quy ước đồ thị đường

- `cost`, `reverse_cost`: mét. `cost_time`, `reverse_cost_time`: giây. **Giá trị âm = không đi được theo chiều đó** (đường một chiều).
- Đường dành cho người đi bộ, bậc thang, đường xe đạp có `cho_xe = false`; chỉ mạng liên thông lớn nhất có `lien_thong = true`.
- Hàm truy vấn chỉ dùng đoạn `cho_xe and lien_thong`. Dựng lại đồ thị dùng `DELETE` trong một giao dịch nên người đang tìm đường không bị chặn.
- `route_path` trả về rỗng khi: địa điểm không có, không có đường đi, hoặc hai địa điểm vào đồ thị ở cùng một nút. Nó thử vùng quanh hai đầu (đệm 5 km) trước, không thấy mới dùng cả mạng.
- `route_cost_matrix` nhận tối đa 50 địa điểm.
- `poi_snap.dist_m` là khoảng cách từ địa điểm tới đoạn đường gần nhất; `vertex_dist_m` là tới nút nơi nó vào đồ thị.

```sql
-- ma trận thời gian (giây) giữa các địa điểm, dùng cho sắp xếp lộ trình
select * from route_cost_matrix(array['DL001','GT002','LT001'], true);
-- đường đi giữa hai địa điểm, từng đoạn theo thứ tự di chuyển
select * from route_path('GT002', 'DL019');
```

## Lỗi dữ liệu đã biết (việc của nhóm dữ liệu, xem `../../tai-lieu/ke-hoach-trien-khai-vps.md` mục 5)

`verify.sql` in các cảnh báo sau; chúng không làm hỏng việc nạp:

- 4 điểm du lịch nằm ngoài ranh tỉnh (ví dụ Bãi biển Cảnh Dương cách đường gần nhất hơn 14 km).
- `ranh_xa`: một xã có đỉnh lỗi chồng ra ngoài tỉnh; tổng diện tích xã lệch khoảng 116 km² so với tỉnh.
- 16 điểm cách đường hơn 150 m (phần lớn ở núi, đầm phá, bờ biển); `poi_snap.xa_duong = true` cho các điểm này, tính tuyến
  nên cộng thêm đoạn đi bộ.
- Lớp ẩm thực chưa có nên `nhom = 'food'` trống.
