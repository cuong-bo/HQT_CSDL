-- Builds the routing graph from stg.manggiaothong (edges) and stg.nutmanggiaothong (nodes) and snaps the POIs to it. Rebuilds from scratch every time.
\set ON_ERROR_STOP on
begin;

-- DELETE (not TRUNCATE): TRUNCATE takes an exclusive lock and would stall every route request until the commit,
-- while readers keep seeing the old graph until this transaction commits.
delete from poi_snap;
delete from ways_vertices;
delete from ways;

-- Edges: one LineString each, no self-loops, costs in metres. A negative source cost means the edge may only be
-- driven against its drawn direction; a row that is closed both ways is bad data and is opened both ways.
insert into ways (gid, osm_id, ten, road_type, ref, source, target, length_m, cost, reverse_cost, bridge, tunnel, geom)
select id, nullif(osm_id, ''), nullif(road_name, ''), road_type, nullif(road_ref, ''), source, target,
       st_length(g), fwd, rev, bridge, tunnel, g
from (
    select s.*, st_geometryn(s.geom, 1) as g,
           case when cost < 0 and reverse_co >= 0 then -1 else st_length(st_geometryn(geom, 1)) end as fwd,
           case when reverse_co < 0 and cost >= 0 then -1 else st_length(st_geometryn(geom, 1)) end as rev
    from stg.manggiaothong s
    where source <> target
) e
where st_length(g) > 0;

-- Typical speed (km/h) when the data has no maxspeed; null = not for motor vehicles (walking, stairs, cycling).
create temporary table speed_by_type (road_type text primary key, kmh real);
insert into speed_by_type values
    ('cao_toc', 80), ('nhanh_noi_cao_toc', 50), ('duong_truc_chinh', 50), ('nhanh_noi_duong_truc_chinh', 40),
    ('duong_chinh_cap_1', 50), ('nhanh_noi_duong_chinh_cap_1', 40), ('duong_chinh_cap_2', 40), ('nhanh_noi_duong_chinh_cap_2', 35),
    ('duong_chinh_cap_3', 35), ('nhanh_noi_duong_chinh_cap_3', 30), ('duong_khu_dan_cu', 25), ('duong_noi_bo', 25),
    ('duong_chua_phan_cap', 30), ('chua_xac_dinh', 25), ('duong_dan_sinh', 25),
    ('duong_chuyen_dung', 20), ('duong_chuyen_dung_cap_1', 20), ('duong_chuyen_dung_cap_2', 20),
    ('duong_chuyen_dung_cap_3', 20), ('duong_chuyen_dung_cap_4', 20), ('duong_chuyen_dung_cap_5', 20),
    ('duong_di_bo', null), ('pho_di_bo', null), ('loi_di', null), ('bac_thang', null), ('duong_xe_dap', null);

-- A road with a maxspeed uses it, otherwise the typical speed of its type; types without one are not for vehicles.
update ways w
set speed_kmh = case when t.kmh is null then null else coalesce(nullif(m.maxspeed, 0)::real, t.kmh) end,
    cho_xe = t.kmh is not null
from speed_by_type t, stg.manggiaothong m
where t.road_type = w.road_type and m.id = w.gid;

-- Road types missing from the table above stay open to vehicles at a slow default speed.
update ways set speed_kmh = 20 where speed_kmh is null and cho_xe;

update ways
set cost_time = case when cost < 0 then -1 else cost / (speed_kmh / 3.6) end,
    reverse_cost_time = case when reverse_cost < 0 then -1 else reverse_cost / (speed_kmh / 3.6) end
where cho_xe;

-- Vertices: the node layer of the road network (NutMangGiaoThong, only for calculation, never drawn) ...
insert into ways_vertices (id, geom)
select id::bigint, geom
from stg.nutmanggiaothong
on conflict (id) do nothing;

-- ... and, for any edge end the node layer does not have, the end point of the edge itself.
insert into ways_vertices (id, geom)
select node, (array_agg(pt))[1]
from (
    select source as node, st_startpoint(geom) as pt from ways
    union all
    select target, st_endpoint(geom) from ways
) p
where not exists (select 1 from ways_vertices v where v.id = p.node)
group by node;

-- Keep only the biggest STRONGLY connected vehicle network: with one-way streets a weakly connected piece can still be
-- a trap you can enter but not leave.
create temporary table comp as
select node, component
from pgr_strongcomponents('select gid as id, source, target, cost, reverse_cost from ways where cho_xe');
update ways_vertices v set component = c.component from comp c where c.node = v.id;
update ways_vertices
set lien_thong = true
where component = (select component from comp group by component order by count(*) desc limit 1);
update ways w set lien_thong = true
from ways_vertices a, ways_vertices b
where w.cho_xe and a.id = w.source and a.lien_thong and b.id = w.target and b.lien_thong;

-- Snap every POI to the nearest road of that network, then to the nearer end of that road.
insert into poi_snap (poi_id, vertex_id, dist_m, vertex_dist_m, xa_duong)
select p.id,
       case when st_distance(p.geom, st_startpoint(e.geom)) <= st_distance(p.geom, st_endpoint(e.geom)) then e.source else e.target end,
       st_distance(p.geom, e.geom),
       least(st_distance(p.geom, st_startpoint(e.geom)), st_distance(p.geom, st_endpoint(e.geom))),
       st_distance(p.geom, e.geom) > 150
from poi p
cross join lateral (
    select source, target, geom
    from ways
    where cho_xe and lien_thong
    order by geom <-> p.geom
    limit 1
) e;

commit;
vacuum analyze ways;
vacuum analyze ways_vertices;
vacuum analyze poi_snap;
