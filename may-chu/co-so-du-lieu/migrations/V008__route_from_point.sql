-- Routing from a free point (a start picked on the map): the point enters the graph at the nearest vertex of the
-- connected vehicle network, like a POI does through poi_snap.

-- The nearest vertex to a point in VN-2000 / UTM 48N metres, and how far it is.
create or replace function snap_vertex(px double precision, py double precision)
returns table (vertex_id bigint, dist_m double precision)
language sql stable
as $$
    select v.id, st_distance(v.geom, st_setsrid(st_makepoint(px, py), 3405))
    from ways_vertices v
    where v.lien_thong
    order by v.geom <-> st_setsrid(st_makepoint(px, py), 3405)
    limit 1
$$;

-- Cost from one vertex to each of the given POIs (seconds when by_time, otherwise metres); null when unreachable.
create or replace function route_cost_from_vertex(from_vertex bigint, poi_ids text[], by_time boolean default true)
returns table (to_id text, cost double precision)
language plpgsql stable
as $$
declare
    edges_sql text;
    target_vertices bigint[];
begin
    edges_sql := format(
        'select gid as id, source, target, %I as cost, %I as reverse_cost from ways where cho_xe and lien_thong',
        case when by_time then 'cost_time' else 'cost' end,
        case when by_time then 'reverse_cost_time' else 'reverse_cost' end);
    select array_agg(distinct s.vertex_id) into target_vertices from poi_snap s where s.poi_id = any (poi_ids);
    if target_vertices is null then return; end if;

    return query
    select s.poi_id,
           case when s.vertex_id = from_vertex then 0::double precision else d.agg_cost end
    from poi_snap s
    left join pgr_dijkstracost(edges_sql, from_vertex, target_vertices, true) d on d.end_vid = s.vertex_id
    where s.poi_id = any (poi_ids);
end
$$;

-- The road path from a vertex to a POI, one row per road segment in travel order (same shape as route_path).
create or replace function route_path_from_vertex(from_vertex bigint, to_poi text, by_time boolean default true)
returns table (seq integer, edge_id bigint, ten text, length_m double precision, seconds double precision, geom geometry)
language plpgsql stable
as $$
declare
    edges_sql text;
    to_vertex bigint;
    area geometry;
begin
    edges_sql := format(
        'select gid as id, source, target, %I as cost, %I as reverse_cost from ways where cho_xe and lien_thong',
        case when by_time then 'cost_time' else 'cost' end,
        case when by_time then 'reverse_cost_time' else 'reverse_cost' end);
    select vertex_id into to_vertex from poi_snap where poi_id = to_poi;
    if from_vertex is null or to_vertex is null or from_vertex = to_vertex then return; end if;

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
