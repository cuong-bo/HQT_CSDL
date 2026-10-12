# PostgreSQL 17 from the official image, plus PostGIS and pgRouting from the PostgreSQL apt repository (already configured
# in that image). The "postgis" package brings shp2pgsql, which db/run.sh needs; psql is already in the image.
# Not the postgis/postgis image: its 17-3.5 tag is built on Debian bullseye, whose PostgreSQL apt repository is gone.
FROM postgres:17-bookworm

RUN apt-get update \
    && apt-get install -y --no-install-recommends postgresql-17-postgis-3 postgresql-17-pgrouting postgis \
    && rm -rf /var/lib/apt/lists/*

COPY init-app-role.sh /docker-entrypoint-initdb.d/20-app-role.sh
