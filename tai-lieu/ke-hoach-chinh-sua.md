# Kế hoạch chỉnh sửa chi tiết

Bốn yêu cầu: (1) chọn đăng nhập / đăng ký / chế độ khách khi mở app, (2) bỏ hẳn tile Google, (3) thêm giờ mở cửa,
(4) xe mô phỏng chạy đúng thời gian ước tính, có tua x2/x4/x8.

Thứ tự làm đề xuất: **2 → 1 → 4 → 3** (từ ít rủi ro đến nhiều; mục 4 là tiền đề cho mục 3 vì cả hai cùng dùng "đồng hồ chuyến đi").
Mỗi mục làm xong thì build, chạy `may-khach/kiem-thu/test_services.ps1` rồi commit riêng.

Tên file/hàm dưới đây lấy từ mã hiện tại; số dòng chỉ để định vị, có thể đã lệch.

---

## Mục 2. Bỏ hẳn tile Google (làm trước, nhanh nhất)

Hiện trạng: `UseLocalData = true` đã tắt tile Google, nhưng địa chỉ `mt1.google.com` vẫn nằm trong mã, máy chủ vẫn gửi
`TileUrl`/`TileAttribution` qua `/v1/config`, và cả một nhánh mã vẽ tile web vẫn còn. "Bỏ hẳn" nghĩa là xóa toàn bộ nhánh đó.

### Máy khách (`may-khach/ung-dung`)

1. `MapEngine/BaseMapCatalog.cs`: xóa 4 dòng `new BaseMapInfo(... mt1.google.com ...)`, hằng `GoogleCredit`, `UseLocalData`,
   `FirstProviderId`, `ApplyOverrides`, `Find`. Nếu không còn gì dùng thì **xóa cả file**. `DefaultId`, `MinZoom`, `MaxZoom`:
   kiểm tra `MapHost.cs` còn dùng không rồi chuyển giá trị cần thiết sang nơi dùng.
2. `MapEngine/MapHost.cs`:
   - dòng 30: bỏ trường `baseMapId`.
   - dòng ~526-560: xóa các hàm tile (`ApplyTiles`, nhánh `if (BaseMapCatalog.UseLocalData) return;`) và hàm đổi lớp nền theo `mapId`.
   - dòng 592 và 616-617: giữ nhánh `ApplyLocalBase()` / `LocalBaseMap.ApplyScale`, bỏ điều kiện `UseLocalData` (luôn đúng) và nhánh `else ApplyTiles`.
   - `ReloadBaseMap()`: chỉ còn nạp lại bản đồ cục bộ (dùng khi tải xong gói mới).
3. `Shell/MainForm.cs` dòng ~450: xóa dòng `BaseMapCatalog.ApplyOverrides(config.TileUrls, ...)`. Giữ phần đọc `MinimumVersion` nếu có.
4. `Services/Online/ApiConfigSource.cs`: xóa `TileUrls`, `TileAttribution` và đoạn đọc chúng; giữ `MinimumVersion`.
5. `Panels/Layers/LayerPanel.cs`: tìm "Chọn lớp nền" / bộ chuyển lớp (roadmap, satellite, hybrid, terrain). Nếu có, xóa bộ chọn
   này (bản đồ nền cục bộ chỉ có một kiểu). Chỉ giữ các công tắc lớp địa điểm.
6. `Resources/LICENSES.md` dòng 9: bỏ câu "Google tile addresses remain ...".
7. `kiem-thu/test_services.ps1`: xóa khối kiểm tra `BaseMapCatalog` (dòng ~89-100) và khối tile (dòng ~481-501), kể cả 3 test
   về `TileUrls`/`TileAttribution` của `ApiConfigSource`. Giữ test `MinimumVersion` nếu có.

### Máy chủ (`may-chu`)

1. `api/appsettings.json`: xóa `TileUrl`, `TileAttribution` trong mục `Client`.
2. `api/Options/ApiOptions.cs` dòng 35-39: xóa `TileUrl`, `TileAttribution`, `TileUrls` khỏi `ClientSettings`.
3. `api/Meta/MetaEndpoints.cs` dòng 8 và 26: bỏ 3 trường đó khỏi `ClientConfigDto` và lời gọi tạo nó. Sửa test trong `kiem-thu/` nếu có kiểm tra các trường này (`grep -rn "TileUrl" may-chu/kiem-thu`).
4. `docker-compose.yml` dòng 36-41: xóa 5 dòng `Client__TileAttribution` và `Client__TileUrls__*`.
5. `.env.example` dòng 15-20: xóa khối "Base map tiles".
6. Tài liệu: `VAN-HANH.md` (mục "Bản đồ nền (nhà cung cấp tile hợp lệ)"), `tai-lieu/thu-vien-su-dung.md` dòng ~135,
   `HUONG-DAN-CAI-DAT.md` nếu nhắc tile.

Lưu ý khi xóa: máy khách cũ (đã cài) vẫn đọc `/v1/config` có thể gặp thiếu trường; `JsonHelper.Str` trả null nên không lỗi, nhưng hãy thử.

**Nghiệm thu**: `grep -rni "google\|mt1\|tile" may-khach may-chu tai-lieu` chỉ còn lại những chỗ không liên quan (ví dụ "Google" trong LICENSES của font Material thì giữ vì đó là giấy phép biểu tượng, không phải bản đồ). Mở app: bản đồ vẫn vẽ, không có kết nối tới `google.com` (kiểm bằng Resource Monitor/Fiddler).

---

## Mục 1. Chọn đăng nhập / đăng ký / khách khi mở app

### Nguyên tắc: khách dùng như Google Maps khi chưa đăng nhập

Chế độ khách chỉ làm được những việc **không cần quyền người dùng** (xem, tìm, dẫn đường). Việc nào gắn với một tài khoản
(lưu, viết, báo, quản lý) thì **vẫn hiện nút như bình thường**; bấm vào thì mở **form đăng nhập / đăng ký** ngay trên bản đồ,
đăng nhập xong thì **làm tiếp đúng việc vừa bấm**, không mất vị trí bản đồ, địa điểm đang xem hay lộ trình đang dựng.

| Trạng thái | Có máy chủ | Có tài khoản | Dùng được |
|---|---|---|---|
| Đã đăng nhập | có | có | tất cả theo vai trò |
| **Khách** (mới) | có | không | các chức năng ở bảng "Khách làm được" dưới đây; còn lại phải đăng nhập |
| Ngoại tuyến (đã có) | không | không | xem bản đồ và địa điểm đã tải; việc cần tài khoản báo "Cần kết nối máy chủ" |

#### Khách làm được (không cần đăng nhập)

Máy chủ đã công khai các API này (`Program.cs`: chỉ nhóm có `.RequireAuthorization()` mới cần token), nên **không cần sửa máy chủ**.

| Chức năng | API |
|---|---|
| Xem bản đồ, phóng to/thu nhỏ, xoay, la bàn, đo khoảng cách và diện tích | cục bộ |
| Tìm kiếm, lọc theo nhóm, danh sách địa điểm quanh khu vực | `GET /v1/pois`, `/v1/area` |
| Xem thẻ địa điểm: ảnh đã duyệt, giờ mở cửa, giá vé, điện thoại, trang web | `GET /v1/pois/{id}`, `/photos` |
| Đọc đánh giá và thông báo của địa điểm | `GET /v1/pois/{id}/reviews`, `/announcements` |
| Thời tiết | `GET /v1/weather` |
| Lập lộ trình (tới 12 điểm), xem tuyến, chạy mô phỏng xe, tua nhanh, in/xuất ra tệp | `POST /v1/route` (khách được 30 lần/phút theo IP) |

#### Phải đăng nhập hoặc đăng ký (khách bấm vào thì chuyển đến form)

| Chức năng | Nơi bấm trong app | Lời nhắc trên form |
|---|---|---|
| Yêu thích / Bỏ thích | nút "Yêu thích" trên thẻ địa điểm | "Đăng nhập để lưu địa điểm yêu thích" |
| Viết, sửa, xóa đánh giá | nút trong cửa sổ đánh giá | "Đăng nhập để viết đánh giá" |
| Báo sai thông tin | nút "Báo sai" trên thẻ | "Đăng nhập để báo thông tin chưa đúng" |
| Nhận quản lý địa điểm | nút "Nhận quản lý" trên thẻ | "Đăng nhập hoặc đăng ký tài khoản chủ quản" |
| Hồ sơ, đổi tên, đổi mật khẩu, lộ trình đã lưu, đánh giá của tôi | mục "Hồ sơ" ở thanh bên trái | "Đăng nhập hoặc đăng ký để xem hồ sơ" |
| Bảng quản trị, quản lý địa điểm và ảnh của chủ quản | chỉ có trong Hồ sơ, nên khách không thấy | (không áp dụng) |

Lưu ý: tất cả đều đã bị **máy chủ** chặn bằng 401 khi không có token (nhóm `/v1` với `.RequireAuthorization()` trong `SocialEndpoints.cs`,
`OwnerEndpoints.cs`, `AdminEndpoints.cs`). Phần chặn ở máy khách chỉ để trải nghiệm tốt; **không được coi là bảo mật**.

### Hai luồng đăng nhập khác nhau, đừng nhập làm một

1. **Khi mở app** (đã có `LoginForm` toàn màn hình): ba lựa chọn Đăng nhập / Đăng ký / **Dùng thử không cần tài khoản**.
2. **Giữa chừng trong app** (mới): khách bấm một chức năng cần quyền → cùng `LoginForm` hiện dạng hộp thoại (`ShowDialog(this)`) **phủ lên cửa sổ chính, không đóng cửa sổ chính**.
   Nút khách bị ẩn, thay bằng "Quay lại bản đồ" (hoặc Esc) để đóng form và tiếp tục là khách.

Khác với cách cũ (`ReturnToSignIn()` đóng `MainForm` rồi mở lại): nếu dùng cách đó thì khách mất bản đồ, địa điểm đang xem và lộ trình đang dựng.

### Các bước

1. **`Services/Online/AppSession.cs`**: thêm `public bool IsGuest { get; set; }`.
   (`Auth.CurrentUser == null` đã là "chưa đăng nhập"; `IsGuest` chỉ cho biết người dùng đã chủ động chọn khách, để không bị coi là "phiên hết hạn".)
2. **`Program.cs`**: trong vòng `while (true)`, trước `new LoginForm(session)` đặt `session.IsGuest = false;`. Màn hình đầu vẫn trả `DialogResult.OK` cho cả đăng nhập lẫn khách.
3. **`Welcome/LoginForm.cs`** — màn hình đầu:
   - `enum Mode { Login, Register, Offline }` giữ nguyên (Offline = không có máy chủ).
   - Thêm nút `btnGuest` (`IconButton`, `StyleButton(btnGuest, false)`) chữ **"Dùng thử không cần tài khoản"**; chỉ hiện khi `session.IsOnline`. Đặt dưới `btnSecondary` trong `ApplyModeLayout()` (dòng ~358-415) và ẩn/hiện trong `SetMode()` (dòng ~419-460).
   - Handler: `btnGuest.Click += (s, e) => { session.IsGuest = true; DialogResult = DialogResult.OK; };`.
   - `OnShown` (dòng ~485) vẫn tự đăng nhập bằng phiên đã ghi nhớ (`RestoreAsync`). Khách không ghi nhớ gì: lần sau mở app vẫn hiện lại 3 lựa chọn.
4. **`Welcome/LoginForm.cs`** — dạng hộp thoại trong app:
   - Thêm constructor `LoginForm(AppSession session, string reason)` (hoặc tham số tùy chọn). Khi có `reason`:
     (a) không hiện `btnGuest`;
     (b) hiện `reason` bằng `ShowMessage(reason, false)` ngay khi mở (kiểu thông tin, không phải lỗi);
     (c) **bỏ qua `RestoreAsync` trong `OnShown`** (khách đã không có phiên để khôi phục);
     (d) đổi nhãn nút đóng thành "Quay lại bản đồ"; `btnClose`/Esc trả `DialogResult.Cancel` (dòng ~204, ~580), nhưng **không** thoát app;
     (e) vẫn cho chuyển qua lại giữa Đăng nhập và Đăng ký (đã có `btnSecondary`), kể cả đăng ký tài khoản chủ quản.
   - Form hiện toàn màn hình, không viền: khi mở bằng `ShowDialog(this)` nó phủ cửa sổ chính; chấp nhận, hoặc đặt `WindowState = Normal` và `StartPosition = CenterParent` với kích thước nhỏ hơn nếu muốn trông giống hộp thoại.
5. **`Shell/MainForm.cs`** — một hàm cổng duy nhất, mọi chỗ gọi đều dùng nó:
   ```csharp
   // Chạy action ngay nếu đã đăng nhập; nếu là khách thì mở form đăng nhập/đăng ký, xong thì chạy action.
   private void RunSignedIn(string reason, Action action)
   {
       if (auth != null && auth.CurrentUser != null) { action(); return; }
       if (api == null) { toast.ShowMessage("Cần kết nối máy chủ để đăng nhập.", ToastKind.Info); return; }   // ngoại tuyến
       using (var form = new LoginForm(session, reason))
       {
           if (form.ShowDialog(this) != DialogResult.OK || auth.CurrentUser == null) return;   // khách bấm "Quay lại bản đồ"
       }
       session.IsGuest = false;
       action();
   }
   ```
   Sau đăng nhập thành công phải nạp lại trạng thái của người dùng (làm trong `UpdateAccountUi()` vì `auth.Changed` đã bắn):
   - `Safe(LoadFavoritesAsync())` để biết địa điểm nào đã thích;
   - nếu `auth.MustChangePassword` (mật khẩu tạm) thì `ShowSidePanel(NavRail.Profile)` như dòng 81;
   - thanh trạng thái / thanh bên đổi nhãn "Khách" thành tên người dùng.
   (Kiểm tra `ToastKind` có giá trị `Info` không; nếu không thì dùng giá trị tương đương đang có.)
6. **`Shell/MainForm.cs` `UpdateAccountUi()`** (dòng ~345): hiện khi `user == null` thì gọi `ReturnToSignIn("Phiên đăng nhập đã hết hạn...")`. Sửa:
   ```csharp
   if (user == null)
   {
       favoriteIds.Clear();
       if (!session.IsGuest && !accountPanel.IsLeaving) ReturnToSignIn("Phiên đăng nhập đã hết hạn. Hãy đăng nhập lại.");
   }
   ```
   Đây là chỗ dễ sót nhất: nếu quên thì khách vừa vào đã bị đá ra màn hình đầu. Đăng xuất chủ động (`accountPanel.LoggedOut`) vẫn đưa về màn hình đầu như cũ.
7. **Nối các chức năng cần quyền vào `RunSignedIn`** (từng chỗ một):
   - **Thẻ địa điểm** `Panels/PoiIdentify/PoiIdentifyCard.cs`, `RefreshAccountButtons()` (dòng ~240): hiện đang ẩn các nút Yêu thích, Nhận quản lý, Báo sai khi chưa đăng nhập (`visible = signedIn && shown != null`). Bỏ điều kiện `signedIn` để **khách vẫn thấy** (`visible = shown != null`); phần đổi chữ "Bỏ thích" chỉ áp dụng khi đã đăng nhập. Rồi ở `MainForm.SetUpAccount()` (dòng ~319-342) bọc các handler:
     ```csharp
     poiIdentifyCard.FavoriteToggled += (s, poi) => RunSignedIn("Đăng nhập để lưu địa điểm yêu thích", () => Safe(ToggleFavoriteAsync(poi)));
     poiIdentifyCard.ClaimRequested  += (s, poi) => RunSignedIn("Đăng nhập hoặc đăng ký tài khoản chủ quản", () => Safe(ClaimAsync(poi)));
     poiIdentifyCard.ReportRequested += (s, poi) => RunSignedIn("Đăng nhập để báo thông tin chưa đúng", () => Safe(ReportAsync(poi)));
     ```
   - **Đánh giá** (`Account/ReviewsDialog.cs`, gọi ở `MainForm` dòng ~329 và ~330 với cờ `auth.CurrentUser != null`): hiện nút "Viết đánh giá" cho cả khách. Khi khách bấm, hộp thoại đóng lại kèm kết quả "cần đăng nhập" (thêm sự kiện `SignInRequested` hoặc `DialogResult.Retry`); `MainForm` gọi `RunSignedIn("Đăng nhập để viết đánh giá", () => ReviewsDialog.ShowFor(this, api, poi, true))` để mở lại hộp thoại đánh giá ở chế độ ghi. Đọc `ReviewsDialog.cs` trước khi sửa vì file này chưa được xem khi lập kế hoạch.
   - **Hồ sơ** (`ShowSidePanel`, dòng ~242-256, và `navRail.ItemClicked` dòng ~116): nếu `index == NavRail.Profile` mà chưa đăng nhập, không mở `accountPanel`:
     ```csharp
     if (index == NavRail.Profile && auth != null && auth.CurrentUser == null)
     { RunSignedIn("Đăng nhập hoặc đăng ký để xem hồ sơ", () => ShowSidePanel(NavRail.Profile)); return; }
     ```
     Và đổi nhãn/biểu tượng mục này trong `Shell/NavRail.cs` thành **"Đăng nhập"** khi là khách (đổi lại thành "Hồ sơ" sau khi đăng nhập).
   - **Lộ trình**: `accountPanel.SetRouteSource(...)` chỉ dùng trong Hồ sơ nên đã nằm sau cổng ở trên. Mở `TripResultPanel.btnSave` (dòng ~231, 281) xem nút "Lưu" ghi **ra tệp** hay **lên tài khoản**: ra tệp/in thì khách dùng tự do; lên tài khoản thì bọc `RunSignedIn("Đăng nhập để lưu lộ trình", ...)`.
8. **Lưới an toàn cho 401** (`Services/Online/ApiClient.cs`, dòng ~99-125): xác nhận một lệnh gọi cần quyền mà không có token (trả 401) **không** phát `SessionExpired` (chỉ phát khi có refresh token mà làm mới thất bại; đọc kỹ rồi thử). Nếu muốn chắc, `ApiClient` phát thêm sự kiện `SignInRequired` và `MainForm` chuyển nó thành `RunSignedIn("Hãy đăng nhập để tiếp tục", () => { })`.
9. **Nhãn trạng thái**: `StatusBar`/`TitleBar` hiện "Khách" khi `IsGuest`, tên người dùng khi đã đăng nhập.
10. Tài liệu: `may-khach/HUONG-DAN-CAI-DAT.md` và `README.md` (đoạn "Địa chỉ máy chủ ... chạy ngoại tuyến") thêm mục chế độ khách và bảng hai nhóm chức năng ở trên.

### Thử

- Mở app có máy chủ → thấy 3 lựa chọn. Chọn khách → vào bản đồ; tìm kiếm, xem thẻ, đọc đánh giá, lập lộ trình, chạy xe, thời tiết đều dùng được và **không** bị đưa ra màn hình đầu.
- Khách bấm **Yêu thích** → hiện form với dòng "Đăng nhập để lưu địa điểm yêu thích" phủ lên bản đồ. Đăng nhập thành công → quay lại **đúng địa điểm đó**, địa điểm đã được thêm vào yêu thích, nút đổi thành "Bỏ thích".
- Làm tương tự với **Báo sai**, **Nhận quản lý**, **Viết đánh giá**, mục **Hồ sơ**.
- Trong form giữa chừng, chuyển sang **Đăng ký**, tạo tài khoản mới → cũng quay lại và làm tiếp việc đang dở.
- Bấm "Quay lại bản đồ" hoặc Esc → vẫn là khách, bản đồ và lộ trình đang dựng còn nguyên.
- Giữa lúc dựng lộ trình 5 điểm, khách bấm Yêu thích rồi đăng nhập: lộ trình 5 điểm vẫn còn.
- Không có máy chủ (ngoại tuyến) → bấm các chức năng cần quyền chỉ hiện thông báo "Cần kết nối máy chủ", không mở form.
- Đăng nhập, ghi nhớ, đóng mở lại → vào thẳng; đăng xuất → về màn hình đầu.
- Kiểm tra bằng curl: không token mà gọi `/v1/favorites`, `POST /v1/pois/x/reviews`, `/v1/owner/claims` → 401 (máy chủ chặn dù máy khách có bị sửa).
- Thêm test vào `test_services.ps1`: `AppSession.IsGuest` mặc định `false`.

---

## Mục 4. Xe chạy đúng thời gian ước tính, tua x2/x4/x8

Làm trước mục 3 vì cả hai cần "đồng hồ chuyến đi" (giờ mô phỏng).

### Hiện trạng (`MapEngine/RouteDrawing.cs`)

Mỗi điểm của đường đã có thời gian thực `elapsed[i]` (giây tính từ đầu chuyến, lấy từ `leg.Times` của máy chủ) và `DistanceAt(seconds)` nội suy ra vị trí. Nhưng `BuildLine()` dòng 155-157 ép cả chuyến vào tối đa 26 giây:

```csharp
double shown = Math.Max(1, Math.Min(TripSeconds, total / MinSpeedMetresPerSecond));
clockScale = totalElapsed / shown;
```

Nghĩa là 20 phút bị nén thành ≤ 26 giây. Việc cần làm chính là thay hệ số nén này bằng hệ số tốc độ do người dùng chọn. Máy chủ **không cần sửa** (đã gửi đủ `Times`).

### Các bước

1. **`RouteDrawing.cs`**:
   - Xóa hằng `MinSpeedMetresPerSecond` và `TripSeconds`, và hai dòng tính `shown`/`clockScale` trong `BuildLine()`.
   - Thêm:
     ```csharp
     public static readonly int[] SpeedSteps = { 1, 2, 4, 8 };
     private int speed = 1;
     public int Speed { get { return speed; } set { speed = Array.IndexOf(SpeedSteps, value) >= 0 ? value : 1; SpeedChanged?.Invoke(this, EventArgs.Empty); } }
     public event EventHandler SpeedChanged;
     public double TripClockSeconds { get { return tripClock; } }   // thời gian mô phỏng đã chạy
     public double TotalTripSeconds { get { return totalElapsed; } }
     ```
   - Trong `Step()` (dòng ~351): đổi `tripClock + clockScale * dt` thành `tripClock + speed * dt`. Bỏ trường `clockScale`.
   - Dừng ở điểm (`DwellSeconds = 0.9`): thời gian dừng hiện là giây **thật** nên khi x8 vẫn 0,9 giây là ổn về hình ảnh. Giữ, nhưng chia theo tốc độ nếu thấy lâu: `dwellLeft = DwellSeconds / Math.Min(speed, 4)`.
   - Giữ giới hạn `dt = Math.Min(0.1, ...)` (đúng rồi): nhân với `speed` sau đó nên ở x8 vẫn mượt, còn cửa sổ bị treo không làm xe nhảy.
   - Tối ưu vẽ: ở x1, quãng đi mỗi khung (33 ms) chỉ khoảng 0,2-0,3 m, nhỏ hơn một điểm ảnh khi thu nhỏ bản đồ nên vẽ lại mỗi khung là lãng phí. Trong `Step()` chỉ gọi `DrawCar()` khi `travelled` đổi ≥ `0.5 * MetresPerPixel()` kể từ lần vẽ trước (nhớ `lastDrawnTravelled`), hoặc khi mức thu phóng đổi (đã có điều kiện `mpp`).
   - Thêm sự kiện tiến độ cho giao diện: `public event EventHandler Progress;` gọi tối đa 4 lần/giây trong `Step()` khi `playing` (đếm khung, 8 khung một lần).
2. **`MapEngine/MapHost.cs`** (dòng ~439-472, nơi có `PlayTrip/PauseTrip/ReplayTrip`): thêm
   `public int TripSpeed { get { return routeDrawing?.Speed ?? 1; } set { if (routeDrawing != null) routeDrawing.Speed = value; } }`,
   `public double TripClockSeconds`, `public double TripTotalSeconds`, và chuyển tiếp sự kiện `TripSpeedChanged`, `TripProgress`.
   Khi `Show(trip, play)` tạo chuyến mới nhớ lại tốc độ cũ (đặt trong `MapHost`, không để mất khi vẽ chuyến mới).
3. **`Panels/RouteOptimizer/TripResultPanel.cs`** (các nút ở dòng ~231-290, bố cục ~395-410):
   - Thêm 4 nút nhỏ **1× 2× 4× 8×** (nhóm chọn một, nút đang chọn tô màu thương hiệu) bên cạnh `btnPlay`/`btnReplay`. Tạo giống `btnReplay` (`Style(...)`).
   - Thêm nhãn thời gian `"08:12 / 20:00"` (đã chạy / tổng) và, sau mục 3, giờ trong ngày `"09:42"`.
   - Sự kiện mới `SpeedRequested(int)`; trong `Shell/MainForm.cs` (dòng ~210-214) nối: `tripResultPanel.SpeedRequested += (s, v) => mapHost.TripSpeed = v;` và `mapHost.TripProgress += ... tripResultPanel.SetProgress(clock, total)`.
   - Cập nhật tổng thời gian ở tiêu đề kết quả (đã có `TotalSeconds`) để nhãn và con số khớp nhau.
   - Phím tắt (tùy chọn): `Space` tạm dừng/chạy, `+`/`-` đổi tốc độ.
4. **Tùy chọn nên làm**: thanh kéo (`TrackBar`) để tua tới thời điểm bất kỳ: `RouteDrawing.SeekTo(seconds)` đặt `tripClock = seconds; travelled = DistanceAt(seconds); UpdateLeg(); DrawCar();`.
5. **Kiểm tra độ chính xác**: Σ `leg.Seconds` phải bằng `TotalSeconds`; trong `LegBuilder.Build` `Times` kết thúc đúng bằng `Seconds`, còn `TripPlanner.Joined` có thể thêm đoạn nối đầu/cuối với thời gian 0 (xe "dịch chuyển tức thời" vài mét từ marker ra đường). Nếu muốn mượt, gán cho đoạn nối thời gian = chiều dài / 6 m/s (đúng như `route_path_from_point` đang làm ở máy chủ) rồi cộng vào các mốc sau.

### Thử

- Chuyến ước tính 20 phút: bấm giờ ở x1 → xe tới đích sau 20 phút ± vài giây (đồng hồ trên nhãn khớp đồng hồ bấm tay).
- x8 → 2 phút 30 giây. Đổi tốc độ **giữa chừng** không làm xe nhảy vị trí.
- Tạm dừng rồi chạy tiếp; Chạy lại giữ nguyên tốc độ đang chọn.
- Chuyến rất ngắn (200 m, ~40 giây) cũng đúng 40 giây (không còn "tối thiểu cho khỏi chớp").

Lưu ý thực tế: ở x1, chuyến 20 phút là 20 phút thật, nên nên nhớ tốc độ lần trước vào `Properties.Settings` (đã có `Settings.settings`).

---

## Mục 3. Giờ mở cửa

### Phát hiện về dữ liệu thật (đã đọc từ các tệp `.dbf`)

Cột `GioMoCua` có nhiều dạng chứ không chỉ `07:00-17:00`:

| Dạng | Ví dụ | Số lượng (mẫu) |
|---|---|---|
| Một khoảng, gạch ngang thường | `07:00-17:00` | 21+ (di tích) |
| Một khoảng, **gạch ngang dài `–`** | `07:00–22:00` | ~50 (ẩm thực) |
| Cả ngày | `Cả ngày` | 7 |
| Không xác định | `Không cố định; nên đi ban ngày`, `Nên tham quan ban ngày`, `Đang cập nhật` | 6+ |
| Nhiều khoảng trong ngày | `T3-CN: 07:30-11:30; 13:00-17:00` | vài dòng |
| Khác nhau theo thứ | `T2–T6 08:00–22:00; T7–CN 08:00–23:00`, `T2–T5 09:00–18:00; T6 ...; CN ...` | vài dòng |
| Hai cột riêng (dịch vụ) | `GioMo = 07:00`, `GioDong = 21:00`, hoặc `Đang cập nhật` | 12 dòng |
| Lưu trú | `Nhận phòng .. Trả phòng ..` | không áp dụng mở/đóng |

**Lỗi đang có**: `f_gio_mo`/`f_gio_dong` trong `V001__extensions_and_helpers.sql` chỉ khớp gạch ngang ASCII `-` và một khoảng duy nhất,
nên toàn bộ các quán ăn dùng `–` hiện có `gio_mo`/`gio_dong` **bằng NULL** trong CSDL. Phải sửa (bước 1).

### Thiết kế

Dữ liệu gốc là chữ tự do nên đặt **bộ phân tích chữ → lịch mở cửa** ở máy khách (chạy được cả khi ngoại tuyến, vì ngoại tuyến
chỉ có chữ trong shapefile). Máy chủ chỉ cần sửa dữ liệu `gio_mo`/`gio_dong` cho đúng. Tối ưu thứ tự theo giờ mở cửa ở máy chủ là giai đoạn 2 (tùy chọn).

Múi giờ: dùng giờ **Việt Nam (UTC+7)**, không dùng giờ đồng hồ máy của khách (khách nước ngoài có thể để máy ở múi giờ khác):
`TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")`.

### Giai đoạn 1: hiển thị, lọc, cảnh báo lộ trình (máy khách)

1. **Sửa CSDL**: tạo `may-chu/co-so-du-lieu/migrations/V011__opening_hours.sql`, `create or replace function f_gio_mo / f_gio_dong`
   với biểu thức chính quy `'^(\d{1,2}):(\d{2})\s*[-–—]\s*(\d{1,2}):(\d{2})$'` (thêm `–` và `—`). Sau đó cập nhật lại các dòng đã nạp:
   `update poi set gio_mo = f_gio_mo(gio_mo_cua), gio_dong = f_gio_dong(gio_mo_cua) where gio_mo is null and gio_mo_cua is not null;`
   (chạy trong chính migration). Xem `transform.sql` có chỗ nào tự tách giờ khác không. CI đã tự áp dụng mọi migration lên CSDL trống nên sẽ bắt lỗi cú pháp.
2. **Tạo `Services/Hours/OpeningSchedule.cs`** (C# 7.3, .NET Framework 4.7.2, không phụ thuộc giao diện):
   ```csharp
   public enum OpenState { Unknown, Open, ClosingSoon, Closed }
   public sealed class OpeningSchedule
   {
       public bool IsKnown;                                   // false: "Đang cập nhật", "Không cố định"
       public List<TimeRange>[] Days;                         // [0]=Chủ nhật .. [6]=Thứ bảy; rỗng = nghỉ cả ngày
       public static OpeningSchedule Parse(string text, PoiCategory category);
       public OpenStatus StatusAt(DateTime vietnamTime);      // trạng thái + giờ đổi trạng thái kế tiếp
       public bool IsOpenForVisit(DateTime arrive, TimeSpan stay);
   }
   ```
   Quy tắc phân tích (ghi thành test, mỗi dòng ở bảng trên là một ca):
   - chuẩn hóa: đổi `–`, `—` thành `-`; bỏ khoảng trắng thừa; viết thường, bỏ dấu để so `ca ngay`, `dang cap nhat`, `khong co dinh`, `nen tham quan`.
   - "Cả ngày", "24/7", "24h" → mở 00:00-24:00 cả tuần.
   - Bắt đầu bằng "Không", "Nên", "Đang cập nhật", rỗng hoặc không có số giờ → `IsKnown = false`. **Không bao giờ coi là "đóng cửa"**; hiển thị "Chưa rõ giờ mở cửa".
   - Tách theo `;`. Đoạn có tiền tố thứ (`T2`..`T7`, `CN`, khoảng `T2-T6`, `T7-CN`, dấu `:` tùy chọn) bắt đầu nhóm ngày mới; đoạn **chỉ có giờ** thì nối tiếp vào nhóm ngày đang xét (ví dụ `T3-CN: 07:30-11:30; 13:00-17:00` → nhóm T3-CN có hai khoảng). Không có tiền tố thứ ở đâu → áp cả tuần. Khoảng thứ `T7-CN` phải xử lý đúng (T7 → CN), `T2` = thứ hai.
   - khoảng giờ `H:mm-H:mm` (chấp nhận `7:00`, `07h00`, `7:00 - 17:00`); `24:00` = 23:59; **đóng trước mở (ví dụ 18:00-02:00)** nghĩa là qua đêm: tách thành 18:00-24:00 hôm nay và 00:00-02:00 ngày hôm sau.
   - Dịch vụ: `Services/Data/ShapefilePoiRepository.cs` dòng 74-78 đã ghép `GioMo`/`GioDong` thành `"07:00 - 21:00"` hoặc `"Mở cửa từ 07:00"` / `"Đóng cửa lúc ..."`; xử lý thêm hai dạng sau (chỉ có giờ mở: mở từ giờ đó, không rõ giờ đóng → coi là Unknown với giờ đóng; chỉ có giờ đóng: Unknown).
   - Nhóm lưu trú (`PoiCategory` stay): bỏ qua hoàn toàn (đang là giờ nhận/trả phòng, không có khái niệm mở/đóng).
   - Trạng thái: `Open` nếu giờ hiện tại nằm trong một khoảng; `ClosingSoon` nếu còn ≤ 60 phút; `Closed` nếu ngoài mọi khoảng **hoặc** hôm nay nghỉ.
3. **Hiển thị**:
   - `Panels/PoiIdentify/PoiIdentifyCard.cs` dòng ~89 (`Row(g, "schedule", HoursCaption(...), ...)`): thêm sau giờ gốc một chip màu: **"Đang mở · đóng lúc 17:00"** (xanh), **"Sắp đóng · còn 25 phút"** (cam), **"Đã đóng · mở lại 07:00 mai"** (đỏ), **"Chưa rõ giờ"** (xám). Vẫn hiện nguyên văn chữ gốc vì dữ liệu có thể đúng hơn bộ phân tích.
   - `Panels/Shared/PoiRowList.cs` (danh sách tìm kiếm): chấm nhỏ xanh/đỏ cạnh tên.
   - `Shell/FilterBar.cs` + `Panels/SearchPoi/PoiSearchFilter.cs`: thêm bộ lọc **"Đang mở cửa"** (Unknown thì **vẫn hiện** vì không chắc chắn; chú thích rõ).
4. **Lộ trình** (`Panels/RouteOptimizer/RouteOptimizerPanel.cs`, `TripResultPanel.cs`, `Services/Routing/TripPlan.cs`):
   - Ô **"Giờ khởi hành"** (mặc định "Bây giờ"; chọn được giờ khác, dùng `DateTimePicker` kiểu giờ).
   - Ô **thời gian lưu lại** mỗi điểm, mặc định theo nhóm (đề xuất: di tích 60 phút, cảnh quan 30, ẩm thực 45, dịch vụ 30; người dùng sửa được cho từng điểm trong `StopList.cs`). Lưu trong `RouteSelection`.
   - Mở rộng `TripPlan` thêm `DepartAt` và với mỗi điểm dừng: `ArriveAt`, `LeaveAt` (= `ArriveAt + stay`), `OpenState`. Tính trong `TripPlanner.FromOnline`: `ArriveAt[i] = LeaveAt[i-1] + leg.Seconds` (chặng đầu từ `DepartAt`).
   - Trong danh sách điểm dừng ở `TripResultPanel` hiện "Đến 09:42 – Rời 10:42" và biểu tượng ⚠ khi `!IsOpenForVisit(ArriveAt, stay)` kèm giải thích ("Đóng cửa lúc 17:00, bạn đến lúc 17:20").
   - Tiêu đề kết quả có dòng tóm tắt "2 điểm có thể đã đóng cửa khi bạn đến".
   - **Không** làm máy chủ đổi thứ tự trong giai đoạn này; chỉ cảnh báo. Người dùng tự sắp lại (`StopList.cs`).
5. **Đồng hồ mô phỏng (nối với mục 4)**: giờ trong ngày = `DepartAt + tripClock + tổng thời gian đã dừng`. Khi xe tới điểm dừng, đồng hồ mô phỏng nhảy thêm đúng `stay` (xe tạm dừng ~1 giây thật rồi chạy tiếp) để giờ hiển thị luôn khớp với ETA. Cần mở rộng `RouteDrawing` với danh sách thời gian dừng theo từng chặng.
6. **Test**: thêm vào `test_services.ps1` ít nhất các ca:
   `07:00-17:00` lúc 06:59/07:00/16:30/17:00; `07:00–22:00` (gạch dài); `Cả ngày`; `Đang cập nhật` → Unknown, không phải Closed;
   `T3-CN: 07:30-11:30; 13:00-17:00` vào thứ hai (đóng) và vào 12:00 thứ ba (giờ nghỉ trưa, đóng); `T2–T6 08:00–22:00; T7–CN 08:00–23:00` vào chủ nhật 22:30 (mở);
   chuỗi qua đêm `18:00-02:00` lúc 01:00; chuỗi rác không làm ngoại lệ.

### Giai đoạn 2 (tùy chọn, sau khi giai đoạn 1 chạy ổn): máy chủ tự sắp thứ tự theo giờ mở cửa

`RouteOptimizer.Order` (Held-Karp, `may-chu/api/Routing/RouteOptimizer.cs`) hiện chỉ tối thiểu quãng đường. Có thể mở rộng
trạng thái `best[mask, last]` thành **thời điểm đến sớm nhất** (cho phép chờ tới giờ mở, loại trạng thái đến sau giờ đóng); độ phức tạp không đổi.
Việc này cần bộ phân tích giờ ở máy chủ (viết bản thứ hai bằng C# 12, hoặc thêm một cột JSON `lich_mo_cua` do bước import tính sẵn) và thêm `departAt`, `stayMinutes[]` vào `RouteRequest`.
Chỉ làm nếu đồ án cần; giai đoạn 1 đã đủ để "có giờ mở cửa" đúng và hữu ích.

### Thử (giai đoạn 1)

- Chọn Đại Nội lúc 18:00 → "Đã đóng · mở lại 07:00 mai". Chọn quán `07:00–22:00` lúc 21:00 → "Sắp đóng". Chọn điểm "Đang cập nhật" → "Chưa rõ giờ", không có ⚠ giả.
- Đổi giờ khởi hành sang 16:30 với lộ trình hai điểm di tích → điểm thứ hai hiện ⚠.
- Ngoại tuyến vẫn hiện đúng trạng thái (vì bộ phân tích chạy ở máy khách).
- Đổi múi giờ Windows sang UTC-5: kết quả không đổi (dùng giờ Việt Nam).

---

## Danh sách việc theo thứ tự (để đánh dấu)

- [ ] **Mục 2**: xóa tile Google ở máy khách (7 mục) và máy chủ (6 mục), build, chạy test, `grep` kiểm tra
- [ ] **Mục 1**: `AppSession.IsGuest` → nút khách ở `LoginForm` → `LoginForm(session, reason)` dạng hộp thoại → hàm `RunSignedIn` → sửa `UpdateAccountUi` → nối Yêu thích / Báo sai / Nhận quản lý / Đánh giá / Hồ sơ → nhãn "Khách" → thử tay các kịch bản
- [ ] **Mục 4**: bỏ nén thời gian ở `BuildLine` → thuộc tính `Speed` → nút 1×/2×/4×/8× + nhãn thời gian → thử bấm giờ
- [ ] **Mục 3.1**: migration V011 (sửa gạch ngang dài) → áp dụng và kiểm bằng `verify.sql`
- [ ] **Mục 3.2-3.3**: `OpeningSchedule` + test → chip trạng thái ở thẻ địa điểm, danh sách, bộ lọc "Đang mở"
- [ ] **Mục 3.4-3.5**: giờ khởi hành, thời gian lưu lại, ETA, cảnh báo ⚠, đồng hồ mô phỏng
- [ ] Cập nhật tài liệu: `so-do.md` (phân rã chức năng: thêm "Chế độ khách", "Giờ mở cửa", "Tua nhanh"), `HUONG-DAN-CAI-DAT.md`, README

## Rủi ro cần để ý

| Rủi ro | Cách tránh |
|---|---|
| Khách bị đá khỏi app vì `UpdateAccountUi` | Bước 6 mục 1; thử ngay sau khi sửa |
| Khách đăng nhập giữa chừng làm mất bản đồ/lộ trình | Dùng `LoginForm` dạng hộp thoại + `RunSignedIn`, không dùng `ReturnToSignIn()` |
| Chỉ chặn ở máy khách, máy chủ để hở | Máy chủ đã chặn 401; có kiểm tra curl ở mục "Thử" |
| Xóa tile làm vỡ cấu hình máy chủ cũ | Máy khách đọc trường thiếu thành null (cần thử); phát hành máy khách mới trước khi triển khai máy chủ là an toàn cả hai chiều |
| Phân tích giờ sai → báo "đã đóng" nhầm | Chỉ báo `Closed` khi **chắc chắn**; không rõ thì `Unknown`; luôn hiện nguyên văn chữ gốc |
| Xe chạy 20 phút thật làm người dùng bực | Nhớ tốc độ lần trước vào Settings |
| Thay đổi `TripPlan` ảnh hưởng nơi khác | `grep -rn "TripPlan\|TripLeg" may-khach` trước khi sửa; chỉ **thêm** thuộc tính, không đổi chữ ký cũ |
