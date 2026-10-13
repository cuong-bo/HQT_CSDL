-- Long-lived tokens that let the app get a new 15-minute access token. Only a SHA-256 hash is stored.
create table refresh_token (
    id          bigint generated always as identity primary key,
    user_id     bigint not null references app_user (id) on delete cascade,
    token_hash  bytea not null unique,
    expires_at  timestamptz not null,
    revoked_at  timestamptz,
    created_at  timestamptz not null default now()
);
create index refresh_token_user_idx on refresh_token (user_id);
