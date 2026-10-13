#!/usr/bin/env bash
# Dọn sạch toàn bộ dự án hue-travel (Cố Đô GIS), KHÔNG đụng tới ecoai.
# An toàn: chỉ thao tác trên tài nguyên thuộc compose project "hue-travel"
# và rule ufw cổng 8082 do dự án này tạo ra; không bao giờ match "ecoai".
set -euo pipefail

PROJECT_DIR="/root/hue-travel"
COMPOSE_FILE="$PROJECT_DIR/may-chu/docker-compose.yml"
NGINX_SITE="${1:-}"   # truyền tên domain làm tham số nếu đã tạo site nginx riêng cho dự án này

echo "== 1. Dừng & xoá container/network/volume của project hue-travel =="
if [ -f "$COMPOSE_FILE" ]; then
    docker compose -f "$COMPOSE_FILE" -p hue-travel down -v --remove-orphans
else
    echo "   (không thấy $COMPOSE_FILE, bỏ qua)"
fi

echo "== 2. Quét tìm tài nguyên Docker còn sót có tiền tố hue-travel =="
docker ps -a --filter "name=hue-travel" --format '{{.Names}}' | while read -r c; do
    echo "   xoá container còn sót: $c"
    docker rm -f "$c"
done
docker volume ls --filter "name=hue-travel" --format '{{.Name}}' | while read -r v; do
    echo "   xoá volume còn sót: $v"
    docker volume rm "$v"
done
docker network ls --filter "name=hue-travel" --format '{{.Name}}' | while read -r n; do
    echo "   xoá network còn sót: $n"
    docker network rm "$n"
done
for img in hue-travel-api hue-travel-db hue-travel-tools; do
    docker image inspect "$img:latest" >/dev/null 2>&1 && { echo "   xoá image còn sót: $img"; docker image rm "$img:latest"; } || true
done

echo "== 3. Gỡ rule tường lửa cổng 8082 (mở riêng cho dự án này) =="
if command -v ufw >/dev/null 2>&1; then
    ufw delete allow 8082/tcp 2>/dev/null || true
    echo "   đã gỡ (nếu có)."
else
    echo "   (không có ufw, bỏ qua)"
fi

if [ -n "$NGINX_SITE" ]; then
    echo "== 4. Xoá nginx site: $NGINX_SITE =="
    rm -f "/etc/nginx/sites-enabled/$NGINX_SITE"
    rm -f "/etc/nginx/sites-available/$NGINX_SITE"
    nginx -t && systemctl reload nginx
    echo "   Lưu ý: nếu có chứng chỉ Let's Encrypt cho domain này, xoá thêm bằng:"
    echo "   certbot delete --cert-name $NGINX_SITE"
else
    echo "== 4. Bỏ qua xoá nginx (chưa có site riêng / không truyền tên domain) =="
fi

echo "== Xong. Kiểm tra lại ecoai vẫn chạy bình thường: =="
docker ps --filter "name=ecoai"

echo
echo "Để xoá luôn thư mục dự án (gồm cả mã nguồn đã clone), chạy thủ công"
echo "(không tự động để tránh mất nhầm dữ liệu/khoá bí mật trong may-chu/.env):"
echo "  rm -rf $PROJECT_DIR"
