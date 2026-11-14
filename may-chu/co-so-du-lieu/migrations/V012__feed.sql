-- Bản tin cộng đồng "Khoảnh khắc": bài đăng, ảnh, thích, bình luận, báo cáo.
alter table app_user add column feed_terms_at timestamptz;   -- lúc người dùng đồng ý quy tắc cộng đồng

create table post (
    id            bigint generated always as identity primary key,
    author_id     bigint not null references app_user (id) on delete cascade,
    body          text not null default '' check (length(body) <= 2000),
    poi_id        text references poi (id) on delete set null,
    shared_route  jsonb,                               -- [{"poiId":"DL021","name":"Đại Nội"}, ...] chụp lại lúc đăng; không khóa ngoại
    status        text not null default 'visible' check (status in ('pending', 'visible', 'hidden', 'removed')),
    like_count    integer not null default 0,
    comment_count integer not null default 0,
    approved_by   bigint references app_user (id) on delete set null,   -- quản trị viên đã duyệt (để tính "người đã tin cậy")
    created_at    timestamptz not null default now(),
    edited_at     timestamptz
);
create index post_feed_idx    on post (created_at desc, id desc) where status = 'visible';
create index post_poi_idx     on post (poi_id, created_at desc, id desc) where status = 'visible';
create index post_author_idx  on post (author_id, created_at desc);
create index post_pending_idx on post (created_at) where status = 'pending';

create table post_media (
    id           bigint generated always as identity primary key,
    post_id      bigint references post (id) on delete cascade,   -- null: đã tải lên nhưng chưa gắn vào bài
    uploaded_by  bigint not null references app_user (id) on delete cascade,
    position     smallint not null default 0,
    data         bytea not null check (octet_length(data)  between 1 and 3000000),   -- ảnh đầy đủ (<= 1600 px)
    thumb        bytea not null check (octet_length(thumb) between 1 and 250000),    -- ảnh thu nhỏ (<= 640 px)
    width        integer not null,
    height       integer not null,
    created_at   timestamptz not null default now()
);
create index post_media_post_idx   on post_media (post_id, position);
create index post_media_orphan_idx on post_media (created_at) where post_id is null;

create table post_like (
    post_id     bigint not null references post (id) on delete cascade,
    user_id     bigint not null references app_user (id) on delete cascade,
    created_at  timestamptz not null default now(),
    primary key (post_id, user_id)
);
create index post_like_user_idx on post_like (user_id, created_at desc);

create table post_comment (
    id          bigint generated always as identity primary key,
    post_id     bigint not null references post (id) on delete cascade,
    author_id   bigint not null references app_user (id) on delete cascade,
    body        text not null check (length(body) between 1 and 1000),
    status      text not null default 'visible' check (status in ('visible', 'hidden')),
    created_at  timestamptz not null default now()
);
create index post_comment_post_idx on post_comment (post_id, id) where status = 'visible';

create table feed_report (
    id          bigint generated always as identity primary key,
    user_id     bigint not null references app_user (id) on delete cascade,
    post_id     bigint references post (id) on delete cascade,
    comment_id  bigint references post_comment (id) on delete cascade,
    kind        text not null check (kind in ('spam', 'inappropriate', 'wrong_info', 'privacy', 'other')),
    content     text check (length(content) <= 1000),
    status      text not null default 'open' check (status in ('open', 'resolved')),
    handled_by  bigint references app_user (id) on delete set null,
    created_at  timestamptz not null default now(),
    check ((post_id is not null) <> (comment_id is not null)),   -- báo cáo đúng một thứ: bài hoặc bình luận
    unique (user_id, post_id),
    unique (user_id, comment_id)                                  -- mỗi người báo cáo một thứ một lần
);
create index feed_report_open_idx on feed_report (created_at) where status = 'open';

-- Đếm thích/bình luận bằng trigger: hai người bấm cùng lúc không làm sai số.
create function post_like_count() returns trigger language plpgsql as $$
begin
    if tg_op = 'INSERT' then update post set like_count = like_count + 1 where id = new.post_id;
    else update post set like_count = greatest(like_count - 1, 0) where id = old.post_id; end if;
    return null;
end $$;
create trigger post_like_count_t after insert or delete on post_like for each row execute function post_like_count();

create function post_comment_count() returns trigger language plpgsql as $$
begin
    if tg_op = 'INSERT' and new.status = 'visible' then update post set comment_count = comment_count + 1 where id = new.post_id;
    elsif tg_op = 'DELETE' and old.status = 'visible' then update post set comment_count = greatest(comment_count - 1, 0) where id = old.post_id;
    elsif tg_op = 'UPDATE' and old.status <> new.status then
        update post set comment_count = greatest(comment_count + case when new.status = 'visible' then 1 else -1 end, 0) where id = new.post_id;
    end if;
    return null;
end $$;
create trigger post_comment_count_t after insert or update or delete on post_comment for each row execute function post_comment_count();
