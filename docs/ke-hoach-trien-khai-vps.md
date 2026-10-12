# Kế hoạch đưa dự án Cố Đô GIS lên VPS Ubuntu

Ngày lập: 12/10/2026. Phạm vi: chuyển dự án từ "app desktop đọc dữ liệu mẫu" sang "app desktop cài trên máy
người dùng, dữ liệu và xử lý nằm trên VPS Ubuntu". Không dùng tên miền riêng.

## 1. Kiến trúc chốt

```
Máy người dùng (Windows)                         VPS Ubuntu
┌────────────────────────────┐   HTTPS (JSON)   ┌─────────────────────────────────────┐
│ Cố Đô GIS (WinForms +      │ ───────────────► │ Caddy (HTTPS) → API (.NET 8)        │
│ MapWinGIS)                 │ ◄─────────────── │        └─► PostgreSQL 17 + PostGIS +│
│ - vẽ bản đồ, đo, tọa độ    │                  │            pgRouting (Docker)       │
│ - hiển thị kết quả         │                  │ sao lưu hằng ngày, tường lửa        │
│ - chế độ ngoại tuyến (SHP) │                  └─────────────────────────────────────┘
└────────────────────────────┘
```

Nguyên tắc:
- Cổng PostgreSQL (5432) **không bao giờ** mở ra internet; chỉ API nói chuyện với CSDL.
- App chỉ gọi API, không có mật khẩu CSDL trong app.
- Mọi bí mật (mật khẩu CSDL, khóa JWT, khóa tile) nằm trong file `.env` trên VPS, không vào Git.
- App luôn có **chế độ ngoại tuyến** đọc dữ liệu mẫu đi kèm, để demo khi VPS hoặc mạng gặp sự cố.

## 2. Không có tên miền: chọn cách làm HTTPS

| Phương án | Cách làm | Ưu | Nhược |
|---|---|---|---|
| **A (khuyên dùng)** | Hostname miễn phí dạng `tenban.duckdns.org` trỏ về IP VPS, chứng chỉ Let's Encrypt tự gia hạn | Chứng chỉ được tin cậy, miễn phí, ổn định | Phụ thuộc dịch vụ DNS miễn phí; cần mở cổng 80 và 443 |
| B | Chứng chỉ Let's Encrypt cấp **theo địa chỉ IP** (phổ biến từ 01/2026, hiệu lực 160 giờ, phải chọn profile `shortlived`, chỉ xác thực HTTP-01) | Không cần tên miền | Cần ACME client hỗ trợ profile; IP phải cố định; gia hạn rất thường xuyên (cần kiểm tra Caddy/certbot bản hiện hành trước khi chọn) |
| C (dự phòng) | Chứng chỉ tự ký, app **ghim dấu vân tay chứng chỉ** (certificate pinning) | Không phụ thuộc bên thứ ba | Đổi chứng chỉ phải phát hành lại app hoặc cập nhật cấu hình |

**Quyết định hiện tại (12/10/2026): chạy bằng HTTP, không HTTPS.** Đây là lựa chọn rủi ro hơn; các rủi ro và cách giảm
nằm ở mục 9. Lưu ý: phương án A và C ở trên **không cần mua tên miền và không tốn tiền**, nên vẫn nên cân nhắc lại.

## 3. Cấu trúc kho Git đề xuất

```
src/Hue_travel.App        client WinForms (dự án hiện tại)
src/Hue_travel.Core       thư viện netstandard2.0: DTO, hợp đồng API, RoutePlanner, GeoMath
src/Hue_travel.Api        API .NET 8 chạy trên Linux
db/migrations             các file SQL đánh số (V001__..., V002__...)
db/import                 script nạp shapefile (ogr2ogr), script kiểm tra sau nạp
deploy/                   docker-compose.yml, Caddyfile, .env.example, script sao lưu, hướng dẫn cài VPS
installer/                Inno Setup cho client
tests/                    dự án test (xUnit) cho Core, Api, App
docs/
```

Dữ liệu `DuLieu/` (64 MB) **không** đưa vào Git thường; để ngoài kho hoặc dùng Git LFS / file đính kèm của bản phát hành.

## 4. Các giai đoạn

### Giai đoạn 0 - Chốt đầu vào (1 ngày)
- Thông tin VPS: phiên bản Ubuntu, RAM/CPU/ổ đĩa, IP, quyền sudo, cổng 80/443 mở được không.
- Chọn phương án HTTPS (mục 2) và nhà cung cấp tile hợp lệ (MapTiler hoặc Esri) thay cho Google.
- Nhóm dữ liệu bổ sung lớp ẩm thực và sửa lỗi (xem mục 5).
- Tách lại kho theo cấu trúc mục 3.
- **Nghiệm thu:** có file `docs/quyet-dinh-ky-thuat.md` ghi các lựa chọn trên.

### Giai đoạn 1 - Dữ liệu thật trên bản đồ, chạy ngoại tuyến (2 đến 3 ngày)
Làm trước vì không phụ thuộc VPS và trở thành chế độ ngoại tuyến.
- Tạo giao diện `IPoiRepository`, `IAreaLocator`; bản cài đặt `LocalPoiRepository` đọc SHP (đã ở EPSG:3405).
- Vẽ các lớp điểm bằng MapWinGIS theo 6 nhóm; thanh trái lọc thật qua `PoiLayerFilter`.
- Bấm vào điểm mở thẻ thông tin; panel Tra cứu dùng dữ liệu thật thay cho `SamplePois`.
- **Nghiệm thu:** bấm từng nút ở thanh trái thì đúng nhóm hiện ra, bản đồ có điểm thật; test chuyển sang dự án xUnit trong kho.

### Giai đoạn 2 - CSDL PostgreSQL + PostGIS (2 đến 3 ngày)
- Schema (SRID 3405): `poi` hợp nhất từ du lịch, lưu trú, dịch vụ, đầu mối giao thông (cột `nhom`, `loai_hinh`, giờ mở, giờ đóng, giá thấp, giá cao, SĐT, website, nguồn); `hanh_chinh_diem`; `ranh_tinh`; `ranh_xa`; `ways`, `ways_vertices`; bảng người dùng; chỉ mục không gian GIST.
- Script nạp chạy lặp lại được từ CSDL rỗng, kèm script kiểm tra: số bản ghi, SRID, `ST_IsValid`, điểm nằm trong tỉnh, khoảng cách bám đường.
- **Nghiệm thu:** chạy 1 lệnh từ CSDL rỗng ra đủ dữ liệu; mọi kiểm tra đạt; ghi số liệu vào báo cáo.

### Giai đoạn 3 - pgRouting (2 đến 3 ngày)
- Dùng sẵn `source`, `target`, `cost`, `reverse_cost` (đổi tên cột `reverse_co`); loại 24 đoạn lặp và các mảnh rời (chỉ giữ mạng liên thông lớn).
- Thêm `cost_time` = độ dài / tốc độ theo `road_type` (chỉ 1,2% đoạn có `maxspeed`).
- Bảng `poi_snap`: mỗi POI bám vào nút gần nhất, đánh dấu POI cách đường quá 150 m.
- Hàm SQL: ma trận chi phí giữa các điểm (`pgr_dijkstraCostMatrix`) và đường đi giữa hai điểm (hình học).
- **Nghiệm thu:** Ga Huế đến Đại Nội ra tuyến hợp lý; ma trận 8 điểm trả về dưới 1 giây (đo thực tế, ghi lại).

### Giai đoạn 4 - API (4 đến 5 ngày)
Điểm truy cập (tiền tố `/v1`):

| Điểm truy cập | Chức năng |
|---|---|
| `POST /auth/register`, `/auth/login`, `/auth/refresh` | Tài khoản. **Không có OTP, không có "quên mật khẩu" qua email, không có đăng nhập Google** (quyết định ngày 12/10/2026); quản trị viên đặt lại mật khẩu thủ công |
| `GET /pois?group=&q=&bbox=&page=` | Danh sách, lọc theo nhóm (nhiều nhóm), tìm không dấu |
| `GET /pois/{id}` | Chi tiết một địa điểm |
| `GET /area?lon=&lat=` | Tên xã/phường (từ `ranh_xa`) |
| `POST /route` | Gửi điểm đi và danh sách điểm đến; trả thứ tự, các chặng, tổng km, phút, hình tuyến |
| `GET /weather` | Thời tiết Huế, lưu tạm 15 phút ở server |
| `GET /config` | Địa chỉ tile, thông tin nhà cung cấp, phiên bản tối thiểu của app |
| `GET /healthz` | Kiểm tra sống |

Yêu cầu: JWT truy cập (15 phút) + token làm mới, băm mật khẩu Argon2id hoặc bcrypt, giới hạn số lần gọi và số lần đăng nhập sai, truy vấn có tham số (không nối chuỗi SQL), lỗi trả theo ProblemDetails, nhật ký không ghi thông tin cá nhân, tài liệu OpenAPI.
- **Nghiệm thu:** test tích hợp với CSDL thật (chạy trong Docker) đạt; thử tải 20 người dùng đồng thời, p95 dưới 1 giây cho các điểm truy cập chính.

### Giai đoạn 5 - Client chuyển sang chế độ trực tuyến (5 đến 7 ngày)
- Thêm bản cài đặt `ApiPoiRepository`, `ApiRouteService`, `ApiAreaLocator`, `ApiAuthService`, `ApiWeatherService`; chọn `Online` hay `Offline` trong cấu hình.
- Tìm đường: gửi điểm cho API, nhận thứ tự và hình tuyến, vẽ tuyến lên bản đồ; bỏ hệ số 1,3 đường chim bay (giữ làm phương án ngoại tuyến).
- Tra cứu: gọi API khi người dùng ngừng gõ (khoảng 300 ms).
- Đăng nhập, đăng ký nối với API (đã bỏ nút đăng nhập Google và luồng quên mật khẩu/OTP khỏi giao diện); lưu token bằng DPAPI/Windows Credential Manager.
- Lưu tạm danh sách địa điểm vào `%LocalAppData%`; có biểu ngữ "đang ngoại tuyến" và tự thử lại khi mất mạng.
- Đổi nền Google sang nhà cung cấp hợp lệ, hiển thị ghi nguồn.
- Địa chỉ API, chứng chỉ ghim (nếu chọn phương án C) nằm trong file cấu hình.
- **Nghiệm thu:** bộ kiểm tra chức năng (đăng nhập, lọc nhiều nhóm, tìm kiếm, lập lộ trình 3 đến 8 điểm, mất mạng rồi có lại) đạt cả hai chế độ.

### Giai đoạn 6 - Đóng gói client (2 ngày)
- Bộ cài Inno Setup: kiểm tra .NET Framework 4.7.2, chép `MapWinGIS.ocx` cùng thư viện phụ thuộc và dữ liệu GDAL, chạy `regsvr32 /s` với quyền quản trị, tạo lối tắt, gỡ cài đặt sạch.
- Số phiên bản trong `/config` để app báo khi quá cũ; liên kết tải bản mới đặt ở trang phát hành GitHub.
- Thử trên máy Windows 10 và 11 **sạch** (Windows Sandbox hoặc máy ảo).
- **Nghiệm thu:** cài trên máy sạch, mở app, thấy bản đồ và dữ liệu mà không cần cài thêm gì thủ công.

### Giai đoạn 7 - Triển khai VPS Ubuntu (2 ngày)
- Bảo mật máy chủ: người dùng sudo riêng, khóa SSH và tắt đăng nhập bằng mật khẩu, `ufw` chỉ cho 22, 80, 443, `fail2ban`, tự cập nhật bản vá.
- Docker Compose: `db` (PostGIS + pgRouting), `api`, `caddy`; ổ đĩa riêng cho dữ liệu; `.env` không vào Git.
- Lần chạy đầu: áp dụng các migration và nạp dữ liệu.
- Sao lưu: `pg_dump` hằng đêm, giữ 7 bản ngày và 4 bản tuần, sao chép ra nơi khác ngoài VPS; **diễn tập khôi phục** ít nhất một lần.
- Giám sát: kiểm tra `/healthz` bằng dịch vụ miễn phí, cảnh báo đầy ổ đĩa, đọc nhật ký container.
- Quy trình cập nhật và quay lui (kéo image mới, chạy migration, nếu lỗi quay về image cũ).
- **Nghiệm thu:** quét cổng từ ngoài chỉ thấy 22/80/443; khôi phục từ bản sao lưu thành công; app trực tuyến chạy trên VPS.

### Giai đoạn 8 - CI/CD, kiểm thử, tài liệu (2 đến 3 ngày)
- GitHub Actions: build client trên `windows-latest`, chạy test, build image API đẩy lên GHCR, triển khai qua SSH khi gắn thẻ phiên bản.
- Quét bí mật và cập nhật phụ thuộc (Dependabot).
- Tài liệu: README, sơ đồ kiến trúc, sổ tay vận hành VPS, từ điển dữ liệu, mục ghi nguồn (OpenStreetMap ODbL, nhà cung cấp tile, Open-Meteo).
- **Nghiệm thu:** gắn thẻ phiên bản thì tự build và triển khai; người ngoài làm theo README cài được.

## 5. Việc của nhóm dữ liệu (làm song song giai đoạn 0 đến 2)

1. Bổ sung lớp **Cơ sở ăn uống** (hiện thiếu hoàn toàn, nhóm "Ẩm thực" sẽ trống).
2. Sửa `rgXa`: điểm lỗi ở Xã Chân Mây - Lăng Cô (northing 2.314.134), tổng diện tích 5.096 km² so với 4.947 km² của tỉnh, 5 xã lệch diện tích, kiểu chữ ở cột dân số và diện tích.
3. Thêm cột nhóm chuẩn (`nhom`) cho `diemDuLich` (hiện 36 giá trị loại hình tự do); kiểm tra 3 điểm ngoài ranh tỉnh.
4. Bỏ lớp trùng (`DichVuDL`, `diemGiaothong`).
5. Thêm cột số: giá vé, giờ mở, giờ đóng; điền giờ nhận/trả phòng, website, SĐT còn thiếu.
6. Ghi nguồn OpenStreetMap cho đường và sông.

## 6. Rủi ro

| Rủi ro | Cách giảm |
|---|---|
| VPS hoặc mạng gián đoạn | Lưu tạm dữ liệu, chế độ ngoại tuyến, giám sát và sao lưu |
| IP VPS thay đổi hoặc hostname miễn phí ngưng | Có phương án C (ghim chứng chỉ) và cấu hình đổi địa chỉ không cần phát hành lại |
| Điều khoản tile và Open-Meteo (phi thương mại, hạn mức) | Chọn nhà cung cấp hợp lệ; thời tiết lưu tạm ở server |
| Đăng ký MapWinGIS thất bại trên máy người dùng | Bộ cài tự đăng ký, thử trên máy sạch, hướng dẫn xử lý lỗi |
| Trình cài đặt chưa ký bị Windows SmartScreen cảnh báo | Ghi chú trong hướng dẫn; ký mã nếu có điều kiện |
| Mạng đường có mảnh rời, ít tốc độ | Loại mảnh rời, gán tốc độ theo loại đường, kiểm tra tuyến mẫu |

## 7. Thứ tự và ước lượng

Đường găng: giai đoạn 0 → 2 → 3 → 4 → 5 → 6 → 7 → 8. Giai đoạn 1 chạy trước, song song với 2 và 3.
Tổng ước lượng 3 đến 4 tuần làm việc cho một người; đây là ước lượng thô, sẽ điều chỉnh sau giai đoạn 2.

## 8. Cần bạn quyết định

1. Mục đích: demo chấm điểm hay có người dùng thật? Nếu chỉ chấm điểm, có thể bỏ luôn tài khoản ở bản đầu.
2. Phương án HTTPS (mục 2): A, B, C, hay vẫn dùng HTTP (mục 9).
3. Nhà cung cấp tile thay Google.
4. Thông số VPS (phiên bản Ubuntu, RAM, CPU, ổ đĩa).

## 9. Rủi ro khi dùng HTTP và cách giảm

HTTP không mã hóa và không xác thực máy chủ. Với dự án này, các lỗi và rủi ro có thể xảy ra như sau.

### 9.1 Bảo mật

| # | Rủi ro | Khi nào xảy ra | Cách giảm |
|---|---|---|---|
| 1 | **Lộ mật khẩu và token** (gửi dạng chữ rõ) | Người dùng dùng Wi-Fi công cộng, mạng trường, hoặc ISP bị nghe lén. Lấy được token là chiếm được phiên | Tốt nhất là chuyển sang HTTPS (mục 2). Nếu giữ HTTP: cảnh báo người dùng không dùng mật khẩu quan trọng, token sống ngắn, không lưu thông tin nhạy cảm (SĐT, email thật) |
| 2 | **Dữ liệu bị sửa giữa đường** (địa điểm, tuyến đường bị đổi hoặc chèn nội dung) | Kẻ tấn công ở giữa, hoặc Wi-Fi công cộng chèn trang đăng nhập/quảng cáo | Server ký mọi phản hồi (ECDSA P-256), app nhúng khóa công khai và kiểm tra chữ ký. Kiểm tra `Content-Type`, báo lỗi rõ khi nhận HTML thay vì JSON |
| 3 | **Phát lại yêu cầu** (replay) | Bắt gói tin đăng nhập rồi gửi lại | Số dùng một lần (nonce) và dấu thời gian, token ngắn hạn |
| 4 | **Vét cạn mật khẩu** | Không có HTTPS không làm nặng thêm, nhưng dễ bị thử hàng loạt | Giới hạn số lần đăng nhập sai theo tài khoản và địa chỉ IP |
| 5 | **Lộ vị trí người dùng** | `/area` và `/route` gửi tọa độ; ai nghe được biết người dùng đang ở đâu (dữ liệu cá nhân) | Chỉ gửi tọa độ khi cần, làm tròn tọa độ, không ghi vào nhật ký |
| 6 | **Tải bộ cài hoặc bản cập nhật qua HTTP** | Kẻ tấn công thay bộ cài bằng mã độc (rất nguy hiểm) | **Chỉ phát hành bộ cài qua HTTPS** (ví dụ GitHub Releases) và công bố mã SHA-256; app không bao giờ tải bản cập nhật qua HTTP |
| 7 | Yêu cầu pháp lý, chấm điểm | Thu thập email, SĐT, mật khẩu mà không mã hóa đường truyền | Tối thiểu hóa dữ liệu: bỏ SĐT, email nếu không cần; hoặc không có tài khoản |

### 9.2 Lỗi vận hành và tương thích

| # | Lỗi | Nguyên nhân | Cách giảm |
|---|---|---|---|
| 8 | **App không kết nối được** | Mạng trường, công ty chặn hoặc ép qua proxy các kết nối HTTP tới IP trần hay cổng lạ; diệt virus cảnh báo | Dùng cổng 80, báo lỗi rõ cho người dùng, tự chuyển sang chế độ ngoại tuyến |
| 9 | **Nhận HTML thay vì JSON** | Wi-Fi công cộng chặn bằng trang đăng nhập (captive portal) | Kiểm tra `Content-Type` và chữ ký; thông báo "mạng yêu cầu đăng nhập" |
| 10 | **Dữ liệu cũ hoặc của người khác** | Proxy trong suốt lưu bộ nhớ đệm các yêu cầu GET | Gửi `Cache-Control: no-store` (hoặc `private`) cho phản hồi có xác thực |
| 11 | **Lỗi chuyển hướng HTTPS** | Caddy mặc định tự bật HTTPS và chuyển hướng 80 sang 443 | Khi chỉ dùng HTTP phải khai báo `http://` hoặc tắt `auto_https` trong Caddyfile; thử bằng `curl` trước khi phát hành app |
| 12 | **App hỏng khi IP VPS đổi** | Địa chỉ IP cố định trong app | Địa chỉ API nằm trong cấu hình; dùng hostname miễn phí (DuckDNS) thay IP cũng được với HTTP |
| 13 | **Tile và thời tiết** | Không ảnh hưởng: tile và Open-Meteo vẫn đi qua HTTPS riêng của họ; riêng API của dự án mới là HTTP | Không cần làm gì |

### 9.3 Ba mức khuyến nghị

1. **Khuyên dùng:** HTTPS với hostname miễn phí (phương án A) hoặc ghim chứng chỉ (phương án C). Chi phí bằng 0, chỉ thêm khoảng nửa ngày công.
2. **Nếu giữ HTTP và giữ tài khoản:** áp dụng các cách giảm ở 9.1, đặc biệt ký phản hồi, token ngắn hạn, giới hạn số lần thử, và phát hành bộ cài qua HTTPS.
3. **Rủi ro thấp nhất khi giữ HTTP:** bỏ tài khoản; app dùng ẩn danh với dữ liệu công khai (địa điểm, tìm đường). Khi đó không còn mật khẩu hay token nào để lộ.
