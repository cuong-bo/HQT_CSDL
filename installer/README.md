# Bộ cài Windows

`CoDoGIS.iss` (Inno Setup 6) đóng gói app, **MapWinGIS** (kèm GDAL) và tự đăng ký `MapWinGIS.ocx` bằng `regsvr32` 32-bit.

## Làm bộ cài

1. Máy dựng cần: Visual Studio 2019 hoặc MSBuild, [Inno Setup 6](https://jrsoftware.org/isinfo.php), và thư mục MapWinGIS đã cài
   (mặc định `C:\Program Files\MapWinGIS`).
2. Dựng bản Release: `msbuild Hue_travel\Hue_travel.csproj /restore /p:Configuration=Release`.
3. (Khuyên dùng) tải *Microsoft Visual C++ Redistributable x86* từ trang Microsoft, đặt thành `installer\redist\vc_redist.x86.exe`; bộ cài sẽ chạy nó nếu có.
4. `ISCC.exe installer\CoDoGIS.iss /DAppVersion=1.0.0` → `installer\output\CoDoGIS-Setup-1.0.0.exe`.

Tham số: `/DMapWinGisDir=...` nếu MapWinGIS nằm chỗ khác, `/DSourceDir=...` nếu bản dựng nằm chỗ khác.

## Bộ cài làm gì

- Kiểm tra .NET Framework 4.7.2; hỏi địa chỉ máy chủ và lưu vào `%ProgramData%\CoDoGIS\server.txt` (để trống = chỉ ngoại tuyến; app đọc file này, có thể sửa sau).
- Chép app và MapWinGIS vào `Program Files\CoDoGIS`, đăng ký `MapWinGIS.ocx`, tạo lối tắt; gỡ cài đặt hủy đăng ký.
- Cần quyền quản trị (đăng ký COM).

## Thử trên máy sạch

Dùng Windows Sandbox hoặc máy ảo Windows 10/11 chưa cài MapWinGIS: cài, mở app, phải thấy bản đồ và điểm dữ liệu mà không phải cài thêm gì.

## Lưu ý

- Bộ cài chưa ký số nên Windows SmartScreen có thể cảnh báo "Unknown publisher"; chọn *More info → Run anyway*. Ký mã (code signing) khi có điều kiện.
- Giấy phép MapWinGIS (MPL) nằm trong thư mục `Licenses` của MapWinGIS; giữ nguyên khi đóng gói.
