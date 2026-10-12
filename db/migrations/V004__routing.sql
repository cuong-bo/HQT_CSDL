-- Road network for pgRouting. Costs: metres (distance) and seconds (time); a negative reverse cost means one-way.
create table ways (
    gid                bigint primary key,
    osm_id             text,
    ten                text,
    road_type          text,
    ref                text,
    source             bigint not null,
    target             bigint not null,
    length_m           double precision not null,
    cost               double precision not null,
    reverse_cost       double precision not null,
    speed_kmh          real,
    cost_time          double precision,
    reverse_cost_time  double precision,
    cho_xe             boolean not null default true,    -- the road type is open to motor vehicles
    lien_thong         boolean not null default false,   -- part of the one big connected network
    bridge             boolean,
    tunnel             boolean,
    geom               geometry(LineString, 3405) not null
);
create index ways_geom_idx on ways using gist (geom);
create index ways_source_idx on ways (source);
create index ways_target_idx on ways (target);
create index ways_vehicle_idx on ways (gid) where cho_xe and lien_thong;

create table ways_vertices (
    id          bigint primary key,
    component   integer,
    lien_thong  boolean not null default false,
    geom        geometry(Point, 3405) not null
);
create index ways_vertices_geom_idx on ways_vertices using gist (geom) where lien_thong;

-- Each POI snapped to the nearest road of the connected vehicle network, entering the graph at that road's nearer end.
create table poi_snap (
    poi_id         text primary key references poi (id) on delete cascade,
    vertex_id      bigint not null references ways_vertices (id),
    dist_m         double precision not null,   -- distance from the POI to the nearest road
    vertex_dist_m  double precision not null,   -- distance from the POI to the vertex it enters the graph at
    xa_duong       boolean not null             -- true when the POI is more than 150 m from any road
);

-- Cost between every pair of the given POIs, in seconds (by_time) or metres; null when unreachable. At most 50 POIs
-- (the work grows with the square of the list).
create or replace function route_cost_matrix(poi_ids text[], by_time boolean default true)
returns table (from_id text, to_id text, cost double precision)
language plpgsql stable
as $$
declare
    edges_sql text;
    vertex_ids bigint[];
begin
    if cardinality(poi_ids) > 50 then
        raise exception 'route_cost_matrix: at most 50 places at a time (got %)', cardinality(poi_ids);
    end if;
    edges_sql := format(
        'select gid as id, source, target, %I as cost, %I as reverse_cost from ways where cho_xe and lien_thong',
        case when by_time then 'cost_time' else 'cost' end,
        case when by_time then 'reverse_cost_time' else 'reverse_cost' end);
    select array_agg(distinct s.vertex_id) into vertex_ids from poi_snap s where s.poi_id = any (poi_ids);
    if vertex_ids is null then return; end if;

    return query
    with m as (select start_vid, end_vid, agg_cost from pgr_dijkstracostmatrix(edges_sql, vertex_ids, true)),
         a as (select s.poi_id, s.vertex_id from poi_snap s where s.poi_id = any (poi_ids))
    select x.poi_id, y.poi_id,
           case when x.vertex_id = y.vertex_id then 0::double precision else m.agg_cost end
    from a x
    cross join a y
    left join m on m.start_vid = x.vertex_id and m.end_vid = y.vertex_id
    where x.poi_id <> y.poi_id;
end
$$;

-- The road path between two POIs, one row per road segment in travel order (geometry oriented along the travel).
-- An empty result means: unknown POI, no route, or both POIs enter the graph at the same vertex (nothing to drive).
-- A POI far from every road still routes from its nearest road; check poi_snap.xa_duong to warn about that.
create or replace function route_path(from_poi text, to_poi text, by_time boolean default true)
returns table (seq integer, edge_id bigint, ten text, length_m double precision, seconds double precision, geom geometry)
language plpgsql stable
as $$
declare
    edges_sql text;
    from_vertex bigint;
    to_vertex bigint;
    area geometry;
begin
    edges_sql := format(
        'select gid as id, source, target, %I as cost, %I as reverse_cost from ways where cho_xe and lien_thong',
        case when by_time then 'cost_time' else 'cost' end,
        case when by_time then 'reverse_cost_time' else 'reverse_cost' end);
    select vertex_id into from_vertex from poi_snap where poi_id = from_poi;
    select vertex_id into to_vertex from poi_snap where poi_id = to_poi;
    if from_vertex is null or to_vertex is null or from_vertex = to_vertex then return; end if;

    -- First try only the roads around the two ends (a fraction of the 62k edges); fall back to the whole network.
    select st_expand(st_collect(v.geom), 5000) into area from ways_vertices v where v.id in (from_vertex, to_vertex);
    perform 1 from pgr_dijkstra(edges_sql || format(' and geom && %L::geometry', area::text), from_vertex, to_vertex, true) limit 1;
    if found then edges_sql := edges_sql || format(' and geom && %L::geometry', area::text); end if;

    return query
    select d.path_seq, w.gid, w.ten, w.length_m,
           case when d.node = w.source then w.cost_time else w.reverse_cost_time end,
           case when d.node = w.source then w.geom else st_reverse(w.geom) end
    from pgr_dijkstra(edges_sql, from_vertex, to_vertex, true) d
    join ways w on w.gid = d.edge
    order by d.path_seq;
end
$$;
