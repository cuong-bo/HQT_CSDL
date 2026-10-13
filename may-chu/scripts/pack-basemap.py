#!/usr/bin/env python3
"""Đóng gói dữ liệu bản đồ nền cho máy khách tải về.

    pack-basemap.py <thư mục shp> <thư mục nen-ban-do> <thư mục xuất>

Chạy trong container (xem dịch vụ "pack-basemap" trong docker-compose.yml, hoặc `scripts/deploy.sh pack`).
Gom các shapefile máy khách cần vẽ (lớp địa điểm, đường, ranh giới trong du-lieu/shp, cộng các lớp chỉ để vẽ nền trong
du-lieu/nen-ban-do), trừ NutMangGiaoThong (chỉ để máy chủ tính đường), rồi ghi vào thư mục xuất:

    <xuất>/<phiên bản>/package.zip      gói nén, tên thư mục chứa phiên bản nên không bao giờ bị ghi đè
    <xuất>/<phiên bản>/manifest.json
    <xuất>/latest.json                  bản kê của phiên bản mới nhất (API trả nguyên văn ở /v1/basemap/manifest)

Phiên bản = 16 ký tự đầu của sha256 trên danh sách "tên + sha256" từng tệp: dữ liệu không đổi thì phiên bản không đổi.
"""
import hashlib
import json
import os
import shutil
import sys
import zipfile
from datetime import datetime, timezone

# Không đi kèm máy khách: lớp chỉ phục vụ tính đường, và các tệp phụ do phần mềm GIS sinh ra.
SKIP_PREFIXES = ("NutMangGiaoThong.",)
SKIP_SUFFIXES = (".xml", ".sbn", ".sbx", ".lock")

# Máy khách (LocalBaseMap, PoiCatalog) mở các lớp này theo tên; thiếu một lớp là bản đồ hoặc địa điểm bị hỏng.
REQUIRED_LAYERS = (
    "NenBien", "DL_Nc_Ngoai", "DiaPhan_Tinh_2025", "rgXa", "song_1n", "song_2n", "MangGiaoThong",
    "diemDuLich", "diemAnUong", "diemLuuTru", "diemDVDuLich", "diemDMGiaoThong", "diemHanhChinh",
)

KEEP_VERSIONS = 2   # phiên bản mới nhất và một bản trước nó (máy khách đang tải dở vẫn xong được)
FIXED_TIME = (1980, 1, 1, 0, 0, 0)


def fail(message):
    print("pack-basemap: " + message, file=sys.stderr)
    sys.exit(1)


def sha256_of(path):
    digest = hashlib.sha256()
    with open(path, "rb") as stream:
        for block in iter(lambda: stream.read(1 << 20), b""):
            digest.update(block)
    return digest.hexdigest()


def collect(sources):
    files = {}
    for folder in sources:
        if not os.path.isdir(folder):
            fail("không thấy thư mục " + folder)
        for name in sorted(os.listdir(folder)):
            path = os.path.join(folder, name)
            if not os.path.isfile(path) or name.startswith(SKIP_PREFIXES) or name.lower().endswith(SKIP_SUFFIXES):
                continue
            if name in files:
                fail("tệp %s có ở hai thư mục (%s và %s)" % (name, os.path.dirname(files[name]), folder))
            files[name] = path
    return files


def check_complete(files):
    for layer in REQUIRED_LAYERS:
        for extension in (".shp", ".shx", ".dbf", ".prj"):
            if layer + extension not in files:
                fail("thiếu %s%s: gói thiếu tệp thì bản đồ trên máy khách sẽ hỏng" % (layer, extension))


def write_package(files, names, target):
    # Thời gian và quyền cố định: cùng nội dung thì cùng gói, cùng sha256.
    with zipfile.ZipFile(target, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as archive:
        for name in names:
            info = zipfile.ZipInfo(name, FIXED_TIME)
            info.compress_type = zipfile.ZIP_DEFLATED
            info.external_attr = 0o644 << 16
            with open(files[name], "rb") as source, archive.open(info, "w") as out:
                shutil.copyfileobj(source, out, 1 << 20)


def prune(out_dir, keep):
    versions = [d for d in os.listdir(out_dir) if os.path.isdir(os.path.join(out_dir, d)) and not d.endswith(".tmp")]
    for old in versions:
        if old not in keep:
            shutil.rmtree(os.path.join(out_dir, old))
            print("đã xoá phiên bản cũ " + old)
    for leftover in (d for d in os.listdir(out_dir) if d.endswith(".tmp") and os.path.isdir(os.path.join(out_dir, d))):
        shutil.rmtree(os.path.join(out_dir, leftover), ignore_errors=True)


def main():
    if len(sys.argv) != 4:
        print(__doc__)
        sys.exit(2)
    out_dir = sys.argv[3]
    files = collect(sys.argv[1:3])
    check_complete(files)
    names = sorted(files)

    entries = [{"name": n, "size": os.path.getsize(files[n]), "sha256": sha256_of(files[n])} for n in names]
    version = hashlib.sha256("".join("%s\t%s\n" % (e["name"], e["sha256"]) for e in entries).encode()).hexdigest()[:16]

    os.makedirs(out_dir, exist_ok=True)
    final = os.path.join(out_dir, version)
    if not os.path.isfile(os.path.join(final, "manifest.json")):
        staging = final + ".tmp"
        shutil.rmtree(staging, ignore_errors=True)
        os.makedirs(staging)
        package = os.path.join(staging, "package.zip")
        write_package(files, names, package)
        manifest = {
            "version": version,
            "createdAt": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
            "package": {"path": "/static/basemap/%s/package.zip" % version, "size": os.path.getsize(package), "sha256": sha256_of(package)},
            "totalSize": sum(e["size"] for e in entries),
            "files": entries,
        }
        with open(os.path.join(staging, "manifest.json"), "w", encoding="utf-8") as stream:
            json.dump(manifest, stream, ensure_ascii=False, indent=1)
        shutil.rmtree(final, ignore_errors=True)
        os.rename(staging, final)
        print("đã đóng gói phiên bản %s: %d tệp, %.1f MB nén (%.1f MB gốc)" % (
            version, len(entries), manifest["package"]["size"] / 1e6, manifest["totalSize"] / 1e6))
    else:
        print("phiên bản %s đã có, giữ nguyên" % version)
    os.utime(final)   # "mới nhất" tính theo lần đóng gói gần nhất

    # latest.json được thay nguyên tử: người đọc không bao giờ thấy tệp ghi dở.
    latest_tmp = os.path.join(out_dir, "latest.json.tmp")
    shutil.copyfile(os.path.join(final, "manifest.json"), latest_tmp)
    os.replace(latest_tmp, os.path.join(out_dir, "latest.json"))

    others = [d for d in os.listdir(out_dir) if d != version and os.path.isfile(os.path.join(out_dir, d, "manifest.json"))]
    others.sort(key=lambda d: os.path.getmtime(os.path.join(out_dir, d)), reverse=True)
    prune(out_dir, {version, *others[:KEEP_VERSIONS - 1]})
    # Caddy và API chạy bằng người dùng khác: mọi người đều phải đọc được.
    for root, dirs, names_in in os.walk(out_dir):
        for d in dirs:
            os.chmod(os.path.join(root, d), 0o755)
        for n in names_in:
            os.chmod(os.path.join(root, n), 0o644)


if __name__ == "__main__":
    main()
