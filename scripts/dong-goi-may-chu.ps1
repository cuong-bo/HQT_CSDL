<#
.SYNOPSIS
  Đóng gói phần MÁY CHỦ của Cố Đô GIS thành một tệp .tar.gz để chép lên VPS.

.DESCRIPTION
  Gói gồm: deploy (Docker Compose, Caddy, script vận hành), db (migration và nạp dữ liệu), mã API,
  các lớp dữ liệu shapefile mà máy chủ cần, và tài liệu hướng dẫn cài đặt máy chủ.
  KHÔNG gồm ứng dụng Windows, bộ cài, tệp .env (bí mật) và thư mục bin/obj.

  Chạy từ gốc kho mã:   powershell -ExecutionPolicy Bypass -File scripts\dong-goi-may-chu.ps1 -Version 1.0.0
  Kết quả:              dist\CoDoGIS-MayChu-1.0.0.tar.gz

  Các tệp văn bản được đổi sang kết thúc dòng LF (Linux) khi đóng gói, nên chạy được trên VPS dù soạn trên Windows.
#>
param(
    [string]$Version = "1.0.0",
    [string]$OutDir = "dist"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$name = "CoDoGIS-MayChu-$Version"
$stage = Join-Path $root (Join-Path $OutDir $name)
if (Test-Path $stage) { Remove-Item -Recurse -Force $stage }
New-Item -ItemType Directory -Force $stage | Out-Null

# Các lớp dữ liệu máy chủ cần (xem db/run.sh). Các lớp chỉ để vẽ nền ở máy khách (NenBien, DL_Nc_Ngoai, song_*) không cần.
$serverLayers = @(
    "diemDuLich", "diemAnUong", "diemLuuTru", "diemDVDuLich", "diemDMGiaoThong", "diemHanhChinh",
    "rgXa", "MangGiaoThong", "NutMangGiaoThong", "DiaPhan_Tinh_2025"
)
$textExtensions = @(".sh", ".sql", ".yml", ".yaml", ".md", ".json", ".cs", ".csproj", ".conf", ".txt", ".example")
$textNames = @("Caddyfile", "Dockerfile", ".env.example", ".gitignore", ".dockerignore")

function Copy-Tree($from, $to, [string[]]$skipDirs) {
    New-Item -ItemType Directory -Force $to | Out-Null
    Get-ChildItem -LiteralPath $from -Force | ForEach-Object {
        if ($_.PSIsContainer) {
            if ($skipDirs -contains $_.Name) { return }
            Copy-Tree $_.FullName (Join-Path $to $_.Name) $skipDirs
        }
        elseif ($_.Name -ne ".env" -and $_.Extension -ne ".lock") {
            Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $to $_.Name)
        }
    }
}

Write-Host "== sao chép các phần máy chủ"
Copy-Tree (Join-Path $root "deploy") (Join-Path $stage "deploy") @()
Copy-Tree (Join-Path $root "db") (Join-Path $stage "db") @()
Copy-Tree (Join-Path $root "src\Hue_travel.Api") (Join-Path $stage "src\Hue_travel.Api") @("bin", "obj")

$dataTo = Join-Path $stage "Hue_travel\Data\shp"
New-Item -ItemType Directory -Force $dataTo | Out-Null
$dataFrom = Join-Path $root "Hue_travel\Data\shp"
foreach ($layer in $serverLayers) {
    $files = @(Get-ChildItem -LiteralPath $dataFrom -Filter "$layer.*" | Where-Object { $_.Extension -in ".shp", ".shx", ".dbf", ".prj", ".cpg" })
    if (-not ($files | Where-Object { $_.Extension -eq ".shp" })) { throw "Thiếu lớp dữ liệu $layer.shp trong $dataFrom" }
    $files | ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $dataTo }
}

Copy-Item (Join-Path $root "docs\huong-dan-cai-dat-may-chu.md") (Join-Path $stage "HUONG-DAN-CAI-DAT.md")
Copy-Item (Join-Path $root "docs\phan-chia-may-chu-may-khach.md") (Join-Path $stage "PHAN-CHIA-MAY-CHU-MAY-KHACH.md")

Write-Host "== đổi kết thúc dòng sang LF cho tệp văn bản"
Get-ChildItem -LiteralPath $stage -Recurse -File | Where-Object {
    $textExtensions -contains $_.Extension -or $textNames -contains $_.Name
} | ForEach-Object {
    $bytes = [IO.File]::ReadAllBytes($_.FullName)
    $text = [Text.Encoding]::UTF8.GetString($bytes)
    if ($text.Contains("`r")) {
        $hasBom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
        if ($hasBom) { $text = $text.Substring(1) }
        [IO.File]::WriteAllBytes($_.FullName, (New-Object Text.UTF8Encoding($false)).GetBytes($text.Replace("`r`n", "`n").Replace("`r", "`n")))
    }
}

Write-Host "== nén"
$archive = Join-Path (Join-Path $root $OutDir) "$name.tar.gz"
if (Test-Path $archive) { Remove-Item -Force $archive }
# Dùng đường dẫn tương đối: tar của Git coi "C:\..." là máy ở xa.
Push-Location (Join-Path $root $OutDir)
try {
    & tar.exe -czf "$name.tar.gz" $name
    if ($LASTEXITCODE -ne 0) { throw "tar thất bại" }
}
finally { Pop-Location }

$sizeMb = [math]::Round((Get-Item $archive).Length / 1MB, 1)
Write-Host ""
Write-Host "Xong: $archive ($sizeMb MB)"
Write-Host "Chép lên VPS:   scp $archive deploy@IP_VPS:/opt/hue-travel/"
Write-Host "Giải nén:       cd /opt/hue-travel && tar -xzf $name.tar.gz --strip-components=1"
Write-Host "Rồi làm theo HUONG-DAN-CAI-DAT.md trong gói."
