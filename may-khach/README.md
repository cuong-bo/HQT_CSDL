# Máy khách Hue Travel (Windows)

Ứng dụng WinForms (.NET Framework 4.7.2, x86) dùng MapWinGIS. Vẽ bản đồ nền từ shapefile đi kèm; tìm đường và tài khoản
gọi máy chủ qua HTTP. Thư mục này **không cần** `may-chu/`.

| Đọc | Khi nào |
|---|---|
| [HUONG-DAN-CAI-DAT.md](HUONG-DAN-CAI-DAT.md) | Phần A: người dùng cài ứng dụng. Phần B: người làm bộ cài |
| [bo-cai/README.md](bo-cai/README.md) | Bộ cài Inno Setup |

## Bố cục

```
may-khach/
├── ung-dung/        mã nguồn ứng dụng (Hue_travel.csproj) và Data/shp (bản đồ nền, địa điểm dự phòng)
├── bo-cai/          HueTravel.iss (Inno Setup)
├── kiem-thu/        test_services.ps1 (chạy bằng PowerShell 32-bit sau khi dựng bản Debug)
├── Hue_travel.sln   solution Visual Studio
└── dong-goi.ps1     dựng bộ cài HueTravel-Setup-<phiên bản>.exe
```

## Dựng và thử

```powershell
msbuild may-khach\ung-dung\Hue_travel.csproj /restore /p:Configuration=Debug
& "$env:WINDIR\SysWOW64\WindowsPowerShell\v1.0\powershell.exe" -File may-khach\kiem-thu\test_services.ps1
powershell -ExecutionPolicy Bypass -File may-khach\dong-goi.ps1 -Version 1.0.0 -Server http://IP_VPS
```
