<#
.SYNOPSIS
  Đóng gói phần MÁY CHỦ của Cố Đô GIS thành một tệp .tar.gz (cách thay thế cho "git clone" khi VPS không vào được GitHub).

.DESCRIPTION
  Gói chính là thư mục may-chu/ (Docker Compose, Caddy, mã API, cơ sở dữ liệu, dữ liệu shapefile, script vận hành,
  tài liệu). KHÔNG gồm ứng dụng Windows, bộ cài, tệp .env (bí mật) và thư mục bin/obj.

  Chạy từ bất kỳ đâu:   powershell -ExecutionPolicy Bypass -File may-chu\dong-goi.ps1 -Version 1.0.0
  Kết quả:              dist\CoDoGIS-MayChu-1.0.0.tar.gz

  Các tệp văn bản được đổi sang kết thúc dòng LF (Linux) khi đóng gói, nên chạy được trên VPS dù soạn trên Windows.
#>
param(
    [string]$Version = "1.0.0",
    [string]$OutDir = "dist"
)

$ErrorActionPreference = "Stop"
$serverDir = $PSScriptRoot
$root = Split-Path -Parent $serverDir
Set-Location $root

$name = "CoDoGIS-MayChu-$Version"
$stage = Join-Path $root (Join-Path $OutDir $name)
if (Test-Path $stage) { Remove-Item -Recurse -Force $stage }
New-Item -ItemType Directory -Force $stage | Out-Null

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

Write-Host "== sao chép thư mục may-chu"
Copy-Tree $serverDir (Join-Path $stage "may-chu") @("bin", "obj")
Remove-Item -Force (Join-Path $stage "may-chu\dong-goi.ps1")

# Máy chủ cần đủ các lớp dữ liệu mà co-so-du-lieu/run.sh nạp.
$required = @("diemDuLich", "diemAnUong", "diemLuuTru", "diemDVDuLich", "diemDMGiaoThong", "diemHanhChinh",
              "rgXa", "MangGiaoThong", "NutMangGiaoThong", "DiaPhan_Tinh_2025")
foreach ($layer in $required) {
    if (-not (Test-Path (Join-Path $stage "may-chu\du-lieu\shp\$layer.shp"))) { throw "Thiếu lớp dữ liệu $layer.shp trong may-chu\du-lieu\shp" }
}

Copy-Item (Join-Path $root "README.md") (Join-Path $stage "README.md")
Copy-Tree (Join-Path $root "tai-lieu") (Join-Path $stage "tai-lieu") @()

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
Write-Host "Rồi:            cd may-chu và làm theo may-chu/HUONG-DAN-CAI-DAT.md"
