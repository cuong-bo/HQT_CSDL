#!/usr/bin/env python3
"""Đưa bộ cài máy khách lên máy chủ để các máy khách tự cập nhật.

    publish-client.py <HueTravel-Setup-x.y.z.exe> <phiên bản x.y.z> <thư mục xuất> [ghi chú]

Chạy trong container (dịch vụ "publish-client" trong docker-compose.yml, hoặc `scripts/deploy.sh publish-client <tệp> <phiên bản>`).
Ghi vào thư mục xuất:

    <xuất>/<phiên bản>/<tên tệp>.exe    bộ cài (mỗi phiên bản một thư mục nên không bao giờ bị ghi đè)
    <xuất>/latest.json                  bản kê của phiên bản mới nhất (API trả nguyên văn ở /v1/client/manifest)

Máy khách có phiên bản thấp hơn bản này sẽ bị buộc cập nhật: không cập nhật thì không dùng được.
"""
import hashlib
import json
import os
import re
import shutil
import sys
from datetime import datetime, timezone

KEEP_VERSIONS = 3
VERSION = re.compile(r"^\d{1,4}\.\d{1,4}\.\d{1,4}$")


def fail(message):
    print("publish-client: " + message, file=sys.stderr)
    sys.exit(1)


def version_key(text):
    return tuple(int(part) for part in text.split("."))


def main():
    if len(sys.argv) < 4:
        fail("cách dùng: publish-client.py <bộ cài .exe> <phiên bản x.y.z> <thư mục xuất> [ghi chú]")
    source, version, dist = sys.argv[1:4]
    notes = sys.argv[4] if len(sys.argv) > 4 else ""
    if not VERSION.match(version):
        fail("phiên bản phải có dạng x.y.z (ví dụ 1.1.0)")
    if not os.path.isfile(source):
        fail("không thấy tệp " + source)

    latest_path = os.path.join(dist, "latest.json")
    if os.path.isfile(latest_path):
        with open(latest_path, encoding="utf-8") as stream:
            current = json.load(stream).get("version", "0.0.0")
        if version_key(version) < version_key(current):
            fail("phiên bản %s thấp hơn bản đang phát hành (%s); máy khách sẽ không bao giờ nhận bản thấp hơn" % (version, current))

    with open(source, "rb") as stream:
        head = stream.read(2)
    if head != b"MZ":
        fail("tệp không phải chương trình Windows (.exe)")

    name = "HueTravel-Setup-%s.exe" % version
    folder = os.path.join(dist, version)
    os.makedirs(folder, exist_ok=True)
    target = os.path.join(folder, name)
    shutil.copyfile(source, target)

    digest = hashlib.sha256()
    with open(target, "rb") as stream:
        for block in iter(lambda: stream.read(1 << 20), b""):
            digest.update(block)

    manifest = {
        "version": version,
        "required": True,
        "installer": {
            "path": "/static/client/%s/%s" % (version, name),
            "size": os.path.getsize(target),
            "sha256": digest.hexdigest(),
        },
        "notes": notes,
        "publishedAt": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
    }
    temporary = latest_path + ".tmp"
    with open(temporary, "w", encoding="utf-8") as stream:
        json.dump(manifest, stream, ensure_ascii=False, indent=2)
    os.replace(temporary, latest_path)   # máy khách không bao giờ đọc phải một tệp ghi dở

    folders = sorted((d for d in os.listdir(dist) if VERSION.match(d)), key=version_key, reverse=True)
    for old in folders[KEEP_VERSIONS:]:
        shutil.rmtree(os.path.join(dist, old), ignore_errors=True)

    print("Đã phát hành %s (%d MB, sha256 %s...)" % (version, manifest["installer"]["size"] // 1048576, manifest["installer"]["sha256"][:12]))


main()
