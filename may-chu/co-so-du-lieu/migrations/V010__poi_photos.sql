-- Photos of places, kept in the database (so they are in every backup and need no extra volume).
-- An owner or admin uploads a photo; it is shown to everyone only after an admin approves it.
-- The API re-encodes every upload to a JPEG of at most 1600 px, so a row is a few hundred kilobytes.
create table poi_photo (
    id           bigint generated always as identity primary key,
    poi_id       text not null references poi (id) on delete cascade,
    data         bytea not null check (octet_length(data) between 1 and 3000000),
    width        integer not null,
    height       integer not null,
    status       text not null default 'pending' check (status in ('pending', 'approved', 'rejected')),
    position     smallint not null default 0,
    note         text,
    uploaded_by  bigint references app_user (id) on delete set null,
    reviewed_by  bigint references app_user (id) on delete set null,
    reviewed_at  timestamptz,
    created_at   timestamptz not null default now()
);
create index poi_photo_poi_idx on poi_photo (poi_id, status, position);
create index poi_photo_pending_idx on poi_photo (created_at) where status = 'pending';
