# PostGIS 17 plus pgRouting. The postgis image already ships psql and shp2pgsql, which db/run.sh needs.
FROM postgis/postgis:17-3.5

RUN apt-get update \
    && apt-get install -y --no-install-recommends postgresql-17-pgrouting \
    && rm -rf /var/lib/apt/lists/*

COPY init-app-role.sh /docker-entrypoint-initdb.d/20-app-role.sh
