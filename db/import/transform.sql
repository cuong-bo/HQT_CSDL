-- Fills the application tables from the staging schema (stg, loaded by run.sh). Safe to run again: POI rows are
-- upserted by id so owner links, status and version survive a re-import.
\set ON_ERROR_STOP on
begin;

-- One id must not be used by two layers (the upserts below would overwrite each other's row).
do $$
declare dup text;
begin
    select string_agg(id, ', ') into dup from (select id from (
        select btrim(madl) as id from stg.diemdulich union all select btrim(malt) from stg.diemluutru
        union all select btrim(madv) from stg.diemdvdulich union all select btrim(magt) from stg.diemdmgiaothong) x
    group by id having count(*) > 1) d;
    if dup is not null then raise exception 'ids used by more than one layer: %', dup; end if;
end
$$;

-- Tourist sites. The group is decided by the first word of the free-text type; keep in sync with
-- Services/Data/TourismGrouping.cs until the data team adds a proper "nhom" column.
insert into poi (id, nhom, loai_hinh, ten, dia_chi, gio_mo_cua, gio_mo, gio_dong, gia_text, gia_thap, gia_cao, sdt, website, nguon, geom)
select btrim(madl), case when split_part(loaidl, ' - ', 1) in ('Biển', 'Cảnh quan', 'Sinh thái', 'Nghỉ dưỡng', 'Đầm phá', 'Phố đi bộ')
                         then 'scenery' else 'heritage' end,
       nullif(btrim(loaidl), ''), btrim(tendl), nullif(btrim(diachi), ''),
       nullif(btrim(giomocua), ''), f_gio_mo(giomocua), f_gio_dong(giomocua),
       nullif(btrim(giavephi), ''), (f_gia_range(giavephi))[1], (f_gia_range(giavephi))[2],
       nullif(btrim(sdthotro), ''), null, nullif(btrim(nguon), ''), geom
from stg.diemdulich
on conflict (id) do update set nhom = excluded.nhom, loai_hinh = excluded.loai_hinh, ten = excluded.ten,
    dia_chi = excluded.dia_chi, gio_mo_cua = excluded.gio_mo_cua, gio_mo = excluded.gio_mo, gio_dong = excluded.gio_dong,
    gia_text = excluded.gia_text, gia_thap = excluded.gia_thap, gia_cao = excluded.gia_cao, sdt = excluded.sdt,
    nguon = excluded.nguon, geom = excluded.geom, deleted_at = null
where poi.updated_by is null;

-- Stays.
insert into poi (id, nhom, loai_hinh, ten, dia_chi, gio_mo_cua, gia_text, gia_thap, gia_cao, sdt, website, mo_ta, ghi_chu, geom)
select btrim(malt), 'stay', nullif(btrim(loaics), ''), btrim(tencs), nullif(btrim(diachi), ''),
       case when coalesce(btrim(gionhan), '') = '' and coalesce(btrim(giotra), '') = '' then null
            else 'Nhận phòng ' || coalesce(nullif(btrim(gionhan), ''), '?') || ' - Trả phòng ' || coalesce(nullif(btrim(giotra), ''), '?') end,
       nullif(btrim(giatk), ''), nullif(giavnd, 0)::int, nullif(giavnd, 0)::int,
       nullif(btrim(sdt), ''), nullif(btrim(website), ''), nullif(btrim(mota), ''), nullif(btrim(ghichu), ''), geom
from stg.diemluutru
on conflict (id) do update set loai_hinh = excluded.loai_hinh, ten = excluded.ten, dia_chi = excluded.dia_chi,
    gio_mo_cua = excluded.gio_mo_cua, gia_text = excluded.gia_text, gia_thap = excluded.gia_thap, gia_cao = excluded.gia_cao,
    sdt = excluded.sdt, website = excluded.website, mo_ta = excluded.mo_ta, ghi_chu = excluded.ghi_chu, geom = excluded.geom, deleted_at = null
where poi.updated_by is null;

-- Tourism services.
insert into poi (id, nhom, loai_hinh, ten, dia_chi, gio_mo_cua, gio_mo, gio_dong, gia_text, sdt, website, mo_ta, ghi_chu, geom)
select btrim(madv), 'service', nullif(btrim(loaidv), ''), btrim(tendv), nullif(btrim(diachi), ''),
       nullif(btrim(giohd), ''), f_gio_mo(giohd), f_gio_dong(giohd), nullif(btrim(giadv), ''),
       nullif(btrim(sdt), ''), nullif(btrim(website), ''), nullif(btrim(dichvu), ''), nullif(btrim(ghichu), ''), geom
from stg.diemdvdulich
on conflict (id) do update set loai_hinh = excluded.loai_hinh, ten = excluded.ten, dia_chi = excluded.dia_chi,
    gio_mo_cua = excluded.gio_mo_cua, gio_mo = excluded.gio_mo, gio_dong = excluded.gio_dong, gia_text = excluded.gia_text,
    sdt = excluded.sdt, website = excluded.website, mo_ta = excluded.mo_ta, ghi_chu = excluded.ghi_chu, geom = excluded.geom, deleted_at = null
where poi.updated_by is null;

-- Transport hubs.
insert into poi (id, nhom, loai_hinh, ten, dia_chi, gio_mo_cua, gio_mo, gio_dong, sdt, mo_ta, ghi_chu, geom)
select btrim(magt), 'transport', nullif(btrim(loaigt), ''), btrim(tengt), nullif(btrim(diachi), ''),
       nullif(btrim(giohd), ''), f_gio_mo(giohd), f_gio_dong(giohd), nullif(btrim(sdt), ''),
       nullif(btrim(donviql), ''), nullif(btrim(ghichu), ''), geom
from stg.diemdmgiaothong
on conflict (id) do update set loai_hinh = excluded.loai_hinh, ten = excluded.ten, dia_chi = excluded.dia_chi,
    gio_mo_cua = excluded.gio_mo_cua, gio_mo = excluded.gio_mo, gio_dong = excluded.gio_dong, sdt = excluded.sdt,
    mo_ta = excluded.mo_ta, ghi_chu = excluded.ghi_chu, geom = excluded.geom, deleted_at = null
where poi.updated_by is null;

-- POI that are no longer in the data are soft-deleted (never hard-deleted: owner links hang on them).
update poi set deleted_at = now()
where deleted_at is null and updated_by is null and id not in (
    select btrim(madl) from stg.diemdulich union all select btrim(malt) from stg.diemluutru
    union all select btrim(madv) from stg.diemdvdulich union all select btrim(magt) from stg.diemdmgiaothong);

-- Administrative points.
insert into hanh_chinh_diem (id, ten, loai, dia_chi, gio_mo_cua, gia_ve, sdt, nguon, geom)
select btrim(mahc), btrim(tenhc), nullif(btrim(loaihc), ''), nullif(btrim(diachi), ''), nullif(btrim(giomocua), ''),
       nullif(btrim(giavephi), ''), nullif(btrim(sdthotro), ''), nullif(btrim(nguon), ''), geom
from stg.diemhanhchinh
on conflict (id) do update set ten = excluded.ten, loai = excluded.loai, dia_chi = excluded.dia_chi,
    gio_mo_cua = excluded.gio_mo_cua, gia_ve = excluded.gia_ve, sdt = excluded.sdt, nguon = excluded.nguon, geom = excluded.geom;

-- Boundaries: made valid, always MultiPolygon; numbers are text in the source, so strip anything that is not a digit.
insert into ranh_tinh (ma_tinh, ten_tinh, dien_tich_km2, dan_so, geom)
select btrim(matinh), btrim(tentinh), nullif(regexp_replace(dientich, '[^0-9.]', '', 'g'), '')::numeric,
       nullif(regexp_replace(danso, '[^0-9]', '', 'g'), '')::int,
       st_multi(st_collectionextract(st_makevalid(geom), 3))
from stg.rgtinh
on conflict (ma_tinh) do update set ten_tinh = excluded.ten_tinh, dien_tich_km2 = excluded.dien_tich_km2,
    dan_so = excluded.dan_so, geom = excluded.geom;

insert into ranh_xa (ma_xa, ten_xa, ma_tinh, dien_tich_km2, dan_so, ghi_chu, geom)
select btrim(maxa), btrim(tenxa), nullif(btrim(matinh), ''), nullif(regexp_replace(dientich, '[^0-9.]', '', 'g'), '')::numeric,
       nullif(regexp_replace(danso, '[^0-9]', '', 'g'), '')::int, nullif(btrim(ghichu), ''),
       st_multi(st_collectionextract(st_makevalid(geom), 3))
from stg.rgxa
on conflict (ma_xa) do update set ten_xa = excluded.ten_xa, ma_tinh = excluded.ma_tinh, dien_tich_km2 = excluded.dien_tich_km2,
    dan_so = excluded.dan_so, ghi_chu = excluded.ghi_chu, geom = excluded.geom;

commit;
