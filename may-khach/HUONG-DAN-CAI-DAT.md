# Hướng dẫn cài đặt MÁY KHÁCH (Windows)

Có hai nhóm người đọc: **A. người dùng** chỉ cài và dùng ứng dụng; **B. người làm bộ cài** dựng bộ cài từ mã nguồn.
Máy chủ cài riêng, xem [../may-chu/HUONG-DAN-CAI-DAT.md](../may-chu/HUONG-DAN-CAI-DAT.md).

---

## A. Dành cho người dùng

### A1. Yêu cầu
| Thứ | Yêu cầu |
|---|---|
| Hệ điều hành | Windows 10 hoặc 11 (64-bit; ứng dụng là 32-bit, vẫn chạy bình thường) |
| .NET Framework | 4.7.2 trở lên (Windows 10/11 bản cập nhật gần đây đã có sẵn) |
| Quyền | Quyền **quản trị** lúc cài (để đăng ký thành phần bản đồ) |
| Mạng | Cần để đăng nhập, **tìm đường** và **tải dữ liệu bản đồ lần đầu** (khoảng 33 MB, chỉ một lần); sau khi đã tải, xem bản đồ và địa điểm không cần mạng |
| Màn hình | Từ 1280 × 720; hỗ trợ màn hình độ phân giải cao |

### A2. Cài đặt
1. Chạy `HueTravel-Setup-<phiên bản>.exe`.
2. Nếu Windows hiện cảnh báo "Windows protected your PC" (bộ cài chưa ký số): chọn **More info** → **Run anyway**.
3. Chọn thư mục cài (mặc định `C:\Program Files\HueTravel`).
4. Trang **"Máy chủ dữ liệu"**: nhập địa chỉ máy chủ do quản trị viên cung cấp, ví dụ `http://203.0.113.10`.
   - Ô *Khóa công khai* chỉ điền nếu quản trị viên đưa cho bạn (dùng để phát hiện dữ liệu bị sửa giữa đường). Để trống nếu không có.
   - Để trống ô địa chỉ nếu chưa có máy chủ: ứng dụng vẫn xem được bản đồ, nhưng không đăng nhập và không tìm được đường.
5. Chọn tạo biểu tượng ngoài màn hình nếu muốn, bấm **Install**, rồi **Finish**.

Bộ cài đã gồm ứng dụng, thành phần bản đồ **MapWinGIS** (tự đăng ký) và dữ liệu bản đồ; không cần cài gì thêm.

### A3. Lần đầu mở ứng dụng
1. Màn hình **đăng nhập** có ba lựa chọn: **Đăng nhập**, **Đăng ký**, **Dùng thử không cần tài khoản**. Chưa cấu hình máy chủ
   thì chỉ có nút **Tiếp tục ngoại tuyến**.
2. Bản đồ hiện sau vài giây. Thanh bên trái: Khám phá, Lộ trình, Hướng dẫn, Hồ sơ (khách thấy **Đăng nhập** ở mục cuối).
3. **Khách** (không cần tài khoản) xem bản đồ, tìm kiếm, lọc nhóm, mở thẻ địa điểm, đọc đánh giá và thông báo, xem thời tiết,
   đo khoảng cách/diện tích, lập lộ trình tới 12 điểm, xem tuyến và chạy xe mô phỏng.
4. Các việc gắn với tài khoản vẫn hiện nút bình thường: **Yêu thích**, **Viết đánh giá**, **Báo sai**, **Nhận quản lý**, mục
   **Hồ sơ**. Khách bấm vào thì form đăng nhập/đăng ký mở ngay trên bản đồ; đăng nhập xong ứng dụng làm tiếp đúng việc vừa
   bấm, không mất địa điểm đang xem hay lộ trình đang dựng. Bấm **Quay lại bản đồ** (hoặc Esc) để tiếp tục là khách.
5. **Lộ trình**: chọn điểm bắt đầu (bấm *Gợi ý điểm bắt đầu* hoặc *Chọn trên bản đồ*), thêm các điểm đến, bấm **Tìm đường**.
   Kết quả hiện ở khung giữa phía dưới bản đồ; xe chạy theo tuyến. Bấm **Dừng lại** để xóa kết quả.
6. **Giờ mở cửa**: thẻ địa điểm và danh sách có chip *Đang mở*, *Sắp đóng*, *Đã đóng* (chữ gốc vẫn hiện đầy đủ). Nút lọc
   **Đang mở cửa** trên thanh nhóm giữ cả địa điểm chưa rõ giờ. Trong lộ trình, chọn **Giờ khởi hành** (mặc định *Bây giờ*,
   theo giờ Việt Nam) và bấm chip **Lưu …′** ở từng điểm để đổi thời gian lưu lại; kết quả hiện giờ đến/giờ rời và cảnh báo
   nếu đến lúc địa điểm đã đóng.
7. **Xe mô phỏng** chạy đúng thời gian ước tính của máy chủ; nút **1× 2× 4× 8×** đổi tốc độ, tốc độ được nhớ cho lần sau.
   Kéo thanh mảnh phía trên hàng nút để tua tới bất kỳ thời điểm nào; khi bảng kết quả đang được chọn, phím **Space** tạm
   dừng/chạy, **+ / -** đổi tốc độ, **← / →** tua lùi/tới 30 giây.

### A4. Đổi địa chỉ máy chủ sau khi cài
Có ba cách, ứng dụng lấy theo thứ tự này (cách trên thắng cách dưới):
1. Biến môi trường `HUE_API_URL`.
2. Tệp `C:\ProgramData\HueTravel\server.txt` (một dòng, ví dụ `http://203.0.113.10`) — **cách nên dùng**: mở bằng Notepad (chạy với quyền quản trị), sửa, lưu, mở lại ứng dụng.
3. Khóa `ApiBaseUrl` trong tệp `Hue_travel.exe.config` cạnh ứng dụng.

Khóa công khai nằm ở `C:\ProgramData\HueTravel\public.key` (hoặc `ApiPublicKey` trong `.exe.config`, hoặc biến `HUE_API_PUBLIC_KEY`).

### A5. Gỡ cài đặt
*Cài đặt → Ứng dụng* → **Hue Travel** → Gỡ cài đặt (tự hủy đăng ký MapWinGIS). Dữ liệu người dùng còn lại ở
`C:\ProgramData\HueTravel` và `%LOCALAPPDATA%\HueTravel` (biểu tượng đã tạo, phiên đăng nhập); xóa tay nếu muốn sạch hoàn toàn.

### A6. Sự cố thường gặp
| Triệu chứng | Cách xử lý |
|---|---|
| Bộ cài báo cần .NET Framework 4.7.2 | Cập nhật Windows hoặc cài .NET Framework 4.7.2 từ trang Microsoft |
| Mở ứng dụng thấy khung bản đồ trống / báo lỗi `MapWinGIS` | Cài lại bằng quyền quản trị; hoặc trong cửa sổ quản trị chạy `%SystemRoot%\SysWOW64\regsvr32.exe "C:\Program Files\HueTravel\MapWinGIS\MapWinGIS.ocx"` |
| Báo thiếu `MSVCP140.dll`/`VCRUNTIME140.dll` | Cài *Microsoft Visual C++ Redistributable x86* (bộ cài làm sẵn nếu có tệp `vc_redist.x86.exe`) |
| "Cần kết nối máy chủ để tìm đường" | Chưa có địa chỉ máy chủ (A4) hoặc không có mạng; kiểm tra mở `http://<địa chỉ>/healthz` bằng trình duyệt |
| Đăng nhập báo không kết nối được | Sai địa chỉ máy chủ, máy chủ tắt, hoặc tường lửa/mạng chặn cổng 80 |
| "Phản hồi máy chủ không hợp lệ" | Khóa công khai trong ứng dụng không khớp với máy chủ; nhờ quản trị viên gửi lại khóa đúng |
| Bản đồ chậm lúc mở | Lần đầu nạp dữ liệu nền (khoảng 3–5 giây); mở lại sau đó nhanh hơn |
| Chữ nhòe hoặc quá nhỏ trên màn hình độ phân giải cao | Chuột phải `Hue_travel.exe` → Properties → Compatibility → *Change high DPI settings* → chọn *Application* |

---

## B. Dành cho người làm bộ cài

### B1. Máy dựng cần
| Thứ | Ghi chú |
|---|---|
| Visual Studio 2019/2022 (hoặc Build Tools) | Có khối lượng công việc *.NET desktop development* |
| **MapWinGIS** đã cài | Mặc định `C:\Program Files\MapWinGIS` (thư mục chứa `MapWinGIS.ocx`) |
| **Inno Setup 6** | https://jrsoftware.org/isinfo.php ; không có thì script chỉ tạo bản `.zip` mang đi |
| (Khuyên dùng) `bo-cai/redist/vc_redist.x86.exe` | Tải *Microsoft Visual C++ Redistributable x86* từ Microsoft; bộ cài sẽ chạy nó |

### B2. Dựng bộ cài
Từ gốc kho mã (sau `git clone`):
```powershell
powershell -ExecutionPolicy Bypass -File may-khach/dong-goi.ps1 -Version 1.0.0 -Server http://203.0.113.10
```
- `-Server`: địa chỉ điền sẵn trong trang "Máy chủ dữ liệu" (người cài vẫn sửa được). Bỏ qua nếu chưa có máy chủ.
- `-PublicKey`: khóa công khai điền sẵn (không bắt buộc).
- `-MapWinGisDir`: nếu MapWinGIS nằm chỗ khác.

Kết quả: `may-khach/dist/HueTravel-Setup-1.0.0.exe`. Script dừng với thông báo rõ nếu thiếu MSBuild, MapWinGIS hoặc nếu bản dựng lọt vào
`NutMangGiaoThong` (lớp chỉ dành cho máy chủ).

Cách thủ công (nếu không dùng script): `msbuild ung-dung/Hue_travel.csproj /restore /p:Configuration=Release`, rồi
`ISCC.exe bo-cai/HueTravel.iss /DAppVersion=1.0.0` (xem `bo-cai/README.md`).

### B3. Thử trên máy sạch
Dùng Windows Sandbox hoặc máy ảo Windows 10/11 **chưa cài MapWinGIS**:
1. Cài bằng bộ cài, nhập địa chỉ máy chủ thật.
2. Mở ứng dụng: phải thấy bản đồ (nền be, sông, đường) và điểm địa điểm.
3. Đăng nhập, tạo lộ trình 2–3 điểm, bấm Tìm đường: phải ra tuyến đường bộ (không phải đường thẳng) và xe chạy.
4. Tắt máy chủ rồi bấm Tìm đường: phải báo "Cần kết nối máy chủ…".

### B4. Phát hành phiên bản mới
1. Tăng số phiên bản (`-Version`), dựng lại bộ cài.
2. Nếu phiên bản cũ không còn tương thích với máy chủ, nâng `MIN_CLIENT_VERSION` trong `may-chu/.env` trên máy chủ để máy cũ được nhắc cập nhật.
3. Khi đổi **dữ liệu bản đồ** (nền, địa điểm, đường) **không cần** phát hành lại bộ cài: chỉ cần đưa dữ liệu mới vào `may-chu/du-lieu` và chạy
   `may-chu/scripts/deploy.sh import` (hoặc `deploy.sh pack`) trên máy chủ. Lần mở app sau, máy khách thấy có bản mới và hỏi người dùng có tải về không.
4. Bộ cài chưa ký số nên Windows SmartScreen cảnh báo; ký mã (code signing) khi có điều kiện.

### B5. Tệp ứng dụng ghi trên máy người dùng
| Đường dẫn | Nội dung |
|---|---|
| `C:\ProgramData\HueTravel\server.txt`, `public.key` | Địa chỉ máy chủ và khóa công khai (do bộ cài ghi) |
| `%LOCALAPPDATA%\HueTravel\markers\` | Biểu tượng địa điểm do ứng dụng tự vẽ (xóa được, tự tạo lại) |
| Kho phiên đăng nhập của người dùng Windows | Mã làm mới đăng nhập, được bảo vệ theo tài khoản Windows |
