-- Accounts (user / owner / admin) and the audit trail. No e-mail, no OTP: the admin resets passwords by hand.
create table app_user (
    id             bigint generated always as identity primary key,
    username       citext not null unique check (length(username) between 3 and 40),
    password_hash  text not null,
    display_name   text not null,
    role           text not null default 'user' check (role in ('user', 'owner', 'admin')),
    status         text not null default 'active' check (status in ('active', 'locked', 'must_change_password')),
    failed_logins  integer not null default 0,
    locked_until   timestamptz,
    created_at     timestamptz not null default now(),
    last_login_at  timestamptz
);

create table audit_log (
    id          bigint generated always as identity primary key,
    at          timestamptz not null default now(),
    user_id     bigint references app_user (id) on delete set null,
    action      text not null,
    entity      text not null,
    entity_id   text,
    old_value   jsonb,
    new_value   jsonb
);
create index audit_log_entity_idx on audit_log (entity, entity_id, at desc);
create index audit_log_user_idx on audit_log (user_id, at desc);
