-- Tourism data in VN-2000 / UTM zone 48N (EPSG:3405), metres.

-- One merged table for tourist sites, stays, services and transport hubs.
create table poi (
    id             text primary key,
    nhom           text not null check (nhom in ('heritage', 'scenery', 'food', 'stay', 'service', 'transport')),
    loai_hinh      text,
    ten            text not null,
    ten_khong_dau  text generated always as (lower(f_unaccent(ten))) stored,
    dia_chi        text,
    gio_mo_cua     text,                 -- the original text, shown to people
    gio_mo         time,                 -- parsed when the text is a plain range
    gio_dong       time,
    gia_text       text,                 -- the original text
    gia_thap       integer check (gia_thap >= 0),
    gia_cao        integer check (gia_cao >= 0),
    sdt            text,
    website        text,
    mo_ta          text,
    nguon          text,
    ghi_chu        text,
    status         text not null default 'visible' check (status in ('visible', 'hidden', 'closed')),
    version        integer not null default 1,
    updated_by     bigint references app_user (id) on delete set null,
    updated_at     timestamptz not null default now(),
    deleted_at     timestamptz,
    geom           geometry(Point, 3405) not null
);
create index poi_geom_idx on poi using gist (geom);
create index poi_nhom_idx on poi (nhom) where deleted_at is null;
create index poi_ten_trgm_idx on poi using gin (ten_khong_dau gin_trgm_ops);

create table hanh_chinh_diem (
    id          text primary key,
    ten         text not null,
    loai        text,
    dia_chi     text,
    gio_mo_cua  text,
    gia_ve      text,
    sdt         text,
    nguon       text,
    geom        geometry(Point, 3405) not null
);
create index hanh_chinh_diem_geom_idx on hanh_chinh_diem using gist (geom);

create table ranh_tinh (
    ma_tinh        text primary key,
    ten_tinh       text not null,
    dien_tich_km2  numeric,
    dan_so         integer,
    geom           geometry(MultiPolygon, 3405) not null
);
create index ranh_tinh_geom_idx on ranh_tinh using gist (geom);

create table ranh_xa (
    ma_xa          text primary key,
    ten_xa         text not null,
    ma_tinh        text,
    dien_tich_km2  numeric,
    dan_so         integer,
    ghi_chu        text,
    geom           geometry(MultiPolygon, 3405) not null
);
create index ranh_xa_geom_idx on ranh_xa using gist (geom);

-- Which owner manages which place (an admin approves the claim).
create table place_owner (
    user_id      bigint not null references app_user (id) on delete cascade,
    poi_id       text not null references poi (id) on delete cascade,
    status       text not null default 'pending' check (status in ('pending', 'approved', 'rejected')),
    reviewed_by  bigint references app_user (id) on delete set null,
    reviewed_at  timestamptz,
    created_at   timestamptz not null default now(),
    primary key (user_id, poi_id)
);
create index place_owner_poi_idx on place_owner (poi_id);
