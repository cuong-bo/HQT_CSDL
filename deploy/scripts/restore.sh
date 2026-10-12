#!/usr/bin/env bash
# Restores a dump made by backup.sh.
#   restore.sh FILE              restore into a NEW database "huegis_restore" (a rehearsal; production is untouched)
#   restore.sh FILE --replace    replace the production database (stops the API while it runs)
set -euo pipefail
cd "$(dirname "$0")/.."

file="${1:?usage: restore.sh FILE [--replace]}"
[ -f "$file" ] || { echo "no such file: $file" >&2; exit 1; }

if [ "${2:-}" = "--replace" ]; then
    docker compose stop api
    docker compose exec -T db psql -U huegis -d postgres -v ON_ERROR_STOP=1 \
        -c "select pg_terminate_backend(pid) from pg_stat_activity where datname = 'huegis' and pid <> pg_backend_pid()" \
        -c "drop database huegis" -c "create database huegis owner huegis"
    docker compose exec -T db pg_restore -U huegis -d huegis --no-owner < "$file"
    docker compose up -d api
    echo "production database replaced from $file"
else
    docker compose exec -T db psql -U huegis -d postgres -v ON_ERROR_STOP=1 \
        -c "drop database if exists huegis_restore" -c "create database huegis_restore owner huegis"
    docker compose exec -T db pg_restore -U huegis -d huegis_restore --no-owner < "$file"
    docker compose exec -T db psql -U huegis -d huegis_restore -Atc "select 'poi', count(*) from poi union all select 'ways', count(*) from ways union all select 'users', count(*) from app_user"
    echo "restored into huegis_restore (drop it when you are done: drop database huegis_restore)"
fi
