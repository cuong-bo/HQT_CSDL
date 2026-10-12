#!/usr/bin/env bash
# Builds the database from scratch (or updates it): migrations -> load shapefiles -> transform -> routing -> verify.
#
#   PGHOST=... PGPORT=... PGUSER=... PGPASSWORD=... PGDATABASE=huegis ./db/run.sh [folder with the .shp files]
#
# Needs psql and shp2pgsql (both come with the postgresql-client and postgis packages). The database must exist.
set -euo pipefail

here="$(cd "$(dirname "$0")" && pwd)"
shp_dir="${1:-$here/../DuLieu/DuLieu}"
: "${PGDATABASE:?set PGDATABASE (and PGHOST, PGPORT, PGUSER, PGPASSWORD as needed)}"
psql_q=(psql -v ON_ERROR_STOP=1 -q)

echo "== migrations"
"${psql_q[@]}" -c "create table if not exists schema_migration (version text primary key, applied_at timestamptz not null default now())"
for file in "$here"/migrations/V*.sql; do
    version="$(basename "$file" .sql)"
    done_already="$("${psql_q[@]}" -Atc "select 1 from schema_migration where version = '$version'")"
    [ "$done_already" = "1" ] && continue
    echo "   apply $version"
    "${psql_q[@]}" -1 -f "$file" -c "insert into schema_migration (version) values ('$version')"
done

echo "== load shapefiles from $shp_dir"
"${psql_q[@]}" -c "drop schema if exists stg cascade; create schema stg"
for layer in diemDuLich diemLuuTru diemDVDuLich diemDMGiaoThong diemHanhChinh rgTinh rgXa mangGiaothong; do
    [ -f "$shp_dir/$layer.shp" ] || { echo "missing $shp_dir/$layer.shp" >&2; exit 1; }
    shp2pgsql -s 3405 -g geom -D -W UTF-8 -c "$shp_dir/$layer.shp" "stg.$layer" 2>/dev/null | "${psql_q[@]}" >/dev/null
    echo "   $layer"
done

echo "== transform"
"${psql_q[@]}" -f "$here/import/transform.sql"
echo "== routing graph"
"${psql_q[@]}" -f "$here/import/build_routing.sql"
echo "== verify"
psql -v ON_ERROR_STOP=1 -f "$here/import/verify.sql"
echo "done"
