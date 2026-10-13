#!/usr/bin/env bash
# Day-to-day operations on the VPS.  Usage: scripts/deploy.sh <command>
#   up        start everything (first time: also run "import")
#   import    build / refresh the database from du-lieu/shp (safe to repeat; keeps edits made through the app),
#             then pack the base map for the app to download
#   pack      only pack the base map (du-lieu/shp + du-lieu/nen-ban-do) into the package the app downloads
#   update    git pull, rebuild the API, restart it; goes back to the previous image if /healthz does not answer
#   rollback  put the previous API image back
#   status    containers and health
#   logs      follow the logs
set -euo pipefail
cd "$(dirname "$0")/.."

[ -f .env ] || { echo ".env is missing: copy .env.example and fill it in" >&2; exit 1; }
compose=(docker compose)

healthy() {
    # Cổng 8082, không phải 80: cổng 80 trên máy này do nginx (dự án ecoai)
    # chiếm, xem /root/hue-travel/DEPLOY_ISOLATION.md.
    for _ in $(seq 1 30); do
        if curl -fsS http://localhost:8082/healthz >/dev/null 2>&1; then return 0; fi
        sleep 2
    done
    return 1
}

case "${1:-}" in
    up)
        "${compose[@]}" up -d --build
        healthy && echo "healthy" || { echo "API did not become healthy" >&2; exit 1; }
        ;;
    import)
        "${compose[@]}" up -d db
        "${compose[@]}" --profile tools run --rm tools
        "$0" pack
        ;;
    pack)
        "${compose[@]}" --profile tools run --rm pack-basemap
        ;;
    update)
        git pull --ff-only
        docker tag hue-travel-api:latest hue-travel-api:previous 2>/dev/null || true
        "${compose[@]}" build api
        "${compose[@]}" up -d api caddy
        if healthy; then echo "updated"; else
            echo "new version is not healthy: rolling back" >&2
            "$0" rollback
            exit 1
        fi
        ;;
    rollback)
        docker image inspect hue-travel-api:previous >/dev/null 2>&1 || { echo "no previous image" >&2; exit 1; }
        docker tag hue-travel-api:previous hue-travel-api:latest
        "${compose[@]}" up -d --no-build api
        healthy && echo "rolled back" || { echo "still not healthy after rollback" >&2; exit 1; }
        ;;
    status)
        "${compose[@]}" ps
        curl -fsS http://localhost:8082/healthz && echo
        ;;
    logs)
        "${compose[@]}" logs -f --tail=100
        ;;
    *)
        sed -n '2,11p' "$0"
        exit 1
        ;;
esac
