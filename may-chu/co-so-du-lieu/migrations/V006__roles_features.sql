-- Features for the three roles: user (favourites, itineraries, reviews, reports), owner (claims, edit requests,
-- announcements) and admin (everything is audited). No photo uploads in this version.

create table favorite (
    user_id     bigint not null references app_user (id) on delete cascade,
    poi_id      text not null references poi (id) on delete cascade,
    created_at  timestamptz not null default now(),
    primary key (user_id, poi_id)
);

create table itinerary (
    id          bigint generated always as identity primary key,
    user_id     bigint not null references app_user (id) on delete cascade,
    name        text not null check (length(name) between 1 and 80),
    created_at  timestamptz not null default now(),
    updated_at  timestamptz not null default now()
);
create index itinerary_user_idx on itinerary (user_id);

create table itinerary_item (
    itinerary_id  bigint not null references itinerary (id) on delete cascade,
    position      integer not null,
    poi_id        text not null references poi (id) on delete cascade,
    primary key (itinerary_id, position)
);

create table review (
    id           bigint generated always as identity primary key,
    user_id      bigint not null references app_user (id) on delete cascade,
    poi_id       text not null references poi (id) on delete cascade,
    rating       smallint not null check (rating between 1 and 5),
    comment      text check (length(comment) <= 1000),
    status       text not null default 'visible' check (status in ('visible', 'hidden')),
    owner_reply  text check (length(owner_reply) <= 1000),
    created_at   timestamptz not null default now(),
    unique (user_id, poi_id)
);
create index review_poi_idx on review (poi_id) where status = 'visible';

create table report (
    id          bigint generated always as identity primary key,
    user_id     bigint not null references app_user (id) on delete cascade,
    poi_id      text not null references poi (id) on delete cascade,
    kind        text not null check (kind in ('wrong_info', 'closed', 'wrong_location', 'other')),
    content     text check (length(content) <= 1000),
    status      text not null default 'open' check (status in ('open', 'resolved')),
    handled_by  bigint references app_user (id) on delete set null,
    created_at  timestamptz not null default now()
);

create table poi_edit_request (
    id           bigint generated always as identity primary key,
    poi_id       text not null references poi (id) on delete cascade,
    user_id      bigint not null references app_user (id) on delete cascade,
    changes      jsonb not null,           -- {"dia_chi": "...", "sdt": "..."}: only the columns an owner may edit
    status       text not null default 'pending' check (status in ('pending', 'approved', 'rejected')),
    note         text,
    reviewed_by  bigint references app_user (id) on delete set null,
    created_at   timestamptz not null default now(),
    reviewed_at  timestamptz
);
create index poi_edit_request_status_idx on poi_edit_request (status, created_at);

create table announcement (
    id          bigint generated always as identity primary key,
    poi_id      text not null references poi (id) on delete cascade,
    title       text not null check (length(title) between 1 and 120),
    body        text not null check (length(body) <= 2000),
    starts_at   timestamptz not null default now(),
    ends_at     timestamptz,
    created_by  bigint references app_user (id) on delete set null,
    created_at  timestamptz not null default now()
);
create index announcement_poi_idx on announcement (poi_id, starts_at desc);

create table poi_stats_daily (
    poi_id      text not null references poi (id) on delete cascade,
    day         date not null default current_date,
    views       integer not null default 0,
    route_adds  integer not null default 0,
    primary key (poi_id, day)
);

-- Snaps one POI (new, or whose position changed) to the nearest road of the connected vehicle network.
create or replace function snap_poi(p_id text) returns void
language sql
as $$
    insert into poi_snap (poi_id, vertex_id, dist_m, vertex_dist_m, xa_duong)
    select p.id,
           case when st_distance(p.geom, st_startpoint(e.geom)) <= st_distance(p.geom, st_endpoint(e.geom)) then e.source else e.target end,
           st_distance(p.geom, e.geom),
           least(st_distance(p.geom, st_startpoint(e.geom)), st_distance(p.geom, st_endpoint(e.geom))),
           st_distance(p.geom, e.geom) > 150
    from poi p
    cross join lateral (
        select source, target, geom from ways where cho_xe and lien_thong order by geom <-> p.geom limit 1
    ) e
    where p.id = p_id
    on conflict (poi_id) do update set vertex_id = excluded.vertex_id, dist_m = excluded.dist_m,
        vertex_dist_m = excluded.vertex_dist_m, xa_duong = excluded.xa_duong
$$;
