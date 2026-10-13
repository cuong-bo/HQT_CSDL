-- Routing from a free point (a start picked on the map): the point now enters the graph ON the nearest drivable road
-- segment, between its two end vertices, instead of jumping to the nearest vertex. A start in the middle of a long road
-- therefore costs only the part of the road it really has to travel. (V008's snap_vertex stays for compatibility.)

-- The nearest drivable segment to a point in VN-2000 / UTM 48N metres: where on it the point projects (frac 0 = source
-- end, 1 = target end), how far the point is from the road, and the cost of walking the segment each way.
create or replace function snap_point(px double precision, py double precision)
returns table (edge_id bigint, source bigint, target bigint, frac double precision, dist_m double precision,
               length_m double precision, cost_fwd double precision, cost_rev double precision,
               time_fwd double precision, time_rev double precision)
language sql stable
as $$
    select w.gid, w.source, w.target,
           st_linelocatepoint(w.geom, p.pt),
           st_distance(w.geom, p.pt),
           st_length(w.geom),
           w.cost, w.reverse_cost, w.cost_time, w.reverse_cost_time
    from (select st_setsrid(st_makepoint(px, py), 3405) as pt) p
    join lateral (
        select * from ways
        where cho_xe and lien_thong
        order by geom <-> p.pt
        limit 1
    ) w on true
$$;

-- Cost from a free point to each of the given POIs (seconds when by_time, otherwise metres); null when unreachable.
-- Leaves the segment through whichever end is cheaper overall, honouring one-way segments.
create or replace function route_cost_from_point(px double precision, py double precision, poi_ids text[], by_time boolean default true)
returns table (to_id text, cost double precision)
language plpgsql stable
as $$
declare
    e record;
    off_target double precision;
    off_source double precision;
begin
    select * into e from snap_point(px, py);
    if e.edge_id is null then return; end if;
    off_target := case when (case when by_time then e.time_fwd else e.cost_fwd end) >= 0
                       then (1 - e.frac) * (case when by_time then e.time_fwd else e.cost_fwd end) end;
    off_source := case when (case when by_time then e.time_rev else e.cost_rev end) >= 0
                       then e.frac * (case when by_time then e.time_rev else e.cost_rev end) end;

    return query
    select c.to_id, min(c.total)
    from (
        select a.to_id, a.cost + off_target as total
        from route_cost_from_vertex(e.target, poi_ids, by_time) a where off_target is not null and a.cost is not null
        union all
        select b.to_id, b.cost + off_source
        from route_cost_from_vertex(e.source, poi_ids, by_time) b where off_source is not null and b.cost is not null
    ) c
    group by c.to_id
    union all
    -- Places that cannot be reached from either end are still listed, with a null cost.
    select s.poi_id, null::double precision
    from poi_snap s
    where s.poi_id = any (poi_ids)
      and not exists (select 1 from (
            select a.to_id from route_cost_from_vertex(e.target, poi_ids, by_time) a where off_target is not null and a.cost is not null
            union all
            select b.to_id from route_cost_from_vertex(e.source, poi_ids, by_time) b where off_source is not null and b.cost is not null) r
          where r.to_id = s.poi_id);
end
$$;

-- The road path from a free point to a POI, one row per piece in travel order (same shape as route_path_from_vertex).
-- Row 0 is the way from the point itself: a short connector to the road plus the part of the segment up to the end the
-- route leaves by; the rows after it are whole segments.
create or replace function route_path_from_point(px double precision, py double precision, to_poi text, by_time boolean default true)
returns table (seq integer, edge_id bigint, ten text, length_m double precision, seconds double precision, geom geometry)
language plpgsql stable
as $$
declare
    e record;
    w ways%rowtype;
    pt geometry := st_setsrid(st_makepoint(px, py), 3405);
    off_target double precision;
    off_source double precision;
    via_target double precision;
    via_source double precision;
    go_target boolean;
    on_road geometry;
    piece geometry;
    leave_vertex bigint;
    offset_cost double precision;
    fwd double precision;
    rev double precision;
begin
    select * into e from snap_point(px, py);
    if e.edge_id is null then return; end if;
    select * into w from ways where gid = e.edge_id;

    fwd := case when by_time then e.time_fwd else e.cost_fwd end;
    rev := case when by_time then e.time_rev else e.cost_rev end;
    off_target := case when fwd >= 0 then (1 - e.frac) * fwd end;
    off_source := case when rev >= 0 then e.frac * rev end;
    select a.cost + off_target into via_target from route_cost_from_vertex(e.target, array[to_poi], by_time) a
    where off_target is not null and a.cost is not null;
    select b.cost + off_source into via_source from route_cost_from_vertex(e.source, array[to_poi], by_time) b
    where off_source is not null and b.cost is not null;
    if via_target is null and via_source is null then return; end if;
    go_target := via_source is null or (via_target is not null and via_target <= via_source);

    on_road := st_lineinterpolatepoint(w.geom, e.frac);
    if go_target then
        leave_vertex := e.target;
        piece := case when e.frac >= 1 then on_road else st_linesubstring(w.geom, e.frac, 1) end;
        offset_cost := off_target;
    else
        leave_vertex := e.source;
        piece := case when e.frac <= 0 then on_road else st_reverse(st_linesubstring(w.geom, 0, e.frac)) end;
        offset_cost := off_source;
    end if;

    return query
    select 0, w.gid, w.ten,
           st_distance(pt, on_road) + st_length(piece),
           offset_cost + st_distance(pt, on_road) / 6.0,   -- the short hop from the point to the road, at about 6 m/s
           st_makeline(st_makeline(pt, on_road), piece);

    return query
    select p.seq + 1, p.edge_id, p.ten, p.length_m, p.seconds, p.geom
    from route_path_from_vertex(leave_vertex, to_poi, by_time) p;
end
$$;
