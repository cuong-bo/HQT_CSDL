# Kế hoạch đưa dự án Cố Đô GIS lên VPS Ubuntu

Ngày lập: 12/10/2026. Phạm vi: chuyển dự án từ "app desktop đọc dữ liệu mẫu" sang "app desktop cài trên máy
người dùng, dữ liệu và xử lý nằm trên VPS Ubuntu". Không dùng tên miền riêng.

## 0. Tình trạng thực hiện (cập nhật 13/10/2026)

Đã làm xong và kiểm tra trên máy dựng (PostgreSQL 17 + PostGIS + pgRouting thật): **52 test API** (gồm test tích hợp với CSDL thật),
**153 kiểm tra phía app**, dựng CSDL từ rỗng trong khoảng 18 giây, chạy lại nhiều lần vẫn đúng.

| Giai đoạn | Trạng thái | Ghi chú |
|---|---|---|
| 1. Dữ liệu thật trên bản đồ, ngoại tuyến | Xong | 109 điểm, lọc theo nhóm, bấm điểm hiện thẻ |
| 2. PostgreSQL + PostGIS | Xong | `db/`, 7 migration, nạp lặp lại được, giữ chỉnh sửa của app |
| 3. pgRouting | Xong | ma trận 8 điểm 0,24 giây; tuyến Ga Huế đến Đại Nội 2,9 km |
| 4. API (.NET 8) | Xong | đăng nhập, địa điểm, tìm đường, ba vai trò (R1 đến R4), nhật ký kiểm toán; đã rà soát bảo mật và sửa |
| 5. App trực tuyến | Xong | tài khoản, địa điểm từ máy chủ có lưu tạm, tìm đường bộ thật vẽ lên bản đồ, thời tiết và tên xã từ máy chủ, giao diện người dùng / chủ quản / quản trị |
| 6. Bộ cài | Có script, chưa thử | `installer/CoDoGIS.iss` cần máy có MapWinGIS để dựng; chưa thử trên máy sạch |
| 7. Triển khai VPS | Có file, chưa chạy thật | Docker Compose, Caddy (HTTP), script cài VPS, sao lưu, khôi phục; máy dựng không chạy Docker nên chưa thử |
| 8. CI/CD, tài liệu | Có file, chưa chạy trên GitHub | `.github/workflows`, README, từ điển dữ liệu |

**Các quyết định đã chốt (không còn là câu hỏi mở của mục 10.8):**
chủ quản không sửa dữ liệu trực tiếp mà gửi yêu cầu chỉnh sửa, quản trị viên duyệt; tài khoản chủ quản có hai đường (gửi yêu cầu nhận địa điểm
rồi được duyệt, hoặc quản trị viên tạo sẵn); một địa điểm có thể có nhiều chủ quản; đánh giá hiển thị ngay và quản trị viên ẩn khi cần;
chưa làm tải ảnh lên; nên có ít nhất 2 quản trị viên; dùng HTTP, không HTTPS, không đăng nhập Google, không OTP.

**Chưa làm hoặc còn rủi ro** (đã biết, chưa che hết):
1. HTTP: mật khẩu và token đi dạng chữ thường; chưa ký phản hồi chống sửa giữa đường (mục 9).
2. Bản đồ nền vẫn là Google qua HTTP, chỉ hợp để phát triển; cần nhà cung cấp hợp lệ khi phát hành (địa chỉ nền đã có sẵn trong `GET /v1/config`, app chưa đọc).
3. Chưa thử bộ cài trên máy Windows sạch, chưa chạy Docker Compose và script VPS trên Ubuntu thật, chưa chạy CI trên GitHub.
4. Dữ liệu: lớp ẩm thực chưa có; 4 điểm ngoài ranh tỉnh; 1 xã có đỉnh lỗi; 16 điểm cách đường hơn 150 m (xem `db/README.md`).
5. Bảng tìm đường trong app gợi ý 4 điểm cố định (Đại Nội, Thiên Mụ, Lăng Tự Đức, Lăng Khải Định); API đã hỗ trợ đến 12 điểm bất kỳ.

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
3. **Rủi ro thấp nhất khi giữ HTTP:** bỏ tài khoản; app dùng ẩn danh với dữ liệu công khai (địa điểm, tìm đường). Khi đó không còn mật khẩu hay token nào để lộ. (Lựa chọn này **không còn khả thi** sau quyết định ở mục 10: hệ thống có tài khoản.)

## 10. Ba nhóm đối tượng: Quản trị viên, Người dùng, Chủ quản địa điểm

Quyết định ngày 12/10/2026: hệ thống phục vụ ba vai trò. Điều này **thay đổi** mục 9: có tài khoản quản trị và chủ quản
được quyền sửa dữ liệu, nên lộ mật khẩu hoặc token qua HTTP kéo theo nguy cơ sửa dữ liệu của cả hệ thống. Khi có hai vai trò
này, **nên bắt buộc dùng HTTPS** (phương án A hoặc C ở mục 2, miễn phí, không cần mua tên miền).

### 10.1 Ma trận quyền

| Chức năng | Khách (chưa đăng nhập) | Người dùng | Chủ quản địa điểm | Quản trị viên |
|---|---|---|---|---|
| Xem bản đồ, lọc theo nhóm, tra cứu, thời tiết | Có | Có | Có | Có |
| Lập lộ trình tối ưu | Có | Có | Có | Có |
| Lưu lộ trình, địa điểm yêu thích | Không | Có | Có | Có |
| Đánh giá, nhận xét địa điểm | Không | Có (qua kiểm duyệt) | Chỉ phản hồi đánh giá địa điểm của mình | Có |
| Báo sai thông tin địa điểm | Không | Có | Có | Có |
| Sửa thông tin địa điểm của mình (giờ mở, giá vé, SĐT, mô tả, ảnh, trạng thái tạm đóng) | Không | Không | Có | Có (mọi địa điểm) |
| Đề xuất đổi tên, loại hình, vị trí địa điểm | Không | Không | Có (chờ duyệt) | Có (áp dụng ngay) |
| Thông báo, sự kiện, khuyến mại của địa điểm | Không | Không | Có | Có |
| Thống kê lượt xem, lượt chọn vào lộ trình | Không | Không | Có (địa điểm của mình) | Có (toàn hệ thống) |
| Thêm, xóa địa điểm; nạp dữ liệu | Không | Không | Không | Có |
| Duyệt đề xuất sửa, duyệt yêu cầu làm chủ quản | Không | Không | Không | Có |
| Quản lý tài khoản (khóa, đặt lại mật khẩu, đổi vai trò) | Không | Không | Không | Có |
| Kiểm duyệt đánh giá, xử lý báo sai | Không | Không | Không | Có |
| Xem nhật ký thay đổi, sao lưu | Không | Không | Không | Có |

Mọi kiểm tra quyền thực hiện **ở server** (vai trò nằm trong token, quyền sở hữu tra trong bảng `place_owner`).
Ẩn nút ở app chỉ để giao diện gọn, không phải biện pháp bảo mật.

### 10.2 Tài khoản khi không có email
- **Người dùng:** tự đăng ký bằng tên đăng nhập và mật khẩu (không bắt buộc email), có giới hạn đăng ký theo địa chỉ IP.
- **Chủ quản:** gửi yêu cầu làm chủ quản một địa điểm; quản trị viên xác minh ngoài hệ thống (gọi điện, giấy tờ) rồi duyệt. Hoặc quản trị viên tạo tài khoản và gán địa điểm.
- **Quên mật khẩu:** không có luồng tự động (đã bỏ OTP); quản trị viên đặt lại mật khẩu tạm và yêu cầu đổi ngay khi đăng nhập.
- Chính sách mật khẩu tối thiểu 8 ký tự (hiện app đang yêu cầu 6), khóa tạm sau nhiều lần sai, phiên hết hạn.

### 10.3 Mô hình dữ liệu bổ sung

| Bảng | Nội dung chính |
|---|---|
| `users` | id, tên đăng nhập, băm mật khẩu, tên hiển thị, `role` (user / owner / admin), trạng thái (hoạt động / khóa / buộc đổi mật khẩu), thời gian tạo, đăng nhập gần nhất |
| `place_owner` | user_id, poi_id, trạng thái (chờ duyệt / đã duyệt / từ chối), người duyệt, thời gian |
| `poi` (thêm cột) | `status` (hiển thị / ẩn / tạm đóng), `updated_by`, `updated_at`, `version`, xóa mềm |
| `poi_edit_request` | poi_id, người gửi, nội dung thay đổi (JSON), trạng thái, người duyệt, ghi chú, thời gian |
| `poi_media` | poi_id, tệp ảnh (đường dẫn, kích thước, trạng thái duyệt) |
| `announcement` | poi_id, tiêu đề, nội dung, thời gian hiệu lực |
| `review` | user_id, poi_id, điểm 1 đến 5, nhận xét, trạng thái kiểm duyệt, phản hồi của chủ quản |
| `report` | user_id, poi_id, loại báo sai, nội dung, trạng thái xử lý |
| `favorite`, `itinerary`, `itinerary_item` | địa điểm yêu thích và lộ trình đã lưu của người dùng |
| `poi_stats_daily` | poi_id, ngày, lượt xem, lượt đưa vào lộ trình |
| `audit_log` | ai, khi nào, làm gì, trên bản ghi nào, giá trị cũ và mới |

### 10.4 API theo vai trò (tiền tố `/v1`)

| Nhóm | Ví dụ điểm truy cập |
|---|---|
| Công khai | `GET /pois`, `GET /pois/{id}`, `GET /area`, `POST /route`, `GET /weather` |
| Người dùng | `GET/PUT /me`, `GET/POST/DELETE /favorites`, `GET/POST/PUT/DELETE /itineraries`, `POST /pois/{id}/reviews`, `POST /pois/{id}/reports` |
| Chủ quản | `GET /owner/places`, `PUT /owner/places/{id}` (trường được phép sửa), `POST /owner/places/{id}/edit-requests`, `POST /owner/places/{id}/media`, `POST /owner/places/{id}/announcements`, `GET /owner/places/{id}/stats`, `POST /owner/claims` |
| Quản trị | `GET/POST/PUT/DELETE /admin/pois`, `GET/PUT /admin/users`, `POST /admin/users/{id}/reset-password`, `GET /admin/edit-requests` kèm duyệt, `GET /admin/claims` kèm duyệt, `GET /admin/reviews` kèm kiểm duyệt, `GET /admin/reports`, `GET /admin/audit`, `GET /admin/stats` |

Bắt buộc: kiểm tra quyền sở hữu cho mọi thao tác `/owner/...`, ghi `audit_log` cho mọi thay đổi dữ liệu, giới hạn kích thước và loại tệp ảnh tải lên, đổi tên tệp tải lên (không dùng tên gốc), truy vấn có tham số.

### 10.5 Giao diện trong app (một app, hiển thị theo vai trò)
- Thanh tiêu đề hiện tên người dùng và nhãn vai trò; menu: hồ sơ, đổi mật khẩu, đăng xuất.
- **Người dùng:** panel "Lộ trình của tôi" (lưu, mở lại), "Yêu thích", nút đánh giá và báo sai trong thẻ địa điểm.
- **Chủ quản:** panel "Địa điểm của tôi" và hộp sửa thông tin (giờ mở, giá, SĐT, mô tả, ảnh, tạm đóng); chọn vị trí mới trên bản đồ để đề xuất đổi vị trí; xem thống kê lượt xem.
- **Quản trị:** cửa sổ "Quản trị" gồm các tab: Tổng quan (số liệu), Người dùng, Địa điểm (thêm bằng cách chấm trên bản đồ), Phê duyệt (đề xuất sửa, yêu cầu làm chủ quản), Đánh giá và báo sai, Nhật ký.
- Nút thuộc vai trò khác thì ẩn; thao tác bị từ chối ở server thì hiện thông báo rõ.

### 10.6 Các giai đoạn bổ sung (ước lượng thô thêm 3 đến 4 tuần)

| # | Giai đoạn | Nội dung | Nghiệm thu |
|---|---|---|---|
| R1 | Nền tảng vai trò (2 đến 3 ngày) | Bảng `users`, `place_owner`, `audit_log`; token mang vai trò; chốt kiểm tra quyền dùng chung; chính sách mật khẩu | Test tích hợp: người dùng gọi điểm truy cập quản trị bị từ chối; chủ quản sửa địa điểm không phải của mình bị từ chối |
| R2 | Tính năng người dùng (3 đến 4 ngày) | Yêu thích, lưu lộ trình, báo sai; panel "Lộ trình của tôi" | Lưu rồi mở lại lộ trình trên máy khác cho cùng kết quả |
| R3 | Chủ quản (5 đến 6 ngày) | Yêu cầu làm chủ quản; sửa trường được phép; đề xuất chờ duyệt; tải ảnh; thông báo; thống kê | Chủ quản sửa giờ mở, khách thấy ngay; đề xuất đổi vị trí chờ quản trị duyệt |
| R4 | Quản trị (6 đến 8 ngày) | Cửa sổ quản trị: người dùng, địa điểm, duyệt, nhật ký, tổng quan | Quản trị duyệt đề xuất và xem lại được ai đã đổi gì |
| R5 | Đánh giá và kiểm duyệt (3 đến 4 ngày) | Đánh giá 1 đến 5 sao, nhận xét, phản hồi của chủ quản, kiểm duyệt | Đánh giá chỉ hiện sau khi duyệt; mỗi người một đánh giá cho mỗi địa điểm |
| R6 | Thống kê (2 đến 3 ngày) | Ghi lượt xem, lượt vào lộ trình; biểu đồ cho chủ quản và quản trị | Số liệu khớp với thao tác thử |

Thứ tự đề xuất: R1, R2, R3, R4 (R5 và R6 sau, có thể cắt nếu thiếu thời gian). R1 làm ngay sau giai đoạn 4 (API).

### 10.7 Rủi ro riêng của ba vai trò

| Rủi ro | Cách giảm |
|---|---|
| Lộ tài khoản quản trị hoặc chủ quản qua HTTP | Dùng HTTPS; nếu vẫn HTTP thì ký phản hồi, token ngắn hạn, quản trị viên chỉ đăng nhập từ mạng tin cậy |
| Người lạ nhận mình là chủ quản | Quản trị xác minh ngoài hệ thống trước khi duyệt; ghi nhật ký |
| Chủ quản sửa sai hoặc phá thông tin | Trường nhạy cảm cần duyệt; `audit_log` lưu giá trị cũ để hoàn tác |
| Đánh giá giả, nội dung xấu | Kiểm duyệt trước khi hiển thị, mỗi người một đánh giá, giới hạn tần suất |
| Tải ảnh độc hại hoặc quá lớn | Giới hạn loại tệp (JPG, PNG), kích thước và số lượng; đổi tên; nén lại ở server |
| Mất quản trị viên duy nhất | Có ít nhất hai tài khoản quản trị; có cách tạo quản trị bằng dòng lệnh trên VPS |
| Dữ liệu cá nhân của người dùng (tên đăng nhập, vị trí, lộ trình) | Thu thập ít nhất có thể, ghi rõ trong chính sách; xóa tài khoản khi được yêu cầu |

### 10.8 Cần chốt trước khi làm
1. Chủ quản sửa thông tin thường (giờ mở, giá, SĐT, mô tả) có hiện ngay không, hay cũng phải chờ quản trị duyệt? (Đề xuất: hiện ngay, có nhật ký và hoàn tác; đổi tên, loại hình, vị trí thì chờ duyệt.)
2. Chủ quản có tài khoản bằng cách nào: tự gửi yêu cầu rồi quản trị duyệt, hay quản trị tạo sẵn? (Đề xuất: cả hai.)
3. Một địa điểm có nhiều chủ quản không? Một chủ quản có nhiều địa điểm không? (Đề xuất: cả hai đều có thể.)
4. Có làm đánh giá và nhận xét của người dùng không (R5)? Có cho chủ quản tải ảnh không?
5. Có bao nhiêu quản trị viên, và ai tạo quản trị viên đầu tiên?
6. Chốt HTTPS (mục 2): cần thiết hơn nhiều khi có quản trị và chủ quản.
