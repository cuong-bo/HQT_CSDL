#!/usr/bin/env bash
# Nightly database backup. Keeps 7 daily and 4 weekly dumps; optionally copies the newest one off the server.
#
#   crontab -e (as deploy):   30 2 * * *  /opt/hue-travel/may-chu/scripts/backup.sh >> /var/log/hue-backup.log 2>&1
#
# BACKUP_REMOTE=user@other-host:/path   copies the dump there with scp (needs a key without a passphrase).
set -euo pipefail
cd "$(dirname "$0")/.."

dir="${BACKUP_DIR:-/var/backups/hue-travel}"
mkdir -p "$dir/daily" "$dir/weekly"
stamp="$(date +%F)"
out="$dir/daily/huegis-$stamp.dump"

docker compose exec -T db pg_dump -U huegis -Fc huegis > "$out.tmp"
mv "$out.tmp" "$out"
if [ "$(date +%u)" = 7 ]; then cp "$out" "$dir/weekly/huegis-$stamp.dump"; fi

# "|| true": ls exits 2 when a folder has no dump yet (every non-Sunday for weekly/), which pipefail would turn into a failed backup.
ls -1t "$dir"/daily/*.dump 2>/dev/null | tail -n +8 | xargs -r rm -- || true
ls -1t "$dir"/weekly/*.dump 2>/dev/null | tail -n +5 | xargs -r rm -- || true

if [ -n "${BACKUP_REMOTE:-}" ]; then
    scp -q -o BatchMode=yes "$out" "$BACKUP_REMOTE/"
fi
echo "$(date -Is) backup ok: $out ($(du -h "$out" | cut -f1))"
