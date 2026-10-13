<#
.SYNOPSIS
  Đóng gói phần MÁY KHÁCH (ứng dụng Windows) của Hue Travel: dựng bản Release và làm bộ cài.

.DESCRIPTION
  1. Dựng Hue_travel.csproj ở chế độ Release (32-bit, .NET Framework 4.7.2).
  2. Nếu có Inno Setup 6 (ISCC.exe): làm bộ cài  dist\HueTravel-Setup-<phiên bản>.exe  (có MapWinGIS bên trong).
     Nếu không có: tạo bản "mang đi" dist\HueTravel-MayKhach-<phiên bản>.zip và nhắc cài Inno Setup.

  Chạy từ gốc kho mã:
    powershell -ExecutionPolicy Bypass -File may-khach/dong-goi.ps1 -Version 1.0.0 -Server http://IP_VPS

  -Server và -PublicKey chỉ là giá trị điền sẵn trong ô nhập của bộ cài (người cài vẫn sửa được); để trống nếu chưa có máy chủ.
#>
param(
    [string]$Version = "1.0.0",
    [string]$Server = "",
    [string]$PublicKey = "",
    [string]$MapWinGisDir = "C:\Program Files\MapWinGIS",
    [string]$OutDir = "dist",
    [string]$SourceDir = ""   # đóng gói nguyên thư mục này (ví dụ bin\Debug đang chạy), không dựng lại
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
Set-Location $root

function Find-Tool($name, [string[]]$candidates) {
    foreach ($path in $candidates) { if ($path -and (Test-Path $path)) { return $path } }
    $found = Get-Command $name -ErrorAction SilentlyContinue
    if ($found) { return $found.Source }
    return $null
}

# --- MSBuild
$vswhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"
$msbuild = $null
if (Test-Path $vswhere) {
    $msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" | Select-Object -First 1
}
if (-not $msbuild) { $msbuild = Find-Tool "msbuild.exe" @() }
if (-not $msbuild) { throw "Không tìm thấy MSBuild. Cài Visual Studio 2019/2022 (có khối lượng công việc .NET desktop) hoặc Build Tools." }
if (-not (Test-Path $MapWinGisDir)) { throw "Không tìm thấy thư mục MapWinGIS: $MapWinGisDir (dùng -MapWinGisDir để chỉ đường dẫn khác)." }

if ($SourceDir) {
    $release = (Resolve-Path $SourceDir).Path
    if (-not (Test-Path (Join-Path $release "Hue_travel.exe"))) { throw "Không thấy Hue_travel.exe trong $release" }
    Write-Host "== dùng nguyên thư mục có sẵn: $release"
    # Chép sang thư mục tạm trước để phần mềm khác (ví dụ ArcMap) đang giữ tệp dữ liệu không làm hỏng bước làm bộ cài.
    $copy = Join-Path $root (Join-Path $OutDir "nguon-tam")
    if (Test-Path $copy) { Remove-Item -Recurse -Force $copy }
    New-Item -ItemType Directory -Force $copy | Out-Null
    & robocopy.exe $release $copy /E /R:1 /W:1 /NFL /NDL /NJH /NJS /NP /XF mwtiles.db3 *.lock *.pdb NutMangGiaoThong.* | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "Không chép được thư mục $release (mã robocopy $LASTEXITCODE)" }
    $release = $copy
}
else {
    Write-Host "== dựng bản Release"
    & $msbuild "ung-dung/Hue_travel.csproj" /restore /nologo /v:m /p:Configuration=Release
    if ($LASTEXITCODE -ne 0) { throw "Dựng bản Release thất bại" }
    $release = Join-Path $root "ung-dung/bin/Release"
    if (-not (Test-Path (Join-Path $release "Hue_travel.exe"))) { throw "Không thấy Hue_travel.exe trong $release" }
}

# Dữ liệu chỉ để máy chủ tính toán không đi kèm máy khách (csproj đã loại). Kiểm tra lại cho chắc.
$shp = Join-Path $release "Data\shp"
foreach ($mustNot in @("NutMangGiaoThong.shp", "NutMangGiaoThong.dbf")) {
    if (-not $SourceDir -and (Test-Path (Join-Path $shp $mustNot))) { throw "$mustNot không được nằm trong bản máy khách (chỉ dành cho máy chủ)." }
}

New-Item -ItemType Directory -Force (Join-Path $root $OutDir) | Out-Null

# --- Inno Setup
$iscc = Find-Tool "ISCC.exe" @(
    (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe"),
    (Join-Path $env:ProgramFiles "Inno Setup 6\ISCC.exe"),
    (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe"))

if ($iscc) {
    Write-Host "== làm bộ cài"
    $arguments = @("bo-cai/HueTravel.iss", "/DAppVersion=$Version", "/DMapWinGisDir=$MapWinGisDir", "/DSourceDir=$release", "/O$OutDir")
    if ($Server) { $arguments += "/DDefaultServer=$Server" }
    if ($PublicKey) { $arguments += "/DDefaultPublicKey=$PublicKey" }
    & $iscc @arguments
    if ($LASTEXITCODE -ne 0) { throw "Inno Setup thất bại" }
    Write-Host ""
    Write-Host "Xong: $(Join-Path $OutDir "HueTravel-Setup-$Version.exe")"
}
else {
    Write-Host "== không có Inno Setup 6: tạo bản mang đi (.zip)"
    $stage = Join-Path $root (Join-Path $OutDir "HueTravel-MayKhach-$Version")
    if (Test-Path $stage) { Remove-Item -Recurse -Force $stage }
    New-Item -ItemType Directory -Force $stage | Out-Null
    Copy-Item -Recurse -Force (Join-Path $release "*") $stage
    Copy-Item -Recurse -Force $MapWinGisDir (Join-Path $stage "MapWinGIS")
    Get-ChildItem $stage -Recurse -Include "*.pdb" | Remove-Item -Force
    $zip = Join-Path $root (Join-Path $OutDir "HueTravel-MayKhach-$Version.zip")
    if (Test-Path $zip) { Remove-Item -Force $zip }
    Compress-Archive -Path (Join-Path $stage "*") -DestinationPath $zip
    Write-Host ""
    Write-Host "Xong (bản mang đi): $zip"
    Write-Host "Muốn có bộ cài .exe: cài Inno Setup 6 (https://jrsoftware.org/isinfo.php) rồi chạy lại script này."
    Write-Host "Bản mang đi cần tự đăng ký MapWinGIS:  regsvr32 MapWinGIS\MapWinGIS.ocx  (quyền quản trị) và tạo %ProgramData%\HueTravel\server.txt."
}
