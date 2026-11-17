#!/usr/bin/env bash
# Tự đồng bộ bản phát hành mới nhất từ GitHub Releases lên server để máy khách tự cập nhật.
# Chạy định kỳ bằng cron (xem VAN-HANH.md). Cần `gh` đã đăng nhập (hoặc repo công khai).
# Dùng: scripts/sync-release.sh [owner/repo]
set -euo pipefail

cd "$(dirname "$0")/.."
REPO="${1:-${GITHUB_REPO:-cuong-bo/HQT_CSDL}}"
BASE="${SYNC_BASE_URL:-http://localhost:8082}"

tag="$(gh release view -R "$REPO" --json tagName -q .tagName 2>/dev/null || true)"
[[ -n "$tag" ]] || { echo "Không đọc được bản phát hành của $REPO"; exit 0; }
ver="${tag#v}"
[[ "$ver" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]] || { echo "Tag '$tag' không phải x.y.z, bỏ qua"; exit 0; }

current="$(curl -fsS "$BASE/v1/client/manifest" 2>/dev/null \
  | python3 -c 'import sys,json; print(json.load(sys.stdin).get("version",""))' 2>/dev/null || true)"
if [[ "$current" == "$ver" ]]; then
  echo "Đã là bản $ver, không có gì để làm"
  exit 0
fi

tmp="$(mktemp -d)"
trap 'rm -rf "$tmp"' EXIT
name="HueTravel-Setup-$ver.exe"
gh release download "$tag" -R "$REPO" -p "$name" -D "$tmp"
notes="$(gh release view "$tag" -R "$REPO" --json name -q .name)"

echo "Phát hành $ver (hiện tại: ${current:-chưa có})"
scripts/deploy.sh publish-client "$tmp/$name" "$ver" "$notes"
