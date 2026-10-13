-- Checks after an import. Hard failures raise an exception (non-zero exit); known data problems raise warnings.
\set ON_ERROR_STOP on
\pset pager off

do $$
declare
    n integer;
    ga text;
    dn text;
    seconds_total double precision;
begin
    -- Every staged place must be in poi under the right group (the group of a staged site may be edited later by an admin).
    select count(*) into n from (
        select btrim(madl) as id from stg.diemdulich union all select btrim(maau) from stg.diemanuong where nullif(btrim(maau), '') is not null
        union all select btrim(malt) from stg.diemluutru
        union all select btrim(macsdv) from stg.diemdvdulich union all select btrim(magt) from stg.diemdmgiaothong) s
    where not exists (select 1 from poi p where p.id = s.id);
    if n > 0 then raise exception 'poi: % staged places are missing', n; end if;
    select count(*) into n from poi p join stg.diemluutru s on btrim(s.malt) = p.id where p.nhom <> 'stay';
    if n > 0 then raise exception 'poi: % stays are in another group', n; end if;
    select count(*) into n from poi where nhom = 'food';
    if n = 0 then raise notice 'poi: no food places yet (the food layer is missing from the data)'; end if;

    select count(*) into n from geometry_columns where f_table_schema = 'public' and srid <> 3405;
    if n > 0 then raise exception '% geometry columns are not in SRID 3405', n; end if;

    select count(*) into n from (select geom from ranh_tinh union all select geom from ranh_xa) b where not st_isvalid(geom);
    if n > 0 then raise exception '% boundary geometries are invalid', n; end if;
    select count(*) into n from stg.rgxa;
    if (select count(*) from ranh_xa) <> n then raise exception 'ranh_xa: % staged communes but another count loaded', n; end if;

    select count(*) into n from poi p where not exists (select 1 from ranh_tinh t where st_intersects(t.geom, p.geom));
    if n > 0 then raise warning '% POI lie outside the province boundary (data team: check the points)', n; end if;

    select count(*) into n from ranh_xa x, ranh_tinh t where not st_covers(st_expand(t.geom, 1000), x.geom);
    if n > 0 then raise warning '% commune boundaries stick out of the province (data team: ranh_xa has a bad vertex)', n; end if;
    select abs(sum(dien_tich_km2) - (select dien_tich_km2 from ranh_tinh limit 1)) into seconds_total from ranh_xa;
    if seconds_total > 5 then raise warning 'commune areas differ from the province area by % km2', round(seconds_total::numeric, 1); end if;

    select count(*) into n from ways;
    if n < 0.95 * (select count(*) from stg.manggiaothong) then raise exception 'ways: only % of % edges kept', n, (select count(*) from stg.manggiaothong); end if;
    select count(*) into n from ways where lien_thong;
    raise notice 'ways: % edges in the connected vehicle network', n;
    if n < 0.5 * (select count(*) from ways) then raise exception 'ways: connected network too small (%)', n; end if;

    select count(*) into n from poi p where p.deleted_at is null and not exists (select 1 from poi_snap s where s.poi_id = p.id);
    if n > 0 then raise exception '% POI are not snapped to the road network', n; end if;
    select count(*) into n from poi_snap where xa_duong;
    if n > 0 then raise warning '% POI are more than 150 m from any road', n; end if;

    select id into ga from poi where nhom = 'transport' and ten_khong_dau like '%ga hue%' limit 1;
    select id into dn from poi where ten_khong_dau like '%hoang thanh hue%' or ten_khong_dau like '%dai noi%' limit 1;
    if ga is null or dn is null then
        raise warning 'sample route skipped: Ga Hue or Dai Noi not found by name (ga=%, dn=%)', ga, dn;
    else
        select sum(seconds) into seconds_total from route_path(ga, dn);
        if seconds_total is null or seconds_total <= 0 then raise exception 'no route from % to %', ga, dn; end if;
        raise notice 'route Ga Hue -> Dai Noi: % s', round(seconds_total::numeric);
    end if;
end
$$;

select nhom, count(*) as so_diem from poi where deleted_at is null group by nhom order by nhom;
select round(avg(dist_m)::numeric, 1) as cach_duong_trung_binh_m, round(max(dist_m)::numeric, 1) as cach_duong_xa_nhat_m from poi_snap;
