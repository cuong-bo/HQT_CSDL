-- Fills the application tables from the staging schema (stg, loaded by run.sh). Safe to run again: POI rows are
-- upserted by id so owner links, status and version survive a re-import.
\set ON_ERROR_STOP on
begin;

-- One id must not be used by two layers (the upserts below would overwrite each other's row).
do $$
declare dup text;
begin
    select string_agg(id, ', ') into dup from (select id from (
        select btrim(madl) as id from stg.diemdulich union all select btrim(maau) from stg.diemanuong where nullif(btrim(maau), '') is not null
        union all select btrim(malt) from stg.diemluutru
        union all select btrim(macsdv) from stg.diemdvdulich union all select btrim(magt) from stg.diemdmgiaothong) x
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

-- Food and drink places.
insert into poi (id, nhom, loai_hinh, ten, dia_chi, gio_mo_cua, gio_mo, gio_dong, gia_text, gia_thap, gia_cao, sdt, nguon, geom)
select btrim(maau), 'food', nullif(btrim(loaiau), ''), btrim(tenau), nullif(btrim(diachi), ''),
       nullif(btrim(giomocua), ''), f_gio_mo(giomocua), f_gio_dong(giomocua),
       nullif(btrim(gia), ''), (f_gia_range(gia))[1], (f_gia_range(gia))[2],
       nullif(btrim(sdthotro), ''), nullif(btrim(nguon), ''), geom
from stg.diemanuong
where nullif(btrim(maau), '') is not null and geom is not null   -- the layer has a blank record
on conflict (id) do update set loai_hinh = excluded.loai_hinh, ten = excluded.ten, dia_chi = excluded.dia_chi,
    gio_mo_cua = excluded.gio_mo_cua, gio_mo = excluded.gio_mo, gio_dong = excluded.gio_dong,
    gia_text = excluded.gia_text, gia_thap = excluded.gia_thap, gia_cao = excluded.gia_cao, sdt = excluded.sdt,
    nguon = excluded.nguon, geom = excluded.geom, deleted_at = null
where poi.updated_by is null;

-- Stays. The price range is in two numeric columns (0 = unknown); the text is what people read.
insert into poi (id, nhom, loai_hinh, ten, dia_chi, gio_mo_cua, gia_text, gia_thap, gia_cao, sdt, website, ghi_chu, geom)
select btrim(malt), 'stay', nullif(btrim(loaics), ''), btrim(tencs), nullif(btrim(diachi), ''),
       case when coalesce(btrim(gionhan), '') = '' and coalesce(btrim(giotra), '') = '' then null
            else 'Nhận phòng ' || coalesce(nullif(btrim(gionhan), ''), '?') || ' - Trả phòng ' || coalesce(nullif(btrim(giotra), ''), '?') end,
       nullif(btrim(giaphong), ''),
       nullif(nullif(regexp_replace(giatu::text, '[^0-9]', '', 'g'), '')::bigint, 0)::int,
       nullif(nullif(regexp_replace(giaden::text, '[^0-9]', '', 'g'), '')::bigint, 0)::int,
       nullif(btrim(sdt), ''), nullif(btrim(website), ''), nullif(btrim(luuy), ''), geom
from stg.diemluutru
on conflict (id) do update set loai_hinh = excluded.loai_hinh, ten = excluded.ten, dia_chi = excluded.dia_chi,
    gio_mo_cua = excluded.gio_mo_cua, gia_text = excluded.gia_text, gia_thap = excluded.gia_thap, gia_cao = excluded.gia_cao,
    sdt = excluded.sdt, website = excluded.website, ghi_chu = excluded.ghi_chu, geom = excluded.geom, deleted_at = null
where poi.updated_by is null;

-- Tourism services: the opening hours are in two columns (open, close), often "Đang cập nhật" (not known yet).
insert into poi (id, nhom, loai_hinh, ten, dia_chi, gio_mo_cua, gio_mo, gio_dong, gia_text, sdt, website, mo_ta, ghi_chu, geom)
select btrim(macsdv), 'service', nullif(btrim(loaidv), ''), btrim(tendv), nullif(btrim(diachi), ''),
       case when btrim(giomo) ~ '^[0-9]{1,2}:[0-9]{2}$' and btrim(giodong) ~ '^[0-9]{1,2}:[0-9]{2}$' then btrim(giomo) || ' - ' || btrim(giodong) end,
       case when btrim(giomo) ~ '^[0-9]{1,2}:[0-9]{2}$' then btrim(giomo)::time end,
       case when btrim(giodong) ~ '^[0-9]{1,2}:[0-9]{2}$' then btrim(giodong)::time end,
       nullif(btrim(giadv), ''), nullif(btrim(sdt), ''), nullif(btrim(website), ''), nullif(btrim(dichvu), ''), nullif(btrim(luuy), ''), geom
from stg.diemdvdulich
on conflict (id) do update set loai_hinh = excluded.loai_hinh, ten = excluded.ten, dia_chi = excluded.dia_chi,
    gio_mo_cua = excluded.gio_mo_cua, gio_mo = excluded.gio_mo, gio_dong = excluded.gio_dong, gia_text = excluded.gia_text,
    sdt = excluded.sdt, website = excluded.website, mo_ta = excluded.mo_ta, ghi_chu = excluded.ghi_chu, geom = excluded.geom, deleted_at = null
where poi.updated_by is null;

-- Transport hubs: the timetable text is shown as the opening hours, the route as the description.
insert into poi (id, nhom, loai_hinh, ten, dia_chi, gio_mo_cua, gia_text, sdt, website, mo_ta, ghi_chu, geom)
select btrim(magt), 'transport', nullif(btrim(loaigt), ''), btrim(tengt), nullif(btrim(diachi), ''),
       nullif(btrim(lichchay), ''), nullif(btrim(giave), ''), nullif(btrim(sdt), ''), nullif(btrim(website), ''),
       nullif(btrim(tuyen), ''), nullif(btrim(luuy), ''), geom
from stg.diemdmgiaothong
on conflict (id) do update set loai_hinh = excluded.loai_hinh, ten = excluded.ten, dia_chi = excluded.dia_chi,
    gio_mo_cua = excluded.gio_mo_cua, gia_text = excluded.gia_text, sdt = excluded.sdt, website = excluded.website,
    mo_ta = excluded.mo_ta, ghi_chu = excluded.ghi_chu, geom = excluded.geom, deleted_at = null
where poi.updated_by is null;

-- POI that are no longer in the data are soft-deleted (never hard-deleted: owner links hang on them).
update poi set deleted_at = now()
where deleted_at is null and updated_by is null and id not in (
    select btrim(madl) from stg.diemdulich union all select btrim(maau) from stg.diemanuong where nullif(btrim(maau), '') is not null
    union all select btrim(malt) from stg.diemluutru
    union all select btrim(macsdv) from stg.diemdvdulich union all select btrim(magt) from stg.diemdmgiaothong);

-- Administrative points.
insert into hanh_chinh_diem (id, ten, loai, dia_chi, gio_mo_cua, gia_ve, sdt, nguon, geom)
select btrim(mahc), btrim(tenhc), nullif(btrim(loaihc), ''), nullif(btrim(diachi), ''), nullif(btrim(giomocua), ''),
       nullif(btrim(giavephi), ''), nullif(btrim(sdthotro), ''), nullif(btrim(nguon), ''), geom
from stg.diemhanhchinh
on conflict (id) do update set ten = excluded.ten, loai = excluded.loai, dia_chi = excluded.dia_chi,
    gio_mo_cua = excluded.gio_mo_cua, gia_ve = excluded.gia_ve, sdt = excluded.sdt, nguon = excluded.nguon, geom = excluded.geom;

-- Boundaries: made valid, always MultiPolygon. The province file covers the whole country in degrees (WGS84): only this
-- province is kept, moved to the project's coordinate system. Numbers may be text in the source, so strip anything that
-- is not a digit.
insert into ranh_tinh (ma_tinh, ten_tinh, dien_tich_km2, dan_so, geom)
select btrim(matinh::text), btrim(tentinh), nullif(regexp_replace(dientich::text, '[^0-9.]', '', 'g'), '')::numeric,
       nullif(regexp_replace(danso::text, '[^0-9]', '', 'g'), '')::bigint::int,
       st_multi(st_collectionextract(st_makevalid(st_transform(geom, 3405)), 3))
from stg.diaphan_tinh_2025
where btrim(tentinh) = 'Thành phố Huế'
on conflict (ma_tinh) do update set ten_tinh = excluded.ten_tinh, dien_tich_km2 = excluded.dien_tich_km2,
    dan_so = excluded.dan_so, geom = excluded.geom;

-- Communes after the 2025 merger; "sap_nhap" lists the former communes each one was formed from.
insert into ranh_xa (ma_xa, ten_xa, ma_tinh, dien_tich_km2, dan_so, ghi_chu, geom)
select btrim(ma_xa::text), btrim(ten_xa), (select ma_tinh from ranh_tinh order by ma_tinh limit 1),
       nullif(regexp_replace(dtich_km2::text, '[^0-9.]', '', 'g'), '')::numeric,
       nullif(regexp_replace(dan_so::text, '[^0-9]', '', 'g'), '')::bigint::int,
       nullif('Sáp nhập từ: ' || btrim(coalesce(sap_nhap, '')), 'Sáp nhập từ: '),
       st_multi(st_collectionextract(st_makevalid(geom), 3))
from stg.rgxa
on conflict (ma_xa) do update set ten_xa = excluded.ten_xa, ma_tinh = excluded.ma_tinh, dien_tich_km2 = excluded.dien_tich_km2,
    dan_so = excluded.dan_so, ghi_chu = excluded.ghi_chu, geom = excluded.geom;

commit;
