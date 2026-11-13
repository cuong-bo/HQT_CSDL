# Kế hoạch thiết kế lại giao diện toàn dự án (phong cách WebGIS du lịch Huế)

> **Cập nhật:** phần *làm thế nào* (từng bước, từng tệp, mã mẫu đã biên dịch thử) nằm ở [`huong-dan-trien-khai-giao-dien.md`](huong-dan-trien-khai-giao-dien.md), mã mẫu ở `ma-mau-giao-dien/`. Sau khi đọc nốt mã, **sáu điểm trong tài liệu này đã được đính chính** (mục 0.1 của hướng dẫn): điểm đánh dấu thực ra đã là ảnh PNG chứ không phải chấm 12 px; lộ trình đã có viền trắng, huy hiệu số và xe quay hướng; cửa sổ chính chỉ bo nhỏ được (~8 px), bo lớn chỉ áp cho thẻ bên trong; chip lọc phải nằm trong một viên thuốc chung; biểu tượng mới vẽ bằng GDI chứ không sửa font; kỹ thuật lớp phủ trên bản đồ đã được `HighlightRing` chứng minh. Khi hai tài liệu khác nhau, **theo hướng dẫn triển khai**.

Tài liệu để bạn tự code. Đường dẫn tính từ `may-khach/ung-dung/` trừ khi ghi khác. Phần nào tôi chưa đọc mã thì ghi "đọc trước khi sửa". Kế hoạch này **chung đường** với ba kế hoạch trước (xem Mục 11), nên đọc Mục 11 trước khi bắt đầu.

---

## 1. Chẩn đoán: vì sao giao diện hiện tại trông "cũ"

Tôi đã đọc `Theme/*`, `Shell/*`, `MapEngine/*`, `Panels/*`. Các nguyên nhân thật, theo mức ảnh hưởng:

| # | Vấn đề | Bằng chứng trong mã | Hậu quả nhìn thấy |
|---|---|---|---|
| 1 | **Bố cục kiểu phần mềm desktop, không phải WebGIS.** Ba dải ngang/dọc đặc cắt bản đồ: `TitleBar` 56 + `FilterBar` 48 + `NavRail` 80 + `AppStatusBar` 28, đều `Dock` và sát mép | `Shell/MainForm.Designer.cs:66-80` | Bản đồ chỉ là phần còn lại ở giữa, không tràn mép như Google Maps/Mapbox |
| 2 | **Bo góc nhỏ và không thống nhất.** Panel nổi cố định 8 px; các nơi khác rải 4, 6, 10, 12, 14, 17, 19 | `FloatingPanelForm.CornerRadius = 8`; `ThemeMetrics` chỉ có `RadiusSm=4, RadiusLg=8`; `Radius = 10/12/14/17` rải ở `PoiIdentifyCard`, `TripResultPanel`, `RouteOptimizerPanel`, `RoundedPanel` | Cảm giác "vuông vức", thiếu thống nhất |
| 3 | **Bán kính panel bị khóa bởi Windows 11.** Mã bật `DWMWCP_ROUND`, mà hệ thống chỉ cho đúng ~8 px. Trên Windows 10 thì rơi về `Region` (răng cưa, mất bóng đổ) | `FloatingPanelForm.cs:208-248` | Không thể bo 16–24 px như web; Win10 xấu hơn Win11 |
| 4 | **Hai bảng màu cạnh tranh.** Ứng dụng dùng xanh trời (`Primary #0284C7`); màn đăng nhập dùng tím (`BrandPurple #5B2A86`); đường ranh giới tỉnh trên bản đồ lại tím nhạt | `ThemeColors.cs`, `LocalBaseMap.cs:78` | Không có nhận diện thương hiệu; chưa nói lên "Huế" |
| 5 | **Bản đồ nền "dữ liệu thô".** Màu phẳng, ranh giới xã/tỉnh nổi, nhãn không có viền chữ, không có vùng di sản, không có icon địa danh | `MapEngine/LocalBaseMap.cs` | Trông như bản đồ GIS học thuật, không phải bản đồ du lịch |
| 6 | **Điểm đánh dấu là chấm tròn 12 px phẳng** | `PoiMarkerStyle.DiameterPixels = 12` | Khó nhận diện loại, không có chiều sâu, không có gom cụm |
| 7 | **Thẻ địa điểm và danh sách thiên về chữ.** Thẻ có ảnh lớn 196 px nhưng danh sách (`PoiRowList`) không có ảnh nhỏ | `PhotoGallery.cs:20`, `Panels/Shared/PoiRowList.cs` | Thiếu cảm giác "du lịch" (ảnh là nội dung chính) |
| 8 | **Lộ trình vẽ đơn giản**: màu theo chặng, chưa có viền trắng, mũi tên, nhãn thứ tự | `RoutePalette.cs`, `RouteDrawing.cs` | Khó đọc khi đường dày đặc |
| 9 | **Hiệu ứng mở panel chỉ là mờ dần** (`Opacity` 0→1) | `FloatingPanelForm.StepFade` | Thiếu chuyển động trượt/nảy hiện đại |

**Một ràng buộc kỹ thuật quyết định cách làm** (đã có chú thích trong mã): bản đồ là ActiveX MapWinGIS, nên điều khiển con đặt đè lên bản đồ chỉ **vẽ đặc, không mờ**. Vì vậy các panel lớn là `Form` cửa sổ riêng (`FloatingPanelForm`) để có bóng đổ; còn nút zoom/la bàn là điều khiển con trong `MapHost` (`MapHost.Designer.cs:14-85`) và phải đặc. Mọi đề xuất dưới đây đều phải đi qua ràng buộc này.

---

## 2. Hai hướng, và quyết định

| | Hướng A: giữ WinForms + MapWinGIS, thiết kế lại lớp vỏ | Hướng B: thay bản đồ bằng MapLibre/Leaflet trong WebView2 |
|---|---|---|
| Giống WebGIS | Gần (nếu làm kỹ) | Hoàn toàn (đúng công nghệ) |
| Công sức | Vừa (vài tuần) | Rất lớn: viết lại `MapHost`, `RouteDrawing`, marker, vẽ chặng, đổi hệ tọa độ VN-2000 → WGS84, dựng gạch vector từ shapefile |
| Rủi ro | Thấp, từng bước, chạy được liên tục | Cao, gần như làm lại ứng dụng |
| Hợp với đồ án (CSDL không gian, VN-2000, MapWinGIS) | Giữ nguyên điểm nhấn kỹ thuật | Mất |

**Quyết định: Hướng A.** Hướng B chỉ nên cân nhắc nếu sau Giai đoạn 5 bạn vẫn thấy chưa đạt và có thời gian làm lại. Nếu thầy cô/đề bài yêu cầu rõ MapWinGIS và CRS VN-2000 thì Hướng B loại hẳn.

---

## 3. Định hướng thiết kế: "Huế · Cố đô"

### 3.1 Tham chiếu phong cách (chung, không sao chép)
- **Bản đồ tràn mép + thẻ nổi**: thanh tìm kiếm dạng viên thuốc ở góc trên trái, hàng chip lọc nổi bên dưới, thẻ chi tiết trượt vào từ trái (Google Maps).
- **Ảnh là nhân vật chính**: thẻ có ảnh bìa lớn, huy hiệu "Đang mở", đánh giá sao (TripAdvisor, Airbnb Map).
- **Bản đồ nền được "chải" màu**: ít nhiễu, đường có viền, nhãn có quầng sáng (Mapbox Streets, Apple Maps).
- **Lộ trình rõ ràng**: nét dày có viền trắng, điểm dừng đánh số, thời gian ở mỗi chặng (Komoot, Citymapper).

### 3.2 Bản sắc Huế (làm nên chữ "đặc sắc")
| Yếu tố | Cách dùng |
|---|---|
| **Tím Huế** (áo dài, sắc tím cung đình) | Màu thương hiệu chính, thay xanh trời: nút chính, tab đang chọn, thanh tiêu đề |
| **Vàng cung đình** | Màu nhấn: huy hiệu, ngôi sao đánh giá, viền vùng di sản, tiêu đề mục |
| **Đỏ son** | Dành cho cảnh báo, "Đã đóng cửa", điểm xuất phát |
| **Xanh ngọc sông Hương** | Màu phụ cho nước, thời tiết, nhóm cảnh quan |
| **Giấy dó ấm** | Nền trung tính hơi ấm thay cho xám lạnh `Slate` |
| **Hoa văn hồi văn / cửa sổ lồng đèn** | Họa tiết rất nhạt ở đầu panel và trang trống, tuyệt đối không phủ lên chữ |
| **Chữ có chân cho tiêu đề** | Một font serif hỗ trợ tiếng Việt (ví dụ Lora hoặc Playfair Display) cho tên địa điểm và tiêu đề lớn; thân chữ vẫn là Plus Jakarta Sans đang dùng |
| **Lời chào theo giờ** | "Chào buổi sáng, Huế hôm nay 27°C" ở góc trên, dùng dữ liệu thời tiết sẵn có |

### 3.3 Bảng màu đề xuất (đặt trong `ThemeColors`, thay dần tên cũ)

| Token | Mã | Dùng cho |
|---|---|---|
| `Brand` | `#5B2A86` (đã có `BrandPurple`) | nút chính, tab chọn, thanh tiêu đề |
| `BrandDark` | `#3F1D63` | hover/pressed, nền gradient đầu panel |
| `BrandSoft` | `#F3ECF9` (đã có) | nền chip chọn, vùng nhấn nhẹ |
| `Gold` | `#C99A2E` | huy hiệu, sao, viền di sản |
| `GoldSoft` | `#FBF3DC` | nền huy hiệu vàng |
| `Lacquer` | `#B3261E` | lỗi, đóng cửa, điểm xuất phát |
| `Jade` | `#0F766E` | nước, cảnh quan, trạng thái tốt |
| `Paper` | `#FBF8F3` | nền thẻ/panel (thay trắng tinh) |
| `PaperDeep` | `#F2EBDD` | nền mục phụ, đất trên bản đồ |
| `Ink` | `#241B2F` | chữ chính (tím rất đậm thay `Slate900`) |
| `InkSoft` | `#5E5470` | chữ phụ |
| `Line` | `#E7DFD0` | đường kẻ, viền |

Màu nhóm địa điểm (marker, chip, danh sách phải **cùng một nguồn**): Di tích = Vàng đậm `#B7791F`, Cảnh quan = Ngọc `#0F766E`, Ẩm thực = Cam đất `#C2410C`, Lưu trú = Xanh chàm `#3949AB`, Dịch vụ = Tím `#7B3FA0`, Giao thông = Xám đá `#475569`. Kiểm độ tương phản chữ trắng trên từng màu (≥ 4.5:1).

> Việc đổi màu phải **đi qua token**. Hiện nhiều nơi gọi thẳng `ThemeColors.Sky600/Slate700…`. Đổi nội dung các token cũ trước (để nhanh thấy hiệu quả), rồi dần chuyển sang tên mới.

### 3.4 Thang bo góc, bóng đổ, khoảng cách (đặt trong `ThemeMetrics`)

| Token | Giá trị (px thiết kế) | Dùng cho |
|---|---|---|
| `RadiusXs` | 6 | ô nhỏ, tag |
| `RadiusSm` | 10 | nút, ô nhập, hàng danh sách |
| `RadiusMd` | 14 | thẻ nhỏ, ảnh thumbnail |
| `RadiusLg` | 20 | **panel nổi, thẻ chi tiết** |
| `RadiusXl` | 28 | cửa sổ chính, hộp thoại lớn |
| `RadiusPill` | 9999 | chip, ô tìm kiếm, huy hiệu |

Quy tắc: **bán kính con = bán kính cha − khoảng đệm** (ảnh bìa trong thẻ bo `RadiusLg - padding`). Xoá hết các số 4/6/8/10/12/14/17/19 rải rác bằng tìm `Radius =` và dùng token.

Bóng đổ ba mức: `Elev1` (chip, nút nổi: mờ 6 px, 12% đen tím), `Elev2` (panel: mờ 24 px), `Elev3` (hộp thoại, thẻ chi tiết: mờ 40 px). Khoảng cách theo bước 4/8/12/16/24/32. Cỡ chữ: 12 (phụ) / 13 (thân) / 15 (mục) / 18 (tiêu đề panel) / 24 (tên địa điểm, serif).

---

## 4. Bố cục mới của cửa sổ chính

```
┌──────────────────────────────────────────────────────────────────────────┐ ← cửa sổ bo 28
│ ◆ Huế Travel                              ☀ 27°C  Chào buổi sáng   ─ ▢ ✕ │ ← thanh tiêu đề mảnh 40, tím gradient
├──────────────────────────────────────────────────────────────────────────┤
│ ╭────────────────────────╮                                      ╭─╮      │
│ │ 🔍 Tìm điểm đến ở Huế…  │  ← ô tìm kiếm dạng viên thuốc, nổi    │＋│      │
│ ╰────────────────────────╯                                      │－│ ← zoom│
│ (Tất cả)(Di tích)(Cảnh quan)(Ẩm thực)(Lưu trú)… ← chip nổi        ╰─╯      │
│ ╭──╮ ╭──────────────────────╮                                  ╭─╮        │
│ │🔍│ │ PANEL (bo 20, bóng)  │         BẢN ĐỒ TRÀN MÉP            │🧭│        │
│ │🧭│ │  ảnh bìa + gradient  │                                  ╰─╯        │
│ │📖│ │  danh sách có ảnh    │      ◉ điểm đánh dấu hình giọt              │
│ │👤│ ╰──────────────────────╯                                              │
│ ╰──╯                                                                      │
│ ╭──────────╮                  ╭──────────────────────╮                   │
│ │ Nền bản đồ│                  │ 16.4637 N · 107.59 E │ 1:25 000 ● Trực tuyến│
│ ╰──────────╯                  ╰──────────────────────╯                   │
└──────────────────────────────────────────────────────────────────────────┘
```

Thay đổi so với hiện tại:

| Hiện tại | Mới |
|---|---|
| `TitleBar` 56 px chứa logo + ô tìm kiếm + nút cửa sổ | Thanh tiêu đề 40 px (logo, lời chào + thời tiết, nút cửa sổ). **Ô tìm kiếm chuyển thành viên thuốc nổi trên bản đồ** |
| `FilterBar` 48 px dải ngang đặc | Hàng chip **nổi** ngay dưới ô tìm kiếm, nền chip đặc (không mờ, do ràng buộc ActiveX), có bóng nhẹ |
| `NavRail` 80 px dải dọc đặc | **Thanh dọc nổi** 64 px, bo viên thuốc, cách mép 12 px, chỉ có biểu tượng + chấm báo mục đang mở (chữ hiện khi rê chuột) |
| `AppStatusBar` 28 px dải đặc | **Viên thuốc nổi** đáy giữa: tọa độ, tỉ lệ, trạng thái máy chủ |
| Nút nền bản đồ (`stackBaseMaps`) 36 px ở góc | **Thẻ chuyển nền bản đồ ở góc dưới trái** có ảnh thu nhỏ (kiểu Google Maps), mở rộng khi bấm |
| Bản đồ chiếm phần còn lại | Bản đồ **lấp đầy toàn bộ** dưới thanh tiêu đề; mọi thứ khác là lớp nổi |

Khi cửa sổ hẹp (< 1100 px): thanh dọc thu về biểu tượng, chip cuộn ngang, panel rộng 360.

---

## 5. Công nghệ bo góc và bóng đổ (rủi ro lớn nhất, phải làm thử trước)

Vấn đề: muốn bo 20 px, có bóng mềm, mượt răng cưa, trên cả Windows 10 và 11, mà panel là `Form` chứa điều khiển WinForms thường.

| Cách | Bo tùy ý | Răng cưa | Bóng | Ghi chú |
|---|---|---|---|---|
| Hiện tại: `DWMWCP_ROUND` | Không (cố định ~8) | Mượt | Có | Chỉ Win11 |
| Hiện tại: `Region` | Có | **Răng cưa** | **Mất** | Dự phòng Win10 |
| **Đề xuất: cửa sổ nền "bóng + khung"** (xem dưới) | Có | Mượt | Có, tùy chỉnh | Thêm 1 cửa sổ cho mỗi panel |
| Cửa sổ phân lớp `UpdateLayeredWindow` cho cả nội dung | Có | Mượt | Có | Điều khiển con **không** vẽ lại tốt trong cửa sổ phân lớp; không nên |

**Cách đề xuất — cửa sổ nền "bóng + khung"** (`Theme/CardShadowForm.cs`, mới):
1. Với mỗi `FloatingPanelForm`, tạo một cửa sổ phụ cùng chủ (`Owner`), kiểu `WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW` (không nhận chuột, không hiện trên thanh tác vụ).
2. Cửa sổ này lớn hơn panel mỗi cạnh một khoảng đệm `Elev` (ví dụ 28 px). Nó vẽ **một bitmap 32 bit có alpha** gồm: bóng đổ mờ Gauss, rồi hình thẻ bo `RadiusLg` đặc màu `Paper` kèm viền 1 px `Line`, có răng cưa mượt. Cập nhật bằng `UpdateLayeredWindow`. Bitmap dựng một lần theo kích thước (cache theo `Size`).
3. Panel chính (nội dung) đặt đúng vào hình thẻ, **thụt vào 1 px**, dùng `Region` bo `RadiusLg - 1`. Cạnh răng cưa của `Region` rơi lên nền thẻ **cùng màu** của cửa sổ nền nên không nhìn thấy. Tắt DWM bo góc (`DWMWCP_DONOTROUND`) và tắt `DwmExtendFrameIntoClientArea` để hệ thống không vẽ bóng thứ hai.
4. Hai cửa sổ di chuyển/đổi cỡ/hiện/ẩn cùng nhau: `FloatingPanelForm.Reposition`, `SetVisibleCore`, `OnResize` cập nhật luôn cửa sổ nền. Thứ tự Z: nền ngay dưới panel.
5. Chuyển động mở panel: trượt 24 px + mờ dần cho **cả hai cửa sổ** bằng cùng bộ hẹn giờ (thay `StepFade`), dùng hàm easing ra-chậm.

**Việc đầu tiên (Giai đoạn 0, bắt buộc):** dựng thử một panel mẫu bằng cách trên, chạy ở 100/125/150% DPI, Win10 và Win11, kéo cửa sổ chính, thu nhỏ/phóng to, đổi nhiều panel nhanh liên tục. Tiêu chí đạt: không nhấp nháy, không lệch 1 px giữa hai cửa sổ, bóng không bị cắt, CPU không tăng đáng kể. **Nếu không đạt**, phương án lùi: giữ DWM bo 8 px (Win11) và chấp nhận `Region` bo 16 px không bóng cho Win10; vẫn thống nhất bằng token.

Các điều khiển con đè lên `axMap` (zoom, la bàn, ô nền bản đồ, ô tìm kiếm, chip) **không** dùng cách này (chúng là điều khiển con, không phải `Form`). Chúng được vẽ đặc, bo viên thuốc/bo `RadiusLg` bằng `RoundedRect` như `MapToolStack` đang làm, với **bóng giả**: vẽ vài vòng bo mờ dần (alpha 6–10%) ngay trong vùng điều khiển lớn hơn nội dung 6–8 px; góc ngoài các vòng này vẫn nằm trên nền bản đồ nên phải dùng nền điều khiển **trùng màu nền bản đồ** (đã có `BackColor = Transparent` trong `MapToolStack`; xác nhận nó thật sự lấy được hình bản đồ ở phía sau, nếu không thì bóng sẽ có viền chữ nhật). Làm thử cùng lúc với Giai đoạn 0.

---

## 6. Bộ thành phần dùng chung (`Theme/`)

Làm trước các panel để các panel chỉ ghép lại. Mỗi thành phần phải: dùng token, vẽ bằng `RoundedRect`, hỗ trợ DPI (`Ui.S`), có trạng thái hover/pressed/focus/disabled, và dùng `PressFeedback` có sẵn.

| Thành phần | Trạng thái hiện tại | Việc làm |
|---|---|---|
| `Tokens` (`ThemeColors`, `ThemeMetrics`, `ThemeFonts`) | Có nhưng cũ | Thêm token ở Mục 3.3–3.4; thêm font serif nhúng (xem Mục 10) |
| `CardShadowForm` | Chưa có | Mục 5 |
| `FloatingPanelForm` | Bo 8, fade | Dùng `CardShadowForm`; trượt + mờ; tham số `RadiusLg`; giữ `AttachTo/AttachToSide` |
| `PanelHeader` | Thanh đặc | Đầu panel gradient tím→tím đậm + hoa văn mờ; tiêu đề serif; nút đóng tròn |
| `IconButton` | Có `Radius`, `Elevated` | Quy về 4 kiểu: `Primary` (tím đặc), `Tonal` (nền `BrandSoft`), `Outline`, `Ghost`; bo `RadiusSm` hoặc pill |
| `InputField`, `SelectField`, `AuthField` | Bo riêng lẻ | Bo `RadiusSm`, viền `Line`, vòng focus tím 2 px |
| `ToggleSwitch`, `BrandCheckBox` | Có | Đổi màu theo token |
| `Chip` | Mỗi nơi vẽ tay (`FilterBar`, `CountChips`) | Gom thành một control dùng lại, có số đếm và biểu tượng |
| `Badge` (Đang mở / Đóng cửa / Sắp đóng / Mới) | Chưa có | Viên thuốc nhỏ: xanh ngọc, đỏ son, vàng |
| `RatingStars` | Chưa rõ | Sao vàng `Gold`, nửa sao |
| `ImageCard` | `PhotoGallery` | Ảnh bo góc có gradient đen ở đáy để chồng chữ; ảnh giữ trong bộ nhớ đệm (`PhotoStore`) |
| `Skeleton` | Chưa có | Khối xám nhấp nháy khi đang tải (thay "Đang tải…") |
| `EmptyState` | Chưa có | Hình minh họa + 1 dòng + 1 nút (ví dụ chưa có yêu thích) |
| `Toast` | Có | Bo `RadiusLg`, biểu tượng theo loại, trượt từ trên |
| `SectionHeader` | Từng panel tự vẽ | Tiêu đề mục chữ hoa nhỏ, gạch vàng ngắn bên dưới |
| `Divider` | `Panel` 1 px tự tạo | Một control, màu `Line` |

---

## 7. Thiết kế từng khu vực

### 7.1 Màn hình mở đầu (`Welcome/*`)
Đã làm bố cục 3 tab. Việc còn lại: đổi nền thẻ sang `Paper`, bo `RadiusXl`, chữ tiêu đề serif, nền trang là ảnh Huế (Ngọ Môn/sông Hương) phủ gradient tím; thêm khối giới thiệu 3 gạch đầu dòng bên trái. Màn tải bản đồ (`BaseMapDownloadForm`) dùng cùng phong cách: logo, thanh tiến độ bo tròn, câu "Đang chuẩn bị bản đồ Cố đô…". Màn tải này xuất hiện lúc mở app nên là ấn tượng đầu tiên.

### 7.2 Thanh tiêu đề và cửa sổ chính (`Shell/TitleBar*`, `MainForm*`)
- `TitleBar` 40 px, gradient tím ngang, logo + chữ "Huế Travel" (serif), ở giữa/bên phải: lời chào và thời tiết (chip), rồi nút cửa sổ.
- Cửa sổ chính bo `RadiusXl` ở bốn góc: Win11 dùng `DWMWCP_ROUND`; Win10 dùng `Region`. Khi **phóng to** thì bỏ bo (cạnh phải vuông khi chiếm toàn màn hình).
- **Ô tìm kiếm** tách khỏi `TitleBar` thành điều khiển nổi trong `MapHost` (viên thuốc 400×48, bóng giả, biểu tượng tìm kiếm tím, nút xóa). Tái dùng nguyên `txtSearch`, `SearchDropdown` và các sự kiện (`SearchTextChanged/Submitted/Moved/FocusLost/FocusGained`); chỉ đổi nơi đặt và `SearchBoxBounds` (nơi `SearchDropdown` neo vào, `Shell/SearchDropdown.cs:101` đang dùng `Region`).
- Danh sách gợi ý khi gõ: hàng có ảnh nhỏ 44 px, tên, loại, huy hiệu giờ mở cửa; bo `RadiusLg`, tối đa 6 dòng.

### 7.3 Thanh dọc điều hướng và chip lọc (`NavRail`, `FilterBar`)
- `NavRail`: thành thẻ nổi bo pill, 4 biểu tượng (Khám phá, Lộ trình, Hướng dẫn/Trợ lý, Hồ sơ), mục chọn có nền `BrandSoft` và thanh tím bên trái; chữ hiện khi rê chuột. Giữ các hằng `NavRail.Explore/Route/Guide/Profile` để `MainForm.sidePanels[]` không lệch.
- `FilterBar`: chip nổi, bo pill, biểu tượng màu nhóm; chip chọn nền tím đặc chữ trắng; số đếm nhỏ; cuộn ngang bằng chuột khi quá hẹp.

### 7.4 Panel Khám phá (`Panels/Explore/ExplorePanel.cs`, `Panels/Shared/PoiRowList.cs`)
- Đầu panel gradient + tiêu đề "Khám phá Huế" + dòng phụ "N địa điểm".
- **`PoiRowList` có ảnh**: mỗi hàng cao 84 px: ảnh bo `RadiusMd` 64×64 bên trái (dùng `PoiPhotos/PhotoStore`; khi chưa có ảnh dùng ô màu nhóm kèm biểu tượng), tên (đậm), loại · khoảng cách?, huy hiệu mở/đóng, sao, nút tim. Hover nâng nhẹ, bấm có gợn sóng (`PressFeedback`).
- Nhóm theo mục bằng `SectionHeader`. Trang trống dùng `EmptyState`. Khi tải dùng `Skeleton`.

### 7.5 Thẻ chi tiết địa điểm (`Panels/PoiIdentify/PoiIdentifyCard.cs`)
Hiện 400 px rộng, ảnh bìa 196 px. Thiết kế lại thành "thẻ du lịch":
- Ảnh bìa tràn đầu thẻ (bo đỉnh `RadiusLg`), gradient đen dưới, **tên địa điểm chồng lên ảnh bằng chữ serif trắng**, huy hiệu loại ở góc trên trái, nút đóng/yêu thích tròn ở góc trên phải.
- Dưới ảnh: hàng "chỉ số nhanh": sao đánh giá · khoảng giá · **Đang mở · đóng lúc 17:00** (dùng kết quả Mục 3 của `ke-hoach-chinh-sua.md`).
- Hàng nút hành động tròn như Google Maps: **Chỉ đường**, **Thêm vào lộ trình**, **Lưu**, **Chia sẻ**, **Báo sai**. Nút cần tài khoản vẫn hiện, bấm khi là khách thì mở form đăng nhập (Mục 1 của `ke-hoach-chinh-sua.md`).
- Thông tin: địa chỉ, điện thoại, giờ mở cửa (mở rộng theo từng thứ), website — mỗi dòng có biểu tượng tròn tím nhạt và nút sao chép.
- Thư viện ảnh: dải ảnh nhỏ bo `RadiusMd`, bấm mở `PhotoViewer` (đã có) với nền đen mờ và nút bo tròn.
- Đánh giá: tóm tắt sao + vài dòng, nút "Xem tất cả" mở `ReviewsDialog`.

### 7.6 Panel Lộ trình và kết quả (`RouteOptimizerPanel`, `TripResultPanel`, `StopList`)
- Panel lộ trình dạng **dòng thời gian dọc**: mỗi điểm dừng là một vòng tròn đánh số tím, nối nhau bằng đường kẻ, kéo thả đổi thứ tự (nếu đã có), nút xóa nhỏ; điểm xuất phát vòng `Lacquer`.
- `TripResultPanel` (đáy giữa, 760×204) thành thẻ lớn bo `RadiusLg`: tóm tắt (tổng km, tổng thời gian, số điểm), thanh các chặng màu, và bộ điều khiển xe: Phát/Dừng, **x1 x2 x4 x8** dạng nhóm nút viên thuốc, thanh tiến độ (khớp Mục 4 của `ke-hoach-chinh-sua.md`).

### 7.7 Panel Lớp bản đồ và nền bản đồ (`LayerPanel`, `stackBaseMaps`)
- Chuyển nền bản đồ: thẻ nổi góc dưới trái, ảnh thu nhỏ 56×56 bo `RadiusMd` cho từng nền còn lại sau khi gỡ Google (Mục 2 kia); bấm để mở rộng thành hàng ảnh.
- `LayerPanel`: công tắc từng nhóm có biểu tượng màu nhóm, bảng chú giải gọn.

### 7.8 Hướng dẫn / Trợ lý (`Panels/TravelGuide/*` hoặc `ChatPanel`)
Khớp `ke-hoach-chatbot-deepseek.md` Bước 13: bong bóng bo `RadiusLg`, bong bóng người dùng nền tím chữ trắng, trợ lý nền `PaperDeep`; chip địa điểm bo pill; thẻ lộ trình có nút "Xem trên bản đồ". Nếu chưa làm chatbot: `TourList` thành **thẻ tour có ảnh bìa** (mỗi tour một ảnh lớn bo, tên serif, thời lượng, "Dùng tour này").

### 7.9 Hồ sơ, đánh giá, quản trị (`Account/*`, `Admin/*`)
- `AccountPanel`: thẻ hồ sơ có ảnh đại diện tròn chữ cái đầu, vai trò dạng huy hiệu; menu (`ProfileMenu`) hàng cao 48 px, biểu tượng tròn tím nhạt.
- Hộp thoại (`Dialogs`, `ReviewsDialog`): bo `RadiusXl`, nền `Paper`, nút bo `RadiusSm`. `Dialogs.cs:97` đang dùng `Region` — chuyển sang cùng cơ chế bo của Mục 5 để thống nhất.
- Bảng điều khiển quản trị (`Admin/*`): bảng có hàng xen kẽ nền `Paper/PaperDeep`, thanh bên trái dạng thẻ; vì chỉ quản trị viên dùng nên để **sau cùng**.

---

## 8. Bản đồ nền và các lớp trên bản đồ (`MapEngine/*`)

Đây là phần làm nên cảm giác "bản đồ du lịch".

### 8.1 Làm lại kiểu bản đồ nền (`LocalBaseMap.cs`)
| Lớp | Hiện tại | Đề xuất |
|---|---|---|
| Biển | xanh nhạt phẳng | giữ, thêm nhãn "BIỂN ĐÔNG" nghiêng chữ có chân |
| Nước (sông, đầm phá, song_2n/1n) | xanh nhạt | màu `Jade` rất nhạt, nét sông Hương dày hơn |
| Đất liền/tỉnh | be `243,239,230`, viền **tím nhạt nét đứt 1.8** | `PaperDeep`, **viền tỉnh mảnh xám ấm** (bỏ tím nổi) |
| Ranh giới xã | viền tím nhạt 1.2 | chỉ hiện khi zoom gần, nét chấm rất nhạt |
| Đường | thân + viền (đã có) | thân trắng, viền xám ấm; đường chính vàng nhạt; đường nhỏ mảnh |
| Nhãn | màu phẳng | **có viền sáng** (halo) quanh chữ, phân cấp cỡ chữ theo loại, tên sông hương nghiêng |
| Vùng di sản (Kinh thành, Đại Nội, lăng tẩm) | không có | polygon phủ **vàng rất nhạt + viền vàng nét đứt** + nhãn; cần lớp dữ liệu (hỏi nguồn; nếu chưa có thì vẽ vài đa giác thô từ tọa độ các điểm di sản) |
| Cây xanh/công viên | không có | nếu dữ liệu có: nền xanh lá rất nhạt |

Chỉ sửa **kiểu**, không đổi dữ liệu; kiểm bằng ảnh chụp trước/sau ở 3 mức thu phóng (cả tỉnh, thành phố Huế, khu Đại Nội).

### 8.2 Điểm đánh dấu (`PoiMarkerStyle.cs`, `PoiMarkerIcons.cs`)
- Thay chấm tròn 12 px bằng **ghim hình giọt** 28×36 px: nền màu nhóm, biểu tượng trắng bên trong (cùng bộ biểu tượng `IconFont`), viền trắng 2 px, bóng nhẹ phía dưới. Dựng bằng GDI+ rồi nạp vào MapWinGIS làm biểu tượng điểm (đọc `PoiMarkerIcons.cs` hiện nạp biểu tượng thế nào; **đọc trước khi sửa**).
- Điểm được chọn: ghim to hơn 1.25× + vòng sáng (`HighlightRing.cs` đã có, đổi màu `Gold`).
- **Gom cụm** khi thu nhỏ: bong bóng tròn tím có số; chỉ làm nếu MapWinGIS dùng được (nếu không, dùng ngưỡng tỉ lệ ẩn nhóm nhỏ). Đây là hạng mục **không chắc**: đo hiệu năng trước.
- Nhãn tên hiện khi zoom đủ gần, chữ có viền sáng, tránh chồng nhau.

### 8.3 Lộ trình trên bản đồ (`RoutePalette.cs`, `RouteDrawing.cs`)
- Nét dày 6 px màu chặng **có viền trắng 2 px ngoài** và bóng mờ; phần đã đi mờ hơn, phần sắp đi sáng (khi xe chạy).
- **Điểm dừng đánh số** (1,2,3…) vòng tròn tím chữ trắng; điểm xuất phát vòng `Lacquer`, điểm cuối cờ.
- Mũi tên chỉ hướng đặt cách đều dọc đường.
- **Xe mô phỏng**: biểu tượng ô tô nhìn từ trên, quay theo hướng chạy, có bóng nhỏ (đã có xe; thêm quay theo hướng nếu chưa có — đọc `RouteDrawing.cs`).
- Bảng màu chặng đổi sang dải hài hòa với tím Huế (ví dụ tím, ngọc, vàng đất, chàm, đỏ son, cam đất, hồng cánh sen, xanh rêu) thay cho bộ màu Tailwind sặc sỡ hiện tại.

### 8.4 Điều khiển trên bản đồ (`MapEngine/Overlays/*`)
- `MapToolStack` thành nhóm nút **tròn/viên thuốc**, bóng nhẹ, bo `RadiusLg`, biểu tượng tím; nút la bàn (`CompassButton`) tròn có kim đỏ son.
- Thanh tỉ lệ ở viên thuốc đáy; nhãn nguồn dữ liệu (`attribution`) thu nhỏ, bo.
- Hiệu ứng **bay tới** khi chọn địa điểm: dịch/zoom mượt trong ~400 ms với easing (nếu `MapHost` hiện nhảy thẳng; đọc `MapHost.cs` để biết hàm đặt khung nhìn).

---

## 9. Chuyển động và vi tương tác

| Nơi | Chuyển động |
|---|---|
| Mở/đóng panel | trượt 24 px + mờ 180 ms, easing ra-chậm |
| Thẻ chi tiết | trượt từ trái, ảnh bìa mờ dần từ placeholder |
| Chip/nút | rê chuột nâng nhẹ + sáng; bấm gợn sóng (có `PressFeedback`) |
| Hàng danh sách | hiện lần lượt, trễ 20 ms mỗi hàng (tối đa 8 hàng) |
| Ảnh | mờ dần khi tải xong, `Skeleton` trong lúc chờ |
| Bay tới địa điểm | 400 ms |
| Toast | trượt từ trên, tự đóng sau 3 s |
| Xe mô phỏng | theo Mục 4 của `ke-hoach-chinh-sua.md` |

Tôn trọng cài đặt "giảm chuyển động" của Windows (`SystemParameters`/`SPI_GETCLIENTAREAANIMATION`): tắt hiệu ứng khi người dùng tắt hoạt ảnh. Dùng một bộ hẹn giờ chung 60 Hz thay vì mỗi panel một `Timer`.

---

## 10. Tài nguyên cần chuẩn bị

| Tài nguyên | Ghi chú |
|---|---|
| Font serif hỗ trợ tiếng Việt (Lora hoặc Playfair Display) | Giấy phép OFL; nhúng vào ứng dụng cùng cách `FontLoader` đang nhúng Plus Jakarta Sans và Material Symbols (`Theme/FontLoader.cs`). Kiểm tra dấu tiếng Việt hiển thị đủ |
| Biểu tượng bổ sung | Bộ `MaterialSymbolsSubset.ttf` là tập con: phải **thêm tên biểu tượng mới** (`directions`, `share`, `bookmark`, `star`, `schedule`, `wb_twilight`…) vào cả tệp font và bảng `IconFont.cs` trước khi dùng, nếu không sẽ không hiện |
| Logo vector và biểu tượng ứng dụng | Làm lại logo theo tím + vàng (hình hoa sen hoặc mái cổng Ngọ Môn cách điệu); xuất PNG nhiều cỡ cho `AppImages` |
| Ảnh nền màn mở đầu | 1 ảnh Huế chất lượng cao có bản quyền hoặc tự chụp; nén ≤ 400 KB |
| Ảnh minh họa trang trống | 3–4 hình đơn giản (chưa có yêu thích, chưa có lộ trình, không tìm thấy, mất mạng) |
| Ảnh thu nhỏ nền bản đồ | Chụp từ chính bản đồ nền ở mỗi kiểu (`Map.SnapShot`) |
| Họa tiết hồi văn | 1 hình lặp mờ (PNG trong suốt) |

Ảnh địa điểm đã có trong dữ liệu (`PoiPhotos`, `PhotoStore`); hãy kiểm tra **nguồn và giấy phép** các ảnh đó trước khi coi chúng là nội dung chính của giao diện.

---

## 11. Phối hợp với các kế hoạch khác (tránh xung đột)

Cả bốn kế hoạch chạm cùng các tệp giao diện. Thứ tự khuyến nghị: làm **việc chức năng trước hoặc xen kẽ đúng tệp**, và dùng một nhánh riêng cho phần giao diện.

| Tệp | Kế hoạch khác chạm vào | Lưu ý |
|---|---|---|
| `Shell/MainForm.cs`, `MainForm.Designer.cs` | Mục 1 (cổng khách), chatbot (đổi `TravelGuidePanel`), giao diện này (bỏ các dải dock) | Đổi tên/giữ chỉ số `NavRail` trước, rồi mới dời bố cục |
| `Shell/NavRail.cs` | chatbot (nhãn "Trợ lý") | Làm cùng lượt |
| `Panels/PoiIdentify/PoiIdentifyCard.cs` | Mục 1 (nút cần tài khoản), Mục 3 (giờ mở cửa) | Thiết kế lại thẻ **sau** Mục 3 để huy hiệu giờ mở dùng dữ liệu thật |
| `Panels/RouteOptimizer/TripResultPanel.cs`, `MapEngine/RouteDrawing.cs` | Mục 4 (xe thời gian thực, x2/x4/x8) | Làm Mục 4 trước hoặc cùng lượt; giao diện chỉ bọc lại |
| `MapEngine/BaseMapCatalog.cs`, `MapHost.cs`, `LayerPanel.cs` | Mục 2 (gỡ Google) | Làm Mục 2 **trước** khi làm thẻ chuyển nền bản đồ có ảnh thu nhỏ |
| `Welcome/LoginForm.cs` | đã làm | Chỉ còn chỉnh màu/ảnh |

---

## 12. Các giai đoạn thực hiện

Mỗi giai đoạn kết thúc bằng một bản chạy được (build xanh, chụp ảnh màn hình 100/125/150% DPI), không để ứng dụng hỏng giữa chừng.

### Giai đoạn 0: Thử nghiệm kỹ thuật (1–3 ngày)
- [ ] Panel mẫu dùng `CardShadowForm` (Mục 5): bo 20, bóng mềm, Win10 và Win11, 3 mức DPI.
- [ ] Thử bóng giả cho điều khiển con đè lên `axMap` (nền `Transparent` có lấy được hình bản đồ không).
- [ ] Thử nạp ghim hình giọt làm biểu tượng điểm trong MapWinGIS và đo hiệu năng với toàn bộ số điểm.
- **Cổng quyết định:** nếu thất bại ở ý đầu, dùng phương án lùi trong Mục 5 và giảm kỳ vọng bo góc.

### Giai đoạn 1: Nền tảng thiết kế (3–5 ngày)
- [ ] Token màu, bo góc, bóng, chữ; font serif; biểu tượng bổ sung.
- [ ] Thay mọi số bo góc rải rác bằng token (`Grep "Radius ="`).
- [ ] Đổi nội dung các màu cũ sang bảng mới để toàn app đổi tông ngay.
- [ ] `CardShadowForm` + nâng `FloatingPanelForm`.
- [ ] Bộ thành phần Mục 6 (nút, ô nhập, chip, huy hiệu, sao, skeleton, trang trống).

### Giai đoạn 2: Vỏ cửa sổ và bố cục (4–6 ngày)
- [ ] Thanh tiêu đề mảnh + cửa sổ bo; bỏ `FilterBar`/`AppStatusBar` dạng dock.
- [ ] Ô tìm kiếm nổi; chip lọc nổi; thanh dọc nổi; viên thuốc trạng thái; thẻ nền bản đồ.
- [ ] Bản đồ lấp đầy; kiểm các panel vẫn bám đúng `AttachToSide` (đổi hằng `SideLeft/SideTop/SideBottom` ở `MainForm.Designer.cs:17-19`).
- [ ] Kiểm tra thay đổi kích cỡ, phóng to/thu nhỏ, kéo cửa sổ, nhiều màn hình.

### Giai đoạn 3: Các panel (5–8 ngày)
- [ ] Khám phá + danh sách có ảnh; thẻ chi tiết; lộ trình + kết quả; lớp; hồ sơ; hộp thoại.
- [ ] Màn mở đầu và màn tải bản đồ.

### Giai đoạn 4: Bản đồ du lịch (5–7 ngày)
- [ ] Kiểu bản đồ nền (Mục 8.1) và so sánh trước/sau.
- [ ] Ghim giọt, chọn điểm, (nếu khả thi) gom cụm.
- [ ] Lộ trình có viền, đánh số, mũi tên, xe quay theo hướng; bảng màu mới.
- [ ] Điều khiển bản đồ; hiệu ứng bay tới.

### Giai đoạn 5: Chuyển động, trạng thái, hoàn thiện (3–5 ngày)
- [ ] Trượt/mờ, bộ hẹn giờ chung, tôn trọng giảm chuyển động.
- [ ] Skeleton, trang trống, thông báo lỗi thân thiện.
- [ ] Phím tắt, thứ tự Tab, vòng focus, độ tương phản.
- [ ] Rà soát toàn bộ: không còn góc vuông, không còn màu lạc tông, không còn chữ bị cắt.

### Giai đoạn 6 (tùy chọn): chế độ tối và bản đồ ban đêm
Token màu hỗ trợ chủ đề; bộ kiểu bản đồ nền tối; tự bật theo giờ hoặc theo cài đặt Windows.

---

## 13. Kiểm thử và nghiệm thu

**Ma trận chụp ảnh** (lưu vào `tai-lieu/anh-giao-dien/` để so sánh trước/sau): màn đăng nhập, cửa sổ chính (không panel / từng panel), thẻ địa điểm (có và không có ảnh), lộ trình (đang chạy xe), bản đồ ở 3 mức thu phóng, hộp thoại, quản trị. Mỗi cái ở: Win10 + Win11, 100/125/150% DPI, cửa sổ 1280×720 / 1536×860 / toàn màn hình.

**Tiêu chí nghiệm thu:**
1. Không còn góc vuông nào ở: cửa sổ (khi không phóng to), panel, thẻ, nút, ô nhập, chip, hộp thoại, toast.
2. Mọi bán kính lấy từ `ThemeMetrics`; tìm `Radius =\s*\d` trong code giao diện không còn số trần.
3. Mọi màu lấy từ `ThemeColors`; không còn `Color.FromArgb(...)` nằm rải trong panel (ngoại lệ: bản đồ nền và bảng màu chặng).
4. Không nhấp nháy khi mở/đóng/đổi cỡ panel; không lệch giữa panel và bóng.
5. Chữ tiếng Việt có dấu đủ ở mọi font; không cắt chữ ở 150% DPI.
6. Độ tương phản chữ ≥ 4.5:1 (chữ thân), ≥ 3:1 (chữ lớn, biểu tượng).
7. Thao tác bằng bàn phím được toàn bộ luồng chính (tìm, chọn, thêm vào lộ trình, phát xe).
8. CPU khi kéo bản đồ không tăng đáng kể so với trước.
9. Chế độ ngoại tuyến và chế độ khách vẫn hiển thị đúng.

---

## 14. Rủi ro

| Rủi ro | Cách giảm |
|---|---|
| Cơ chế hai cửa sổ (bóng + nội dung) nhấp nháy hoặc lệch | Giai đoạn 0 làm thử; phương án lùi giữ DWM/`Region` |
| Điều khiển đè lên `axMap` không bo/bóng đẹp (ràng buộc ActiveX) | Vẽ đặc, bóng giả; nếu nền trong suốt không hoạt động thì dùng nền viền trắng 2 px để che góc |
| Hiệu năng giảm vì nhiều cửa sổ phân lớp | Cache bitmap bóng theo kích cỡ; chỉ cập nhật khi đổi cỡ; ẩn cửa sổ nền khi panel ẩn |
| Ghim hình giọt/gom cụm làm bản đồ chậm | Đo ở Giai đoạn 0; nếu chậm, dùng ghim tĩnh nhỏ, bỏ gom cụm |
| Đổi bảng màu làm lệch nơi vẽ tay | Đổi nội dung token cũ trước; sau đó quét `Grep "FromArgb\|FromHtml"` |
| Biểu tượng mới không hiện (font tập con) | Thêm vào tệp font + `IconFont.cs` trước khi dùng |
| Ảnh địa điểm không có giấy phép dùng | Kiểm nguồn; thay bằng ảnh tự chụp hoặc có bản quyền mở |
| Phạm vi quá lớn, dở dang | Theo giai đoạn; mỗi giai đoạn nghiệm thu được; Giai đoạn 1–2 đã đủ thay đổi diện mạo rõ rệt |
| Xung đột với các kế hoạch chức năng | Mục 11; dùng nhánh riêng, hợp nhất theo từng giai đoạn |
| Hiển thị lệch khi DPI > 150% hoặc nhiều màn hình | Dùng `Ui.S` mọi nơi; thử `Ui.Scale` cố định theo tiến trình (ứng dụng nhận biết DPI hệ thống, không đổi khi chuyển màn hình khác DPI) |

---

## 15. Danh sách việc (tóm tắt)

- [ ] 0 Thử `CardShadowForm`, bóng giả trên `axMap`, ghim giọt
- [ ] 1 Token (màu, bo, bóng, chữ) + font serif + biểu tượng mới
- [ ] 2 Gỡ số bo rải rác; `FloatingPanelForm` mới; bộ thành phần
- [ ] 3 Cửa sổ bo + thanh tiêu đề mảnh
- [ ] 4 Ô tìm kiếm nổi, chip nổi, thanh dọc nổi, viên thuốc trạng thái, thẻ nền bản đồ
- [ ] 5 Panel: Khám phá, thẻ chi tiết, Lộ trình, Kết quả, Lớp, Hồ sơ, hộp thoại
- [ ] 6 Màn mở đầu + màn tải
- [ ] 7 Kiểu bản đồ nền
- [ ] 8 Ghim giọt, chọn điểm, nhãn
- [ ] 9 Lộ trình: viền, số, mũi tên, xe quay hướng, màu mới
- [ ] 10 Chuyển động + bộ hẹn giờ chung + giảm chuyển động
- [ ] 11 Skeleton, trang trống, thông báo lỗi
- [ ] 12 Rà soát nghiệm thu (Mục 13)
- [ ] 13 (tùy chọn) Chế độ tối
