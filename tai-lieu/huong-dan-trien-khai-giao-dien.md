# Hướng dẫn triển khai giao diện: từng bước, từng tệp

Bổ sung cho `ke-hoach-giao-dien.md` (kế hoạch: *làm gì và vì sao*). Tài liệu này là *làm thế nào*: tệp nào, dòng nào, mã nào. Đường dẫn tính từ `may-khach/ung-dung/` trừ khi ghi khác; số dòng là của mã tại thời điểm viết (nhánh `Cuong-bo/Hue-update`), nên **tìm theo nội dung dòng chứ đừng tin tuyệt đối vào số**, nhất là sau khi bạn đã sửa các tệp đó.

---

## 0. Đọc trước khi làm

### 0.1 Đính chính so với `ke-hoach-giao-dien.md`
Khi đọc nốt mã tôi thấy một số điều trong kế hoạch **sai hoặc đã có sẵn**. Lấy hướng dẫn này làm chuẩn:

| # | Kế hoạch nói | Thực tế trong mã | Hệ quả |
|---|---|---|---|
| 1 | Điểm đánh dấu là "chấm tròn 12 px phẳng" | Thật ra đã là **ảnh PNG tròn 34 px** (`PoiMarkerIcons.Draw`, nạp ở `MapHost.cs:285`). Chấm 12 px chỉ là dự phòng khi nạp ảnh lỗi | Chỉ cần đổi hình vẽ thành ghim giọt, không phải làm lại cơ chế |
| 2 | Lộ trình "chưa có viền, chưa đánh số, xe không quay hướng" | **Đã có**: viền trắng (`RouteDrawing.cs:196-206`), huy hiệu đánh số (`:235-269`), xe quay theo hướng chạy (`:425-456`) | Chỉ cần đổi bảng màu và thêm mũi tên chỉ hướng |
| 3 | Cửa sổ chính bo `RadiusXl` (28 px) | Một cửa sổ cấp cao nhất **không thể bo lớn mà vẫn mượt**: Windows 11 chỉ cho ~8 px, Windows 10 chỉ có `Region` răng cưa | Cửa sổ chính bo nhỏ (8 px); bo lớn 20-28 px chỉ áp cho **thẻ và panel bên trong** |
| 4 | Chip lọc nổi từng viên riêng | Điều khiển nổi trên bản đồ ActiveX **không có nền trong suốt** thật: khe giữa các chip sẽ lộ màu nền của `MapHost` | Cả hàng chip nằm trong **một viên thuốc trắng duy nhất** (xem Bước 6) |
| 5 | Thêm biểu tượng phải sửa tệp font | Đã có **5 biểu tượng tự vẽ bằng GDI** (`IconFont.cs`: `schedule`, `sell`, `landscape`, `arrow_forward`, `play_arrow`…) | Biểu tượng mới vẽ tay theo mẫu đó, không đụng tệp `.ttf` |
| 6 | Cơ chế lớp phủ trên bản đồ "chưa biết có chạy không" | `HighlightRing` đã dùng **đúng kỹ thuật** (cửa sổ phân lớp, click xuyên, trên bản đồ) và đang chạy | Rủi ro thấp hơn dự kiến, nhưng vẫn phải thử ở Bước 4 |

### 0.2 Mã mẫu đã kiểm chứng
Thư mục `tai-lieu/ma-mau-giao-dien/` chứa các tệp **sẵn dùng**. Chúng nằm ngoài `csproj` nên không ảnh hưởng build cho đến khi bạn chép vào `ung-dung/` và khai báo.

| Tệp mẫu | Chép tới | Đã kiểm chứng thế nào |
|---|---|---|
| `ThemeMetrics.cs` | `Theme/ThemeMetrics.cs` (ghi đè; **không nơi nào dùng tệp này**) | biên dịch ✔ |
| `ThemeColors.them-vao.txt` | dán vào cuối lớp `ThemeColors` | biên dịch ✔; độ tương phản đã tính ✔ |
| `ChromeLayer.cs` | `Theme/ChromeLayer.cs` | biên dịch ✔; ảnh bóng đổ đã xem ✔; **chạy lớp phủ thật trên cửa sổ: chưa** |
| `OverlayCard.cs` | `Theme/OverlayCard.cs` | biên dịch ✔; chạy thật: chưa |
| `FloatingPanelForm.cs` | `Theme/FloatingPanelForm.cs` (ghi đè) | biên dịch ✔; chạy thật: chưa |
| `PanelHeader.cs` | `Theme/PanelHeader.cs` (ghi đè) | biên dịch cùng `IconButton` thật ✔; chạy: chưa |
| `WindowChrome.cs` | `Theme/WindowChrome.cs` | biên dịch ✔ |
| `IconsExtra.cs` | `Theme/IconsExtra.cs` | biên dịch ✔; ảnh 16/24/48 px đã xem ✔ |
| `SearchPill.cs` | `Shell/SearchPill.cs` | biên dịch ✔; chạy: chưa |
| `TitleBar.cs` | `Shell/TitleBar.cs` (**ghi đè và xóa `TitleBar.Designer.cs`**) | biên dịch ✔; chạy: chưa |
| `PoiPin.cs` | `MapEngine/PoiPin.cs` | biên dịch ✔; ảnh ghim 34/60 px đã xem ✔ |

Quan trọng: tôi **chưa chạy được ứng dụng thật** (cần MapWinGIS, máy chủ và dữ liệu). Mọi thứ "chạy: chưa" nghĩa là biên dịch được nhưng bạn phải xem bằng mắt; mỗi bước dưới đây có mục **Kiểm tra** nói rõ cần nhìn gì.

### 0.3 Quy trình làm việc
1. **Nhánh riêng**: `git switch -c giao-dien` từ nhánh hiện tại. Mỗi bước dưới đây là **một commit** (build xanh rồi mới commit). Nếu một bước hỏng, `git revert` đúng commit đó.
2. **Khai báo tệp mới**: `Hue_travel.csproj` là kiểu cũ, phải liệt kê từng tệp. Với mỗi tệp `.cs` chép vào, thêm một dòng cạnh các dòng tương tự (ví dụ gần `MapEngine\HighlightRing.cs`, dòng ~235):
   ```xml
   <Compile Include="Theme\ChromeLayer.cs" />
   ```
   Quên bước này thì tệp không được biên dịch và bạn sẽ gặp lỗi "type not found". Tệp đã bị xóa thì xóa luôn dòng khai báo.
3. **Build**: trong Visual Studio nhấn Ctrl+Shift+B, hoặc dòng lệnh:
   ```
   "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" may-khach\Hue_travel.sln -t:Build -v:minimal -nologo
   ```
   (đây là lệnh tôi đã dùng và build thành công; đổi đường dẫn nếu Visual Studio của bạn ở chỗ khác).
4. **Ảnh chụp trước/sau**: trước khi sửa gì, chụp cửa sổ chính ở 100% DPI và lưu vào `tai-lieu/anh-giao-dien/truoc/`. Sau mỗi bước, chụp lại vào `sau-buoc-N/`. Đổi DPI: *Cài đặt → Hiển thị → Tỉ lệ* (100/125/150%), **đăng xuất rồi đăng nhập lại Windows** vì ứng dụng nhận biết DPI hệ thống một lần.
5. **Tắt tạm tính năng nếu hỏng**: công tắc `FloatingPanelForm.SoftChrome = false` (Bước 4) đưa panel về cách cũ ngay, không cần revert.

### 0.4 Thứ tự và độ lớn (S nhỏ, M vừa, L lớn)
| Bước | Việc | Cỡ | Làm được độc lập? |
|---|---|---|---|
| 1 | Token màu và bo góc, đổi tông toàn app | M | Có |
| 2 | Font serif | S | Có |
| 3 | Biểu tượng mới | S | Có |
| 4 | **Lớp bóng + panel mới (kiểm chứng kỹ thuật)** | L | Sau 1 |
| 5 | Nút, đầu panel, ô nhập | M | Sau 1, 2 |
| 6 | Vỏ cửa sổ: thanh tiêu đề, ô tìm kiếm, chip, thanh dọc, thanh trạng thái | L | Sau 4, 5 |
| 7 | Các panel: danh sách có ảnh, thẻ địa điểm, bán kính | L | Sau 5 |
| 8 | Bản đồ: màu nền, ghim giọt, lộ trình | L | Sau 1 |
| 9 | Hoàn thiện và nghiệm thu | M | Cuối |

Nếu thiếu thời gian: **Bước 1 + 4 + 5 đã đổi diện mạo rõ rệt**; Bước 6 là bước lớn nhất, Bước 8 cho cảm giác "bản đồ du lịch".

---

## Bước 1. Token màu và bo góc

**Mục tiêu:** toàn app đổi sang tông tím Huế + giấy dó ấm, mọi bán kính bo góc có một thang chung. Chưa đổi bố cục.

### 1.1 `Theme/ThemeMetrics.cs`
Chép đè bằng `ma-mau-giao-dien/ThemeMetrics.cs`. Tệp cũ chỉ có `RadiusSm=4, RadiusLg=8` và **không nơi nào gọi** (đã `Grep ThemeMetrics\.` không có kết quả), nên đổi giá trị thoải mái. Thang mới: `RadiusXs 6, Sm 10, Md 14, Lg 20, Xl 28, Full 9999`.

Quy ước dùng, để các lần sau không đổi ý:
- thuộc tính `IconButton.Radius`, `RoundedPanel.Radius`: truyền **thẳng** `ThemeMetrics.RadiusSm` (chúng tự nhân `Ui.S` bên trong);
- chỗ gọi `Ui.RoundedRect(rect, bán_kính)`: truyền `Ui.S(ThemeMetrics.RadiusSm)`.

### 1.2 `Theme/ThemeColors.cs`: thêm bảng màu mới
Dán nội dung `ThemeColors.them-vao.txt` vào cuối lớp, trước dấu `}` đóng lớp. Nó thêm `Brand, BrandDeep, BrandSoft, Gold, GoldSoft, GoldText, Lacquer, LacquerSoft, Jade, JadeSoft, Paper, PaperDeep, Ink, InkSoft, Line` và sáu màu nhóm địa điểm `CatHeritage … CatTransport`.

Độ tương phản đã tính (WCAG, cần ≥ 4.5 cho chữ, ≥ 3 cho biểu tượng):

| Cặp | Tỉ lệ | Ghi chú |
|---|---|---|
| chữ trắng trên `Brand` | 9.9 | tốt |
| `Ink` trên `Paper` | 15.6 | chữ chính |
| `InkSoft` trên `Paper` | 6.7 | chữ phụ |
| chữ trắng trên `CatHeritage #A0620F` | 4.9 | đã chỉnh từ `#B7791F` (3.6, không đạt) |
| chữ trắng trên `CatScenery/Food/Stay/Service/Transport` | 5.2 / 5.5 / 7.7 / 6.9 / 7.6 | đạt |
| chữ trắng trên `Gold` | **2.6** | **không dùng chữ trắng trên vàng**: dùng `Ink` (6.4) hoặc `GoldText` trên `GoldSoft` (4.6) |
| `Slate400` mới trên trắng | 3.6 | chỉ cho biểu tượng/gợi ý, **không** cho chữ thân |

### 1.3 `Theme/ThemeColors.cs`: đổi giá trị các màu cũ để toàn app đổi tông ngay
Đổi **giá trị** (giữ tên) để hàng trăm chỗ đang gọi `Primary`, `Sky*`, `Slate*` tự đổi theo. Sửa các dòng sau:

| Tên (dòng) | Cũ | Mới |
|---|---|---|
| `Primary` (8) | `#0284C7` | `#5B2A86` |
| `PrimaryContainer` (9) | `#0369A1` | `#4C2273` |
| `Surface` (12) | `#F8FAFC` | `#FBF8F3` |
| `Background` (13) | `#F0F9FF` | `#F2EBDD` |
| `SurfaceContainerLow` (15) | `#F0F7FF` | `#F7F1FB` |
| `SurfaceContainer` (16) | `#E0F2FE` | `#F3ECF9` |
| `SurfaceContainerHigh` (17) | `#BAE6FD` | `#E4D4F2` |
| `OnSurface` (19) | `#0F172A` | `#241B2F` |
| `OnSurfaceVariant` (20) | `#334155` | `#4B4259` |
| `Secondary` (22) | `#64748B` | `#5E5470` |
| `Outline` (25) | `#94A3B8` | `#8C8299` |
| `OutlineVariant` (26) | `#CBD5E1` | `#D6CBB8` |
| `Error` (28) | `#DC2626` | `#B3261E` |

Dải `Slate50…900` (dòng 44-53), cũ → mới:
`#F8FAFC→#FBF8F3`, `#F1F5F9→#F4EEE3`, `#E2E8F0→#E7DFD0`, `#CBD5E1→#D6CBB8`, `#94A3B8→#8C8299`, `#64748B→#5E5470`, `#475569→#4B4259`, `#334155→#3A3047`, `#1E293B→#2D2339`, `#0F172A→#241B2F`.

Dải `Sky50…900` (dòng 55-64) giờ là **dải tím thương hiệu** (tên cũ giữ lại để khỏi sửa hàng trăm chỗ; hãy thêm chú thích "legacy name: holds the brand ramp"):
`Sky50 #F7F1FB`, `Sky100 #F3ECF9`, `Sky200 #E4D4F2`, `Sky300 #C9A9E3`, `Sky400 #A877CF`, `Sky500 #8A4DB8`, `Sky600 #5B2A86`, `Sky700 #4C2273`, `Sky800 #3F1D63`, `Sky900 #2D1347`.

Giữ nguyên `Amber*`, `Orange600`, `Blue600`, `Cyan*`, `Emerald*`, `Red500` (dùng cho sao, trạng thái).

### 1.4 Màu nhóm địa điểm: một nguồn duy nhất
- `MapEngine/PoiMarkerStyle.cs:16-21`: `ColorOf` trả về `CatHeritage / CatScenery / CatFood / CatStay / CatService`, mặc định `CatTransport`.
- `Shell/FilterBar.cs:35-40`: `IconColor` của từng chip dùng cùng sáu màu (`Emerald700` ở dòng 39 → `CatService`; `Slate600` ở dòng 40 → `CatTransport`).
- Sau đó `Grep "ThemeColors.Category"` và thay hết, rồi **xóa** bốn dòng `CategoryHeritage/Food/Stay/Scenery` cũ (30-33).

### 1.5 Quét các màu viết cứng
Màu đã đổi tông nhưng nhiều nơi viết thẳng `Color.FromArgb(...)` hoặc `Color.White`. Quét bằng:
```
Grep  pattern: Color\.FromArgb\(\d+, ?\d+, ?\d+\)   glob: *.cs
Grep  pattern: BackColor = Color\.White             glob: *.cs
```
Bỏ qua `LocalBaseMap.cs` và `RoutePalette.cs` (sửa ở Bước 8) và `RouteDrawing.cs` (màu xe). Với `BackColor = Color.White` của nền panel/danh sách/thẻ (ví dụ `PoiIdentifyCard.cs:171`, `InfoView` dòng 31, `PhotoGallery.cs:37`, `PoiRowList.cs:59`, `SearchDropdown.cs:35`), đổi thành `ThemeColors.Paper` để khớp với nền thẻ ở Bước 4.

### Kiểm tra bước 1
Build, chạy, đăng nhập. **Toàn app ngả tím và ấm**: nút chính, chip chọn, thanh dọc, vòng sáng quanh địa điểm (`HighlightRing.cs:98` dùng `Primary`) đều tím. Marker và chip cùng màu theo nhóm. Chữ vẫn đọc rõ. Nếu một chỗ còn xanh trời: đó là màu viết cứng, sửa theo 1.5.

**Commit:** "Giao diện: bảng màu Huế và thang bo góc".

---

## Bước 2. Font serif cho tiêu đề

**Mục tiêu:** tên địa điểm và tiêu đề panel dùng chữ có chân (Lora). Chữ thân vẫn là Plus Jakarta Sans.

1. Tải **Lora** từ Google Fonts (giấy phép SIL OFL). Lấy hai tệp tĩnh `Lora-SemiBold.ttf` và `Lora-Bold.ttf`. Lora hỗ trợ tiếng Việt; vẫn nên mở tệp xem thử "Cố đô Huế, ầ ẫ ộ ơ ữ ế" trong trình xem font.
2. Chép vào `Resources/`.
3. `Hue_travel.csproj`: thêm cạnh các dòng `PlusJakartaSans` (~329-340), cùng kiểu:
   ```xml
   <EmbeddedResource Include="Resources\Lora-SemiBold.ttf">
     <LogicalName>Hue_travel.Resources.Lora-SemiBold.ttf</LogicalName>
   </EmbeddedResource>
   <EmbeddedResource Include="Resources\Lora-Bold.ttf">
     <LogicalName>Hue_travel.Resources.Lora-Bold.ttf</LogicalName>
   </EmbeddedResource>
   ```
4. `Theme/FontLoader.cs:20-27`: thêm `"Lora-SemiBold.ttf", "Lora-Bold.ttf",` vào mảng `FontFiles`.
5. `Theme/ThemeFonts.cs` (đã biên dịch thử):
   - sau dòng `private const string MonoFamily = "Consolas";` thêm
     ```csharp
     public const string SerifFamily = "Lora";
     private const string SerifFallback = "Georgia";
     ```
   - trước `public static Font Mono(...)` thêm
     ```csharp
     /// <summary>The serif of titles and place names (Lora SemiBold); Georgia when the font file is missing.</summary>
     public static Font Serif(float px) => Make(SerifFamily + " SemiBold", px, FontStyle.Regular);
     public static Font SerifBold(float px) => Make(SerifFamily, px, FontStyle.Bold);
     ```
   - trong `Make(...)`: ngay sau `float points = ...;` thêm `string fallback = family.StartsWith(SerifFamily) ? SerifFallback : FallbackFamily;` rồi thay **hai** chỗ dùng `FallbackFamily` ở khối `font = ...` và `catch` bằng `fallback`.
6. `Resources/LICENSES.md`: ghi dòng "Lora, SIL Open Font License 1.1".

### Kiểm tra bước 2
Tên họ font trong tệp `.ttf` tĩnh có thể là `Lora SemiBold` (đúng như mã giả định, giống cách `Plus Jakarta Sans SemiBold` đang chạy) hoặc khác. Kiểm bằng một đoạn tạm:
```csharp
var pfc = new System.Drawing.Text.PrivateFontCollection();
pfc.AddFontFile(@"đường\dẫn\Lora-SemiBold.ttf");
foreach (var f in pfc.Families) System.Diagnostics.Debug.WriteLine("[" + f.Name + "]");
```
Nếu in ra tên khác `Lora SemiBold`, sửa chuỗi trong `ThemeFonts.Serif` cho khớp. Rồi thử `ThemeFonts.Serif(22)` ở một nhãn: có dấu tiếng Việt đủ, và **không** rơi về Georgia (nhìn nét chữ).

**Commit:** "Giao diện: font serif Lora".

---

## Bước 3. Biểu tượng mới

**Mục tiêu:** có `favorite` (tim), `bookmark`, `share`, `content_copy`, `language` cho thẻ địa điểm.

1. Chép `IconsExtra.cs` vào `Theme/`, khai báo trong csproj.
2. `Theme/IconFont.cs`, hàm `Render` (dòng ~183): thêm **trước** khối `if (name == "arrow_forward")` (dòng ~217):
   ```csharp
   if (name == "favorite" || name == "favorite_fill") { IconsExtra.DrawHeart(bmp, px, color, name == "favorite_fill"); return bmp; }
   if (name == "bookmark" || name == "bookmark_fill") { IconsExtra.DrawBookmark(bmp, px, color, name == "bookmark_fill"); return bmp; }
   if (name == "share") { IconsExtra.DrawShare(bmp, px, color); return bmp; }
   if (name == "content_copy") { IconsExtra.DrawCopy(bmp, px, color); return bmp; }
   if (name == "language") { IconsExtra.DrawGlobe(bmp, px, color); return bmp; }
   ```
3. Các biểu tượng sau **đã có** trong font tập con (bảng `Glyphs`, dòng 16-39): `star`, `directions`, `place`, `pin_drop`, `call`, `check_circle`, `lock`, `explore`, `support_agent`, `route`. Không cần vẽ.

Ghi chú: `IconFont.Contains(name)` chỉ nhìn bảng `Glyphs` nên với biểu tượng tự vẽ nó trả `false` (như `schedule` hiện nay). Nếu chỗ nào của bạn kiểm tra bằng `Contains` trước khi vẽ, bỏ kiểm tra đó.

### Kiểm tra bước 3
Tạo tạm một nút `new IconButton { IconName = "favorite", IconSize = 20 }`: tim hiện đúng màu `IconColor`, sắc nét ở 100% và 150% DPI. Ảnh mẫu 16/24/48 px tôi đã xem: nét rõ, bookmark có khuyết đáy, globe có kinh tuyến.

**Commit:** "Giao diện: biểu tượng tim, đánh dấu, chia sẻ".

---

## Bước 4. Lớp bóng đổ và panel nổi mới (bước kiểm chứng kỹ thuật)

**Mục tiêu:** panel bo 20 px, bóng mềm, trượt và mờ khi mở; chạy giống nhau trên Windows 10 và 11.

### 4.1 Cơ chế (để bạn hiểu mà sửa được)
- `ChromeLayer` là **một** cửa sổ phân lớp, click xuyên, phủ toàn vùng client của cửa sổ chính (cùng kỹ thuật `HighlightRing`). Nó vẽ bóng mờ (bitmap bóng dựng sẵn bằng ba lần làm mờ hộp, lưu đệm theo kích cỡ) cho mọi "thẻ" đã đăng ký.
- **Panel** (là `Form`, nằm **trên** lớp): lớp vẽ *thẻ đặc* bo mượt màu `Paper` kèm viền 1 px dưới panel; chính panel bị cắt bằng `Region` **thụt vào 1 px**. Phần răng cưa của `Region` rơi lên nền thẻ cùng màu nên không thấy; viền mượt do lớp vẽ chính là cạnh nhìn thấy.
- **Điều khiển trên bản đồ** (nằm **dưới** lớp): lớp **đục một lỗ** đúng hình thẻ (để bóng không làm tối điều khiển) và vẽ viền 1 px mượt đè lên cạnh `Region`.
- Lớp được hạ xuống đáy các cửa sổ cùng chủ (`SetWindowPos HWND_BOTTOM`) để các panel mở sau luôn nằm trên nó.

### 4.2 Việc làm
1. Chép `ChromeLayer.cs`, `OverlayCard.cs` vào `Theme/`; khai báo trong csproj.
2. **Ghi đè** `Theme/FloatingPanelForm.cs` bằng `ma-mau-giao-dien/FloatingPanelForm.cs`. API công khai giữ nguyên (`AttachTo`, `AttachToSide`, `RememberPosition`, `ToggleCollapsed`, `Notify`, `CloseRequested`, `MessageRaised`) nên các panel con không phải sửa. Khác biệt: bán kính 20 (`ThemeMetrics.RadiusLg`), `BackColor = Paper`, trượt lên 16 px + mờ trong ~8 khung hình, tôn trọng `SystemInformation.UIEffectsEnabled` (người tắt hoạt ảnh Windows thấy panel hiện ngay).
3. Không cần sửa `MainForm`: panel tự đăng ký lớp khi gọi `AttachToSide/AttachTo` (trong `InitializeComponent`), và lớp tự tạo theo cửa sổ chính.
4. Công tắc lùi: `FloatingPanelForm.SoftChrome` (tĩnh, mặc định `true`). Đặt `false` ở đầu `Program.Main` để về cách cũ của Windows 11 (bo 8 px, bóng hệ thống).

### 4.3 Kiểm tra bước 4: phải làm kỹ, đây là rủi ro lớn nhất
Build, chạy, mở từng panel (Khám phá, Lộ trình, Hướng dẫn, Lớp, thẻ địa điểm):

| # | Nhìn gì | Đúng là |
|---|---|---|
| 1 | Quanh panel | Có bóng mềm, lệch xuống, không có đường chữ nhật cứng |
| 2 | Góc panel (phóng ảnh chụp 300%) | Bo 20 px mượt, **không răng cưa** |
| 3 | Viền panel | Một đường mảnh màu `Line` (be nhạt). Không thích thì đặt `Ring = Color.Empty` trong `RegisterCard` |
| 4 | Mở/đóng panel liên tục | Trượt lên + mờ, **không nhấp nháy**, bóng mờ cùng panel |
| 5 | Kéo cửa sổ chính | Bóng đi theo, trễ không quá một khung hình |
| 6 | Thu nhỏ rồi khôi phục cửa sổ | Bóng trở lại đúng chỗ; không còn bóng "ma" |
| 7 | Chuyển sang ứng dụng khác rồi quay lại | Bóng **nằm dưới** panel, không phủ lên nội dung panel |
| 8 | Bấm xuyên qua vùng bóng | Chuột tới bản đồ bên dưới (lớp click xuyên) |
| 9 | 125% và 150% DPI | Bán kính, bóng, độ lệch đều tỉ lệ đúng |
| 10 | Windows 10 (nếu có máy) | Giống Windows 11: bo 20 px, có bóng |

**Nếu lỗi:**

| Triệu chứng | Nguyên nhân hay gặp | Cách xử lý |
|---|---|---|
| Bóng phủ lên nội dung panel | Lớp được hiện **sau** panel và lên trên | Trong `OnShown` của `MainForm`, gọi `ChromeLayer.For(this).RequestRender()` **trước** mọi `ShowSidePanel`; `ChromeLayer.Render` đã hạ xuống đáy sau khi `Show`, kiểm tra dòng `SetWindowPos` còn đó |
| Nhấp nháy khi kéo/đổi cỡ | Dựng lại quá dày | Tăng `coalesce.Interval` từ 16 lên 33 |
| Có 1 px sáng ở góc | Viền `Line` + nền `Paper` | Đúng thiết kế; bỏ bằng `Ring = Color.Empty` |
| Bóng không đổi khi panel đổi cỡ | Thiếu gọi vẽ lại | `RegisterCard` đã nối `SizeChanged/LocationChanged/VisibleChanged`; kiểm tra bạn chưa ghi đè `OnResize` mà quên `base` |
| CPU tăng cao | Nhiều kích cỡ khác nhau làm bộ đệm bóng dựng liên tục | Bộ đệm tối đa 48; nếu panel co giãn liên tục, làm tròn kích cỡ khi tra bộ đệm |
| Panel bị lệch 1 px so với bóng | `Region` thụt 1 px là chủ ý | Không phải lỗi |

**Cổng quyết định:** nếu mục 1-4 đạt, tiếp tục. Nếu không khắc phục được, đặt `SoftChrome = false` và **bỏ qua phần bo lớn** ở Bước 6 (giữ bố cục nổi, chấp nhận bo 8 px). Phần còn lại của kế hoạch vẫn làm được.

**Commit:** "Giao diện: lớp bóng đổ và panel nổi bo 20".

---

## Bước 5. Nút, đầu panel, ô nhập

### 5.1 `Theme/IconButton.cs`: kiểu nút dựng sẵn (đã biên dịch thử)
Thêm ngay dưới dòng `[Category("Appearance")] public int Radius { get; set; }` (dòng ~96):
```csharp
/// <summary>Ready-made looks. Set Kind first, then override single colours if a button needs it.</summary>
public enum ButtonKind { Custom, Primary, Tonal, Outline, Ghost, Danger }

private ButtonKind kind = ButtonKind.Custom;

[Category("Appearance"), DefaultValue(ButtonKind.Custom)]
public ButtonKind Kind { get { return kind; } set { kind = value; ApplyKind(); Invalidate(); } }

private void ApplyKind()
{
    switch (kind)
    {
        case ButtonKind.Primary:   // the one main action of a screen
            FillColor = ThemeColors.Brand; HoverFillColor = ThemeColors.BrandDeep; BorderColor = Color.Empty;
            ForeColor = Color.White; IconColor = Color.White; TrailingIconColor = Color.White; Elevated = true;
            break;
        case ButtonKind.Tonal:     // a secondary action that still deserves attention
            FillColor = ThemeColors.BrandSoft; HoverFillColor = Color.FromArgb(233, 219, 245); BorderColor = Color.Empty;
            ForeColor = ThemeColors.Brand; IconColor = ThemeColors.Brand; TrailingIconColor = ThemeColors.Brand; Elevated = false;
            break;
        case ButtonKind.Outline:
            FillColor = Color.White; HoverFillColor = ThemeColors.BrandSoft; BorderColor = ThemeColors.Brand;
            ForeColor = ThemeColors.Brand; IconColor = ThemeColors.Brand; TrailingIconColor = ThemeColors.Brand; Elevated = false;
            break;
        case ButtonKind.Ghost:     // icon buttons and quiet actions
            FillColor = Color.Transparent; HoverFillColor = ThemeColors.BrandSoft; BorderColor = Color.Empty;
            ForeColor = ThemeColors.InkSoft; IconColor = ThemeColors.InkSoft; TrailingIconColor = ThemeColors.InkSoft; Elevated = false;
            break;
        case ButtonKind.Danger:
            FillColor = ThemeColors.Lacquer; HoverFillColor = Color.FromArgb(143, 29, 23); BorderColor = Color.Empty;
            ForeColor = Color.White; IconColor = Color.White; TrailingIconColor = Color.White; Elevated = false;
            break;
    }
}
```
Và trong hàm khởi tạo, đổi `Radius = 6;` (dòng 52) thành `Radius = ThemeMetrics.RadiusSm;`.

Dùng thử, thay cho cả cụm gán màu thủ công. Ví dụ `PoiIdentifyCard.cs:182-192`:
```csharp
btnRoute.AutoWidth = false;
btnRoute.Text = "Thêm vào lộ trình tham quan";
btnRoute.IconName = "add";
btnRoute.IconSize = 18;
btnRoute.Gap = 8;
btnRoute.Kind = IconButton.ButtonKind.Primary;   // thay cho 6 dòng gán màu/bo góc/nổi
```
Và `AccountButton(...)` (dòng 294-302): bỏ `ForeColor/IconColor/FillColor/HoverFillColor/BorderColor/Radius`, thêm `Kind = IconButton.ButtonKind.Outline`.

### 5.2 `Theme/PanelHeader.cs`
**Ghi đè** bằng `ma-mau-giao-dien/PanelHeader.cs`. Khác bản cũ: cao 52 (cũ 32), nền gradient `Brand → BrandDeep` kèm vạch vàng 2 px dưới, tiêu đề serif 17, nút đóng/thu nhỏ **tròn 28 px**, nút đóng đỏ son khi rê chuột. Giữ nguyên API (`IconName`, `Prominent`, `ShowMinimize`, `Text`, `CloseClicked`, `MinimizeClicked`).

Việc cần xem: panel nào gán `Height` của header riêng hoặc tính vị trí dưới header theo 32 px. `Grep "PanelHeader"` rồi mở từng panel xem thân panel có bị chồng lên header (header giờ cao hơn 20 px).

### 5.3 Bo góc ô nhập và hộp
| Tệp:dòng | Nay | Đổi thành |
|---|---|---|
| `Theme/InputField.cs:168` | `Ui.S(6)` | `Ui.S(ThemeMetrics.RadiusSm)` |
| `Theme/SelectField.cs:81` | `Ui.S(6)` | `Ui.S(ThemeMetrics.RadiusSm)` |
| `Theme/RoundedPanel.cs:20` | `Radius = 6;` | `Radius = ThemeMetrics.RadiusSm;` |
| `Welcome/AuthField.cs:149` | `Ui.S(12)` | `Ui.S(ThemeMetrics.RadiusSm)` |
| `Welcome/AuthField.cs:73` | `revealButton.Radius = 14;` | `= ThemeMetrics.RadiusFull;` (nút tròn) |

Bảng đầy đủ các chỗ bo góc còn lại ở Bước 7.4.

### Kiểm tra bước 5
Mở panel Lộ trình và thẻ địa điểm: header tím gradient có vạch vàng, nút tròn; nút chính tím đặc nổi nhẹ, nút phụ viền tím. Ô nhập bo 10 px. Thử rê chuột và bấm: hiệu ứng gợn sóng vẫn chạy (`IconButton` không đổi cơ chế).

**Commit:** "Giao diện: kiểu nút, đầu panel, ô nhập".

---

## Bước 6. Vỏ cửa sổ chính

**Mục tiêu:** bản đồ tràn mép; ô tìm kiếm, chip lọc, thanh dọc, thanh trạng thái là các thẻ nổi trên bản đồ; thanh tiêu đề mảnh 40 px.

Đây là bước lớn nhất. **Chia làm 6 commit nhỏ** (6.1-6.6), mỗi cái build được.

### 6.1 Thanh tiêu đề mảnh
1. Chép `ma-mau-giao-dien/TitleBar.cs` đè `Shell/TitleBar.cs`.
2. **Xóa** `Shell/TitleBar.Designer.cs` và khối `<Compile Include="Shell\TitleBar.Designer.cs">…</Compile>` (csproj dòng ~212-214, có `<DependentUpon>`). Ở khối `<Compile Include="Shell\TitleBar.cs">` (dòng ~209), **xóa dòng con `<SubType>`** nếu có (nó khai báo `UserControl`; bản mới là `Control` thuần, giữ dòng đó thì Visual Studio sẽ cố mở nó bằng bộ thiết kế). Nếu có `TitleBar.resx` thì xóa cả nó và dòng khai báo.
3. API còn lại: sự kiện `MinimizeClicked/MaximizeClicked/CloseClicked`, `SetMaximized(bool)`, và mới `SetWeather(string)`. Các thành viên tìm kiếm (`SearchTextChanged`, `SearchBoxBounds`, `ClearSearch`, …) **đã bỏ**: `MainForm` sẽ báo lỗi biên dịch ở các dòng dùng chúng, và 6.2 sửa đúng các dòng đó.
4. `MainForm.Designer.cs:67`: `this.titleBar.Size = new Size(1536, 56);` → `new Size(1536, 40);`.

### 6.2 Ô tìm kiếm nổi
1. Chép `ma-mau-giao-dien/SearchPill.cs` vào `Shell/`; khai báo csproj.
2. `MainForm.Designer.cs`: thêm trường `private SearchPill searchPill;` (cạnh dòng 21-34) và `this.searchPill = new SearchPill();` (cạnh dòng 44-56).
3. `MainForm.cs`, đổi tên đối tượng ở các sự kiện tìm kiếm (dòng 130-137, đổi `titleBar.` thành `searchPill.`):
   ```csharp
   searchPill.SearchTextChanged += (s, text) => UpdateSearchResults(text);
   searchPill.SearchFocusGained += (s, e) => UpdateSearchResults(searchPill.SearchText);
   searchPill.SearchFocusLost += (s, e) => searchDropdown.Dismiss();
   searchPill.SearchMoved += (s, delta) => searchDropdown.MoveSelection(delta);
   searchPill.SearchSubmitted += (s, text) => { if (!searchDropdown.ChooseSelected()) UpdateSearchResults(text); };
   ```
4. `MainForm.cs:276-277` (đọc dòng ~270-285 trước), hai dòng:
   ```csharp
   System.Drawing.Rectangle box = titleBar.SearchBoxBounds;
   box.Offset(titleBar.Location);
   ```
   đổi thành tọa độ của ô tìm kiếm **trong vùng client của cửa sổ chính** (nay nó nằm trong `MapHost`, không còn trong thanh tiêu đề):
   ```csharp
   System.Drawing.Rectangle box = RectangleToClient(searchPill.RectangleToScreen(searchPill.ClientRectangle));
   ```
5. Nơi khác dùng `titleBar.ClearSearch()` hay `SearchText`: `Grep "titleBar\."` trong `MainForm.cs` và sửa thành `searchPill.`.

(Ô tìm kiếm chưa hiện được cho đến khi 6.3 đặt nó vào `MapHost`; làm 6.2 và 6.3 liền nhau.)

### 6.3 `MapHost` chứa các thẻ nổi
`MapHost.cs` thêm trường và hàm (đặt cạnh `LayoutOverlays`, dòng ~628):
```csharp
private SearchPill searchPill;
private Hue_travel.Shell.FilterBar filterBar;
private Hue_travel.Shell.NavRail navRail;
private Hue_travel.Shell.AppStatusBar statusBar;

/// <summary>Moves the search box, the chip bar, the tool rail and the status pill onto the map as floating cards.</summary>
public void HostOverlays(Form host, SearchPill search, Hue_travel.Shell.FilterBar chips, Hue_travel.Shell.NavRail rail, Hue_travel.Shell.AppStatusBar status)
{
    searchPill = search; filterBar = chips; navRail = rail; statusBar = status;
    foreach (Control c in new Control[] { search, chips, rail, status })
    {
        if (c.Parent != null) c.Parent.Controls.Remove(c);
        c.Dock = DockStyle.None;
        Controls.Add(c);
        c.BringToFront();
    }
    OverlayCard.Attach(host, search, ThemeMetrics.RadiusFull);
    OverlayCard.Attach(host, chips, ThemeMetrics.RadiusFull);
    OverlayCard.Attach(host, rail, ThemeMetrics.RadiusLg);
    OverlayCard.Attach(host, status, ThemeMetrics.RadiusFull);
    OverlayCard.Attach(host, stackBaseMaps, ThemeMetrics.RadiusMd);
    OverlayCard.Attach(host, stackZoom, ThemeMetrics.RadiusMd);
    OverlayCard.Attach(host, stackTools, ThemeMetrics.RadiusMd);
    OverlayCard.Attach(host, compass, ThemeMetrics.RadiusFull);
    OverlayCard.Attach(host, tripChip, ThemeMetrics.RadiusFull);
    LayoutOverlays();
}
```
và trong `LayoutOverlays()` (cuối hàm, dòng ~640) thêm bố trí **tọa độ trong MapHost** (gốc ở dưới thanh tiêu đề):
```csharp
if (searchPill != null)
{
    int m = Ui.S(16);
    searchPill.SetBounds(m, m, Ui.S(420), Ui.S(48));
    int chipsLeft = searchPill.Right + Ui.S(12);
    int chipsRoom = Width - chipsLeft - Ui.S(RightMargin) - stackBaseMaps.Width - Ui.S(12);
    filterBar.SetBounds(chipsLeft, m + Ui.S(4), Math.Max(Ui.S(120), Math.Min(filterBar.WantedWidth, chipsRoom)), Ui.S(40));
    navRail.SetBounds(Ui.S(12), m + Ui.S(48) + Ui.S(12), Ui.S(64), navRail.WantedHeight);
    int statusWidth = Math.Min(Ui.S(720), Width - Ui.S(24));
    statusBar.SetBounds((Width - statusWidth) / 2, Height - Ui.S(32) - Ui.S(12), statusWidth, Ui.S(32));
}
```
Chú ý: cột nút bên phải dùng `ColumnTop = 16` (`MapHost.cs:20`), nên nó đã nằm cùng hàng với ô tìm kiếm (cũng ở y = 16): không phải sửa. Chỉ cần thêm `using Hue_travel.Shell;` ở đầu `MapHost.cs` (cho `SearchPill`; các kiểu `FilterBar`, `NavRail`, `AppStatusBar` ở trên đã viết đầy đủ tên nên không cần).

### 6.4 `FilterBar`, `NavRail`, `AppStatusBar` thành thẻ
**`FilterBar.cs`** (đọc lại dòng 73-86 và 141-146):
- trong `LayoutChips()`, sau vòng lặp, thêm `WantedWidth = x - Ui.S(ChipGap) + Ui.S(SidePadding);` và khai báo `public int WantedWidth { get; private set; }`;
- `Height = 48;` (dòng 55) → `40` và `ChipHeight = 30` giữ;
- `OnPaint`, **xóa** dòng 146 (`using (var pen = ...) g.DrawLine(pen, 0, Height - 1, Width, Height - 1);`): viền dưới của dải cũ.

**`NavRail.cs`:**
- `DesignWidth = 80` → `64` (dòng 24), `PillWidth = 56` → `44` (dòng 27);
- thêm `public int WantedHeight => Ui.S(TopMargin) * 2 + Icons.Length * Ui.S(ItemHeight);`;
- `OnPaint`: **xóa** dòng 130 (đường kẻ dọc bên phải).

**`AppStatusBar`** (`Shell/StatusBar.cs`, `StatusBar.Designer.cs`; tôi chưa đọc Designer): đọc `StatusBar.Designer.cs`, đổi `BackColor` sang `Color.White`, bỏ viền trên nếu có, cao 32 (`StatusBar.cs` bố trí theo `Height`/`Width` nên chạy được ở mọi cỡ). Nó vẫn đặt tên vùng (trái), thời tiết (giữa), tọa độ + tỉ lệ (phải) trong thẻ viên thuốc.

### 6.5 `MainForm.Designer.cs` và `MainForm.cs`: bỏ dock, nối thẻ nổi
`MainForm.Designer.cs`:
- dòng 17-19: `SideLeft = 92` → `88`; `SideTop = 116` (giữ: 40 thanh tiêu đề + 16 + 48 ô tìm kiếm + 12); `SideBottom = 40` → `56` (chừa chỗ cho thẻ trạng thái).
- dòng 70-71, 74-75, 77-78: **xóa** cấu hình `Dock/Size` của `filterBar`, `navRail`, `statusBar` (chúng sẽ được đặt bằng mã ở 6.3).
- dòng 84-85, 102: **xóa** `this.Controls.Add(this.navRail/filterBar/statusBar);`. Giữ `Add(titleBar)`.
- dòng 97: `tripResultPanel.AttachTo(... ClientSize.Height - TripResultPanel.DesignHeight - 56 ...)` đã chừa 56 px đáy, khớp `SideBottom = 56`.

`MainForm.cs`: trong hàm khởi tạo, **ngay sau** `InitializeComponent();` (đọc dòng ~50-58):
```csharp
mapHost.HostOverlays(this, searchPill, filterBar, navRail, statusBar);
OverlayCard.Attach(this, searchDropdown, ThemeMetrics.RadiusMd, Elevation.Panel);
WindowChrome.Attach(this);
```
và nối thời tiết lên thanh tiêu đề (cạnh dòng 164-165, `statusBar.SetWeather(...)`): thêm `titleBar.SetWeather(weatherUpdater.Text);` và `weatherUpdater.TextChanged += (s, text) => titleBar.SetWeather(text);`.

### 6.6 `WindowChrome`: cửa sổ chính
Chép `WindowChrome.cs` vào `Theme/` (đã gọi `Attach(this)` ở 6.5). Windows 11 bo ~8 px mượt; Windows 10 dùng `Region` 8 px; phóng to thì vuông. Giữ nguyên chuỗi xử lý phóng to của `MainForm.cs:94-104`.

### Kiểm tra bước 6
- Cửa sổ chính: thanh tiêu đề tím mảnh; **bản đồ tràn xuống tận mép** dưới thanh tiêu đề; ô tìm kiếm viên thuốc trên trái; hàng chip bên phải nó trong một viên thuốc trắng; thanh dọc 4 biểu tượng nằm dưới ô tìm kiếm; viên trạng thái ở đáy giữa.
- **Mỗi thẻ có bóng và cạnh mượt, góc không lộ chữ nhật nền.** Đây là chỗ cần nhìn kỹ: nếu thấy chữ nhật xám/trắng quanh thẻ, lớp bóng chưa phủ đúng (xem bảng lỗi ở Bước 4.3, hàng "Bóng phủ lên…" và "Bóng không đổi…").
- Gõ vào ô tìm kiếm: danh sách gợi ý hiện ngay dưới ô, bo 14 px, có bóng.
- Bấm từng mục thanh dọc: panel mở đúng chỗ (x ≈ 88), **không đè lên** ô tìm kiếm (panel bắt đầu từ y = 116).
- Thu hẹp cửa sổ tới 1100 px: chip bị thu gọn, không đè lên cột nút bên phải; ở dưới đó thì chưa xử lý (ghi chú sau).
- Phóng to và thu nhỏ cửa sổ, kéo cửa sổ: các thẻ không giật.

Việc **chưa làm** trong bước này: khi cửa sổ quá hẹp (< 1000 px) hàng chip cần cuộn ngang hoặc thu về nút "Lọc". Hiện nó chỉ co lại tối thiểu 120 px.

**Commit:** 6 commit nhỏ, mỗi bước con một cái.

---

## Bước 7. Các panel

### 7.1 `Panels/Shared/PoiRowList.cs`: danh sách có ảnh
Hiện mỗi hàng cao 60 px (`RowHeight`, dòng 40) với **huy hiệu tròn 38 px** (`BadgeSize`, dòng 41). Đổi thành hàng 72 px có **ảnh thu nhỏ 52 px bo `RadiusMd`**, ảnh đầu tiên của địa điểm, nếu không có thì ô màu nhóm + biểu tượng.

1. Tạo `Panels/Shared/PoiThumbs.cs` (**mã này tôi chưa biên dịch**, phụ thuộc `PoiPhotos`, `Poi`, `PoiMarkerIcons`):
   ```csharp
   using System.Collections.Generic;
   using System.Drawing;
   using System.Drawing.Drawing2D;
   using Hue_travel.MapEngine;
   using Hue_travel.Models;
   using Hue_travel.Services.Data;
   using Hue_travel.Theme;

   namespace Hue_travel.Panels.Shared
   {
       /// <summary>First photo of a place as a small rounded thumbnail, kept in a small cache (the file system is never asked while painting).</summary>
       internal static class PoiThumbs
       {
           private const int MaxEntries = 160;
           private static readonly Dictionary<string, Bitmap> Cache = new Dictionary<string, Bitmap>();   // key: id|size; a null value means "no photo"

           public static void Draw(Graphics g, Poi poi, Rectangle box, bool dim)
           {
               using (GraphicsPath clip = Ui.RoundedRect(box, Ui.S(ThemeMetrics.RadiusMd)))
               {
                   Bitmap photo = Find(poi.Id, box.Width);
                   GraphicsState state = g.Save();
                   g.SetClip(clip, CombineMode.Intersect);
                   if (photo != null)
                   {
                       // "cover": fill the box, cropping the longer side
                       float scale = System.Math.Max(box.Width / (float)photo.Width, box.Height / (float)photo.Height);
                       float w = photo.Width * scale, h = photo.Height * scale;
                       g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                       g.DrawImage(photo, box.X + (box.Width - w) / 2, box.Y + (box.Height - h) / 2, w, h);
                   }
                   else
                   {
                       using (var fill = new SolidBrush(Ui.Lerp(PoiMarkerStyle.ColorOf(poi.Category), Color.White, 0.82f))) g.FillRectangle(fill, box);
                       int icon = (int)(box.Width * 0.5);
                       g.DrawImage(IconFont.Get(PoiMarkerIcons.IconOf(poi.Category), icon, PoiMarkerStyle.ColorOf(poi.Category)),
                           box.X + (box.Width - icon) / 2, box.Y + (box.Height - icon) / 2, icon, icon);
                   }
                   g.Restore(state);
               }
               if (dim)
               {
                   using (GraphicsPath path = Ui.RoundedRect(box, Ui.S(ThemeMetrics.RadiusMd)))
                   using (var veil = new SolidBrush(Color.FromArgb(150, ThemeColors.Paper))) g.FillPath(veil, path);
               }
           }

           private static Bitmap Find(string id, int size)
           {
               string key = id + "|" + size;
               lock (Cache)
               {
                   Bitmap cached;
                   if (Cache.TryGetValue(key, out cached)) return cached;
                   if (Cache.Count > MaxEntries) Clear();
                   IReadOnlyList<string> paths = PoiPhotos.PathsFor(id);
                   Bitmap bmp = paths.Count > 0 ? PoiPhotos.Load(paths[0], size * 2) : null;
                   Cache[key] = bmp;   // PoiPhotos keeps its own (small) cache; the thumbnail is shared, never disposed here
                   return bmp;
               }
           }

           /// <summary>Forget what was found (call when the server's photos finished downloading).</summary>
           public static void Clear() { lock (Cache) Cache.Clear(); }
       }
   }
   ```
   Chú ý `PoiPhotos.Load` có bộ đệm riêng giới hạn 48 (`PoiPhotos.cs:55`) và **xóa sạch khi đầy**; ảnh nó trả về có thể bị `Dispose` khi đó. Vì vậy ở `Find` hãy **sao chép** ảnh nhỏ sang bitmap của riêng bạn (`new Bitmap(source)`) thay vì giữ tham chiếu, nếu thấy lỗi "Parameter is not valid" khi cuộn danh sách dài.
2. `PoiRowList.cs`:
   - dòng 40-41: `RowHeight = 72; ThumbSize = 52;` (thay `BadgeSize`).
   - dòng 300-303 thay bằng:
     ```csharp
     int sink = press.SinkFor(i);
     int thumb = Ui.S(ThumbSize);
     var thumbRect = new Rectangle(Ui.S(14), top + (RowPx - thumb) / 2 + sink, thumb, thumb);
     PoiThumbs.Draw(g, row.Poi, thumbRect, dim);
     ```
     và dòng 305 `int textLeft = thumbRect.Right + Ui.S(12);`
   - dòng 332 và 334: toạ độ chữ cho hàng cao 72: tên `top + Ui.S(15)`, dòng phụ `top + Ui.S(40)`; đổi `ThemeFonts.SemiBold(14)` giữ.
   - dòng 295-298 (thanh chọn bên trái, `Primary`) giữ; dòng 284 `rest` (`Sky50` chọn, `Slate50` rê) giữ, tự đổi tông.
3. Nơi ảnh tải xong (`PhotoSync`, hoặc nơi gọi `ReloadPhotos`): gọi `PoiThumbs.Clear()` rồi `Invalidate()` danh sách.

### 7.2 `Panels/PoiIdentify/PoiIdentifyCard.cs`: thẻ du lịch
Làm theo từng lớp, build sau mỗi lớp.

**(a) Màu và chữ (đơn giản):**
- `InfoView` dòng 72: `ThemeFonts.Bold(20)` → `ThemeFonts.Serif(22)` (tên địa điểm có chân). Dòng 74 `Slate900` giữ (đã thành `Ink`).
- Dòng 62-64: màu chip nhóm vẫn dùng `PoiMarkerStyle.ColorOf`, tự đổi.
- Dòng 128: biểu tượng dòng thông tin dùng `Primary` (nay tím); đổi nền biểu tượng thành ô tròn tím nhạt nếu muốn: vẽ `FillEllipse` màu `BrandSoft` kích thước `iconPx + Ui.S(10)` trước `DrawImage`.

**(b) Ảnh bìa chồng tên (`PhotoGallery.cs`):** đọc `PhotoGallery.OnPaint` (dòng 160-260, tôi chưa đọc kỹ). Sau khi vẽ ảnh bìa và **trước** khi vẽ nút mũi tên, thêm lớp tối ở đáy ảnh và tên:
```csharp
Rectangle hero = HeroRect;
var shade = new Rectangle(hero.X, hero.Bottom - hero.Height * 45 / 100, hero.Width, hero.Height * 45 / 100);
using (var brush = new LinearGradientBrush(new Rectangle(shade.X, shade.Y - 1, shade.Width, shade.Height + 2),
    Color.FromArgb(0, ThemeColors.Ink), Color.FromArgb(190, ThemeColors.Ink), LinearGradientMode.Vertical))
{
    g.FillRectangle(brush, shade);
}
TextRenderer.DrawText(g, Title ?? "", ThemeFonts.Serif(22), new Rectangle(hero.X + Ui.S(18), hero.Bottom - Ui.S(54), hero.Width - Ui.S(36), Ui.S(44)), Color.White,
    TextFormatFlags.Left | TextFormatFlags.Bottom | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
```
cùng `public string Title { get; set; }` trong `PhotoGallery`. `PoiIdentifyCard.ShowPoi` (dòng 214-223) gán `gallery.Title = poi.Name;`; **khi đó bỏ dòng tên trong `InfoView`** (dòng 71-75) để khỏi in hai lần, và trừ độ cao tương ứng ở `Run`.
Với địa điểm **không có ảnh**, `PhotoGallery` vẽ ô màu + huy hiệu: giữ, nhưng cũng cần có vùng tối để chữ trắng đọc được (nền ô đó nhạt): với nhánh này hãy vẽ tên bằng `Ink`.

**(c) Hàng nút hành động (thay hàng Yêu thích / Chủ quản / Báo sai):** hiện `LayoutCard` (dòng 272-279) xếp ba nút chữ khi đã đăng nhập. Làm lại thành hàng nút **tròn có biểu tượng + nhãn nhỏ** như Google Maps: Chỉ đường (`directions`), Thêm vào lộ trình, Lưu (`favorite`), Chia sẻ (`share`, sao chép địa chỉ), Báo sai (`campaign`). Mỗi nút: `IconButton { Kind = Tonal, Stacked = true, IconSize = 20, Radius = ThemeMetrics.RadiusMd }` (`Stacked` có sẵn: biểu tượng trên, chữ dưới, `IconButton.cs:153-170`), cao 64, chia đều chiều ngang.
**Quy tắc khách** (Mục 1 của `ke-hoach-chinh-sua.md`): các nút cần tài khoản **vẫn hiện** cho khách; `RefreshAccountButtons` (dòng 240-248) đổi `bool visible = signedIn && shown != null;` thành `shown != null`, và bấm khi chưa đăng nhập thì mở form đăng nhập.

**(d) Thẻ trong cả cửa sổ:** `DesignWidth = 400` giữ; đáy phải: panel anchor `Bottom | Right`, offset 1044/198 ở `MainForm.Designer.cs:100`: kiểm tra không đè cột nút bên phải (cột ~44 px + lề 16).

### 7.3 Các panel còn lại (làm theo mẫu)
`Panels/Explore/ExplorePanel.cs`, `Panels/Layers/LayerPanel.cs`, `Panels/RouteOptimizer/RouteOptimizerPanel.cs`, `TripResultPanel.cs`, `StopList.cs`, `Panels/TravelGuide/*`, `Account/AccountPanel.cs`, `Account/ProfileMenu.cs`, `Account/ReviewsDialog.cs`, `Admin/*`. Với mỗi tệp:
1. `BackColor = Color.White` của thân → `ThemeColors.Paper`.
2. Mọi `Radius = N` / `Ui.RoundedRect(..., Ui.S(N))` theo bảng 7.4.
3. Nút: dùng `Kind` thay vì gán màu thủ công.
4. Tiêu đề mục (`Caption(...)` như `TravelGuidePanel.cs:97-98` dùng `Bold(15)`): `ThemeFonts.Serif(16)`.
5. Danh sách dùng `PoiRowList` tự có ảnh nhờ 7.1.
6. Trang trống: hiện chỉ có chữ; thêm biểu tượng 40 px màu `Slate400` phía trên dòng chữ (không cần minh họa).

### 7.4 Bảng bo góc cần sửa (từ `Grep`, đã gom theo tệp)
Ý nghĩa cột "Đổi thành": `RS`=`ThemeMetrics.RadiusSm`(10), `RM`=`RadiusMd`(14), `RL`=`RadiusLg`(20), `RF`=`RadiusFull`. Với `.Radius = N` dùng thẳng `RS`..; với `Ui.RoundedRect(.., Ui.S(N))` dùng `Ui.S(RS)`..

| Tệp:dòng | Hiện tại | Đổi thành |
|---|---|---|
| `Panels/RouteOptimizer/TripResultPanel.cs:149` | `Ui.S(12)` (thẻ) | `RM` |
| `…/TripResultPanel.cs:273` | `btnStop.Radius = 10` | `RS` |
| `…/TripResultPanel.cs:303` | `b.Radius = 14` | `RM` |
| `…/TripResultPanel.cs:315` | `b.Radius = 10` | `RS` |
| `…/RouteOptimizerPanel.cs:199` | `b.Radius = 10` | `RS` |
| `…/RouteOptimizerPanel.cs:215` | `b.Radius = 12` | `RS` |
| `Panels/TravelGuide/TourList.cs:190` | `Ui.S(12)` (thẻ tour) | `RM` |
| `…/TourList.cs:250` | `Ui.S(10)` (nút dùng) | `RS` |
| `…/InfoRows.cs:154` | `Ui.S(8)` | `RS` |
| `Panels/PoiIdentify/PoiIdentifyCard.cs:179` | `btnClose.Radius = 17` | giữ (nút tròn 34 px) |
| `…/PoiIdentifyCard.cs:191` | `btnRoute.Radius = 12` | `RS` (hoặc để `Kind`) |
| `…/PoiIdentifyCard.cs:298` | `Radius = 10` | `RS` |
| `…/PhotoGallery.cs:239` | `Ui.S(8)` (ảnh nhỏ) | `RM` |
| `…/PhotoViewer.cs:123` | `Ui.S(10)` | `RM` |
| `Account/PhotoGrid.cs:158` | `Ui.S(10)` | `RM` |
| `Account/ProfileMenu.cs:134` | `Ui.S(10)` | `RS` |
| `Account/AccountKit.cs:285` | `Ui.S(10)` | `RS` |
| `Account/Dialogs.cs:96, 104` | `Ui.S(14)` (hộp thoại) | `RL` |
| `Admin/ApprovalsSection.cs:50` | `Ui.S(10)` | `RS` |
| `Shell/SearchDropdown.cs:99, 123` | `Ui.S(14)` | `RM` (đã là 14) |
| `MapEngine/Overlays/MapToolStack.cs:122` | `Ui.S(8)` | `RM` |
| `MapEngine/Overlays/CompassButton.cs:51` | `Ui.S(8)` | `Width / 2f` (tròn) |
| `MapEngine/MapHost.cs:660` | `tripChip.Radius = 23` | giữ (viên thuốc 46 px) |
| `Welcome/LoginForm.cs:211, 230, 261` | 14 / 14 / 12 | `RS`/`RM` |
| `Welcome/BrandCheckBox.cs:54` | `Ui.S(5)` | giữ (ô tích nhỏ) |

Sau khi sửa: `Grep "Radius\s*=\s*\d"` và `Grep "Ui\.S\(\d+\)\)\)"` quanh `RoundedRect` chỉ còn các ngoại lệ ghi "giữ".

### Kiểm tra bước 7
Mở lần lượt: danh sách Khám phá (có ảnh thu nhỏ, cuộn mượt, không giật, không lỗi bitmap khi cuộn danh sách dài); thẻ địa điểm có ảnh và không có ảnh; panel Lộ trình; Hồ sơ; hộp thoại đánh giá. Mỗi nơi: góc bo đúng thang, không còn xanh trời, chữ tên địa điểm là serif.

**Commit:** theo từng panel.

---

## Bước 8. Bản đồ du lịch

### 8.1 Màu nền bản đồ (`MapEngine/LocalBaseMap.cs`)
Chỉ đổi màu, không đổi dữ liệu. Sửa các dòng sau (tìm theo nội dung):

| Dòng | Hiện tại | Đổi thành | Lý do |
|---|---|---|---|
| 78 | `StyleArea(provinces, (243,239,230), (167,139,250), 1.8f);` | `(243,239,230), (206,196,178), 1.0f` | bỏ viền tím nổi, dùng nâu ấm mảnh |
| 79 | `provinces.DefaultDrawingOptions.LineStipple = tkDashStyle.dsDash;` | **xóa dòng** | viền liền, mảnh |
| 80 | màu nhãn tỉnh `(91,42,134)` | `(94,84,112)` | `InkSoft`, dịu hơn |
| 87 | `StyleArea(communes, ..., (188,176,214), 1.2f);` | `..., (226,218,203), 0.8f` | ranh giới xã gần như chìm |
| 88 | màu nhãn xã `(148,163,184)` | `(125,114,140)` | đủ tương phản trên nền be |
| 95 | `StyleArea(lakes, (165,212,238), (140,196,228), 1f);` | `(170,214,226), (143,196,212), 1f` | nước ngả ngọc (sông Hương) |
| 104 | màu sông nhỏ `(150,202,232)` | `(143,196,212)` | khớp mặt nước |
| 265 | màu đường "khác" `(214,217,224)` | `(230,223,210)` | xám ấm |
| 270 | viền đường trung `(185,192,203)` | `(205,196,180)` | xám ấm |
| 271 | viền đường nhỏ `(206,211,219)` | `(221,213,198)` | xám ấm |
| `MapHost.cs:608` | `axMap.BackColor = "#E2E8F0"` | `"#CFE8F5"` | cùng màu biển, không lóe xám lúc tải |

Giữ: màu đất `(243,239,230)` (đã gần `PaperDeep`), đường chính vàng (dòng 269), biển, các lớp nước ngoài.
**Cách làm:** đổi vài dòng, chạy, so ảnh ở ba mức thu phóng (cả tỉnh, thành phố, khu Đại Nội), rồi chỉnh tiếp. Đừng đổi hết một lần.

Phần "vùng di sản vàng" và "cây xanh" trong kế hoạch: **chỉ làm nếu có dữ liệu**. Hỏi nguồn dữ liệu trước; không có thì bỏ, đừng vẽ đa giác ước lượng rồi gán tên.

### 8.2 Ghim hình giọt
**Bắt đầu bằng việc xác minh điểm neo**, vì mã hiện tại tự mâu thuẫn: `MapHost.cs:218` ghi chú ảnh "đứng trên điểm" (đáy giữa là địa điểm), nhưng nhãn tên ở `:322` đặt cách điểm `MarkerDesignPixels/2` (như thể ảnh nằm **giữa** điểm). Hai cách đặt cần hai ảnh khác nhau, nên phải biết trước.

**Thử neo (khoảng 15 phút):** trong `AddMarkerLayer` (`MapHost.cs:250`), tạm thêm ngay trước `int handle = axMap.AddLayer(layer, true);` một lớp chấm đen 4 px **cùng tọa độ**:
```csharp
// TẠM: chấm đen đúng chỗ địa điểm, để so với ảnh ghim
var dots = new Shapefile();
if (dots.CreateNew(string.Empty, ShpfileType.SHP_POINT))
{
    foreach (Poi poi in pois) { var p = new Shape(); if (p.Create(ShpfileType.SHP_POINT)) { p.AddPoint(poi.X, poi.Y); dots.EditAddShape(p); } }
    dots.DefaultDrawingOptions.PointType = tkPointSymbolType.ptSymbolStandard;
    dots.DefaultDrawingOptions.PointShape = tkPointShapeType.ptShapeCircle;
    dots.DefaultDrawingOptions.PointSize = 4;
    dots.DefaultDrawingOptions.FillColor = 0;   // đen
    axMap.AddLayer(dots, true);
}
```
Phóng to hết cỡ trên một địa điểm. Nhìn chấm đen so với ảnh tròn hiện tại:
- chấm đen nằm ở **đáy giữa** ảnh → ảnh "đứng trên điểm" → dùng ghim như `PoiPin.Draw` trả về;
- chấm đen nằm ở **tâm** ảnh → ảnh "đặt giữa điểm" → dùng `PoiPin.PadForCentreAnchor(pin)` (ghim ở nửa trên của ảnh gấp đôi chiều cao, mũi ghim rơi đúng tâm).
Xóa đoạn tạm sau khi biết kết quả.

**Làm thật:**
1. Chép `PoiPin.cs` vào `MapEngine/`; khai báo csproj.
2. `MapEngine/PoiMarkerIcons.cs`, thêm hàm (cạnh `FilePath`, dòng ~70):
   ```csharp
   /// <summary>The teardrop pin of a group saved as a PNG for the map engine; null when it cannot be written.</summary>
   public static string PinFilePath(PoiCategory category, int px, bool centreAnchor)
   {
       try
       {
           string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HueTravel", "markers");
           Directory.CreateDirectory(folder);
           // the file name says what it is: a changed drawing must use a new name, or the old PNG is reused
           string path = Path.Combine(folder, "pin2_" + category.ToString().ToLowerInvariant() + "_" + px + (centreAnchor ? "_c" : "") + ".png");
           if (!File.Exists(path))
           {
               using (Bitmap pin = PoiPin.Draw(px, PoiMarkerStyle.ColorOf(category), IconFont.Get(IconOf(category), px, Color.White)))
               using (Bitmap result = centreAnchor ? PoiPin.PadForCentreAnchor(pin) : (Bitmap)pin.Clone())
               {
                   result.Save(path, ImageFormat.Png);
               }
           }
           return path;
       }
       catch (Exception ex)
       {
           System.Diagnostics.Debug.WriteLine("PoiMarkerIcons.PinFilePath: " + ex.Message);
           return null;
       }
   }
   ```
   Lưu ý tên tệp: ảnh cũ nằm ở `%LOCALAPPDATA%\HueTravel\markers\`; nếu bạn chỉnh hình ghim sau này, **đổi tiền tố `pin2_`** hoặc xóa thư mục, nếu không bản cũ vẫn được dùng.
   `PoiMarkerIcons.Badge(...)` (tròn, vuông) **giữ nguyên**: danh sách, thư viện ảnh, chip còn dùng nó.
3. `MapEngine/MapHost.cs`:
   - dòng 45: `MarkerDesignPixels = 34` giờ là **chiều rộng** ghim; ghim cao `PoiPin.HeightOf(px)` ≈ 1.28 lần.
   - dòng 285: `UsePicture(style, PoiMarkerIcons.PinFilePath(category, Ui.S(MarkerDesignPixels), CENTRE))` với `CENTRE` là kết quả thử neo (`true`/`false`).
   - dòng 206: `new HighlightRing(Ui.S(HighlightDesignPixels * 2), PoiPin.HeadDiameter(Ui.S(MarkerDesignPixels)), HighlightCenterOnScreen)`.
   - dòng 218-219 (`HighlightCenterOnScreen`): tâm vòng sáng = **tâm đầu ghim**, cao hơn mũi `HeadCenterAboveTip`:
     ```csharp
     var local = new System.Drawing.Point((int)Math.Round(px), (int)Math.Round(py - PoiPin.HeadCenterAboveTip(Ui.S(MarkerDesignPixels))));
     ```
   - dòng 322 (nhãn tên): ghim đứng trên điểm nên nhãn ở **ngay dưới mũi**: `labels.OffsetY = Ui.S(2);` nếu neo là "đáy"; nếu neo là "tâm" thì nhãn phải xuống dưới nửa ảnh gấp đôi: `OffsetY = PoiPin.HeightOf(Ui.S(MarkerDesignPixels)) / 2 + Ui.S(2)`. Kiểm tra bằng mắt.
   - dòng 339-355 (`FindPoiAt`): người dùng bấm vào **đầu ghim**, không phải mũi. Thay phần tính khoảng cách:
     ```csharp
     double head = PoiPin.HeadCenterAboveTip(Ui.S(MarkerDesignPixels));
     double radius = Math.Max(Ui.S(HitRadius), PoiPin.HeadDiameter(Ui.S(MarkerDesignPixels)) / 2.0 + Ui.S(3));
     double bestSquared = radius * radius;
     // trong vòng lặp:
     double dx = px - pixelX, dy = (py - head) - pixelY;   // khoảng cách tới tâm đầu ghim
     double squared = dx * dx + dy * dy;
     ```
     (và để chắc người bấm trúng mũi ghim cũng được: lấy `Math.Min(squared, khoảng-cách-tới-mũi²/4)` nếu muốn.)

**Kiểm tra bước 8.2:** zoom gần: mũi ghim chạm đúng điểm (so với chấm đen tạm); đầu ghim mang đúng biểu tượng nhóm; bấm vào đầu ghim mở thẻ địa điểm; vòng sáng quanh địa điểm đã chọn nằm quanh **đầu** ghim; nhãn tên không đè lên ghim; ghim **không bị cắt** ở rìa bản đồ.

### 8.3 Lộ trình (`MapEngine/RoutePalette.cs`, `RouteDrawing.cs`)
Đã có viền trắng, huy hiệu số, xe quay hướng. Chỉ đổi:

1. **Bảng màu** `RoutePalette.cs:8-18`, hài hòa với tím Huế, **không có màu đỏ** (dành cho điểm xuất phát):
   ```csharp
   Color.FromArgb(91, 42, 134),    // tím Huế
   Color.FromArgb(15, 118, 110),   // ngọc
   Color.FromArgb(194, 65, 12),    // cam đất
   Color.FromArgb(57, 73, 171),    // chàm
   Color.FromArgb(161, 98, 7),     // nâu vàng
   Color.FromArgb(190, 24, 93),    // sen
   Color.FromArgb(77, 124, 15),    // rêu
   Color.FromArgb(8, 145, 178),    // xanh lơ
   ```
2. **Điểm xuất phát** `RouteDrawing.cs:254`: `Color.FromArgb(22, 163, 74)` → `ThemeColors.Lacquer` (đỏ son), nhãn "BĐ" giữ.
3. **Nét dày hơn** `:196`: `edge = Ui.S(9), line = Ui.S(5)` → `edge = Ui.S(11), line = Ui.S(7)`.
4. **Mũi tên chỉ hướng** (mã **chưa biên dịch**, đặt trong `RouteDrawing`). Gọi `DrawArrows(badgeDrawing, mpp);` ngay sau vòng `for` vẽ huy hiệu trong `DrawBadges` (sau dòng 258), vì lớp này được vẽ lại mỗi khi đổi thu phóng (đúng thứ ta cần: mũi tên cỡ pixel cố định):
   ```csharp
   // White chevrons along the route every ~110 px so the direction of travel reads at a glance.
   private void DrawArrows(int layer, double mpp)
   {
       if (points.Count < 2 || total <= 0 || mpp <= 0) return;
       double spacing = Ui.S(110) * mpp;                      // metres between arrows at this zoom
       int count = (int)Math.Min(400, total / spacing);       // never hundreds of polygons when zoomed far in
       PointF[] chevron =
       {
           new PointF(Ui.S(5f), 0), new PointF(-Ui.S(3.5f), -Ui.S(4.5f)), new PointF(-Ui.S(1f), 0), new PointF(-Ui.S(3.5f), Ui.S(4.5f)),
       };
       for (int i = 1; i <= count; i++)
       {
           double x, y, heading;
           if (!Locate(i * spacing, out x, out y, out heading)) break;
           Polygon(layer, chevron, x, y, heading, mpp, Color.White, 235);
       }
   }
   ```
   (`Polygon(layer, pixels, cx, cy, heading, mpp, color, alpha)` và `Locate(...)` có sẵn, dòng 408 và 485.) Mũi tên **không** nên rơi sát huy hiệu điểm dừng: bỏ qua mũi tên cách chặng `legEnds[k]` dưới 30 px.

### 8.4 Điều khiển trên bản đồ
- `MapToolStack.cs:122` bo `RM` (đã trong bảng 7.4); các nút `ActiveFill/ActiveIconColor` (`MapHost.Designer.cs:57-58, 66-67` dùng `Sky50/Sky600`) tự đổi tím.
- `CompassButton.cs:51`: bo thành tròn (`Width / 2f`); kim bắc đổi màu `Lacquer` (đọc tệp để tìm chỗ vẽ kim).
- `MapHost.Designer.cs:45-46`: biểu tượng nút nền bản đồ `"map,satellite_alt,layers,explore"` gắn với bốn nền của Google (Mục 2 của `ke-hoach-chinh-sua.md` gỡ chúng). **Làm Mục 2 trước** rồi mới thiết kế thẻ chuyển nền có ảnh thu nhỏ; trong lúc chờ, giữ nguyên cột này.

**Commit:** "Giao diện: màu bản đồ, ghim giọt, lộ trình".

---

## Bước 9. Hoàn thiện và nghiệm thu

### 9.1 Chuyển động
- Panel: đã có trượt + mờ (Bước 4).
- Thanh dọc/Chip: `PressFeedback` đã có gợn sóng và lún; giữ.
- Toast (`Theme/Toast.cs`, tôi chưa đọc): bo `RL`, thêm biểu tượng theo loại; trượt từ trên xuống 200 ms nếu `SystemInformation.UIEffectsEnabled`.
- Tải ảnh: thay chữ "Đang tải…" bằng khối xám nhấp nháy (`Skeleton`) nếu muốn; việc phụ, làm sau cùng.

### 9.2 Quét lần cuối
```
Grep  Radius\s*=\s*\d                      -> chỉ còn ngoại lệ đã ghi "giữ"
Grep  Color\.FromArgb\(\d+, ?\d+, ?\d+\)   -> chỉ còn LocalBaseMap, RoutePalette, RouteDrawing (xe), màu hiệu ứng
Grep  ThemeColors\.(Sky|Slate)             -> còn dùng được (đã là tím/ấm); không bắt buộc đổi tên
Grep  BackColor = Color\.White             -> nền panel/danh sách đã là Paper
```

### 9.3 Ma trận nghiệm thu
| Kiểm tra | Cách làm | Đạt khi |
|---|---|---|
| Bo góc | Phóng ảnh chụp 300% quanh panel, thẻ, nút, chip, hộp thoại, toast | không còn góc vuông (trừ cửa sổ chính phóng to) |
| Tông màu | Mở mọi màn | không còn xanh trời; marker và chip cùng màu nhóm |
| Chữ | Mở thẻ địa điểm, danh sách, panel | dấu tiếng Việt đủ, không cắt chữ ở 150% DPI |
| Hiệu năng | Kéo bản đồ liên tục 10 giây, mở/đóng panel liên tục | không giật rõ rệt; CPU không cao hơn bản trước đáng kể |
| Cửa sổ | Thu nhỏ, phóng to, kéo, đổi màn hình | bóng đi theo, không còn bóng "ma" |
| DPI | 100%, 125%, 150% | không lệch, không mờ |
| Chế độ | Khách, đăng nhập, ngoại tuyến | đều hiển thị đúng |
| Hoạt ảnh | Tắt hiệu ứng Windows (Cài đặt → Trợ năng → Hiệu ứng hình ảnh) | panel hiện tức thì |
| Bàn phím | Tab qua nút; Enter, Esc ở ô tìm kiếm | vẫn hoạt động (không đổi hành vi) |
| Lùi được | `SoftChrome = false` | về cách cũ, không lỗi |

---

## Phụ lục A. Việc chưa có lời giải trong hướng dẫn này

1. **Thẻ chuyển nền bản đồ có ảnh thu nhỏ**: phụ thuộc Mục 2 (gỡ Google); chưa có nền nào khác ngoài bản đồ cục bộ để chuyển, nên có thể thẻ này không còn cần.
2. **Gom cụm điểm** khi thu nhỏ: chưa kiểm tra MapWinGIS làm được không (`Shapefile` có `Clustering` trong một số bản; **chưa xác minh** cho bản bạn dùng). Hiện nhóm ẩn theo tỉ lệ (`LabelMaxScaleDenominator`); đủ dùng.
3. **Vùng di sản và cây xanh trên bản đồ**: cần dữ liệu.
4. **Cửa sổ rất hẹp** (< 1000 px): hàng chip cần cuộn ngang; chưa viết.
5. **Chế độ tối**: chưa làm; token đã tách nên làm được sau.
6. **Ảnh địa điểm**: kiểm tra giấy phép trước khi coi ảnh là nội dung chính của thẻ và danh sách.

## Phụ lục B. Khi mắc kẹt

| Triệu chứng | Xem |
|---|---|
| "type or namespace not found" sau khi chép tệp | Chưa khai báo `<Compile Include>` trong csproj (mục 0.3) |
| Panel không hiện bóng | `FloatingPanelForm.SoftChrome` đang `false`; hoặc panel chưa gọi `AttachTo/AttachToSide` |
| Bóng phủ lên nội dung | Bảng lỗi ở Bước 4.3 |
| Chữ nhật lạ quanh ô tìm kiếm/chip | Điều khiển nền không đặc, hoặc `OverlayCard.Attach` chưa gọi cho nó |
| Ghim lệch khỏi điểm | Chưa làm "thử neo" ở 8.2, hoặc dùng sai biến thể `CENTRE` |
| Marker không đổi sau khi sửa hình | Tệp PNG cũ trong `%LOCALAPPDATA%\HueTravel\markers\`; đổi tiền tố `pin2_` hoặc xóa thư mục |
| Chữ serif không hiện | Tên họ font trong `.ttf` khác `Lora SemiBold`; xem Bước 2, mục kiểm tra |
| Cửa sổ chính còn cạnh vuông trên Windows 10 | Đúng thiết kế: `Region` 8 px; bo lớn chỉ áp cho thẻ bên trong |
