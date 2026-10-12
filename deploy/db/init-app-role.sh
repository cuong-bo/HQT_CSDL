#!/usr/bin/env bash
# Runs once, when the database volume is created. The API connects as a limited role (no superuser, no DDL):
# it can read and write rows but cannot create or drop tables. Objects created later by "huegis" (the migrations)
# automatically grant it the rights below. (V007 takes UPDATE and DELETE on audit_log away again.)
set -euo pipefail

psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "$POSTGRES_DB" <<SQL
create role huegis_app login password '${APP_DB_PASSWORD}';
grant connect on database ${POSTGRES_DB} to huegis_app;
grant usage on schema public to huegis_app;
alter default privileges for role ${POSTGRES_USER} in schema public
    grant select, insert, update, delete on tables to huegis_app;
alter default privileges for role ${POSTGRES_USER} in schema public
    grant usage, select on sequences to huegis_app;
SQL
