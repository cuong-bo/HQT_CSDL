-- Extensions and small pure helpers used by the later migrations.
create extension if not exists postgis;
create extension if not exists pgrouting;
create extension if not exists unaccent;
create extension if not exists pg_trgm;
create extension if not exists citext;

-- unaccent() is only STABLE; the wrapper is the usual way to use it in an index / generated column.
create or replace function f_unaccent(text) returns text
    language sql immutable parallel safe strict
as $$ select public.unaccent('public.unaccent', $1) $$;

-- Opening time of a range written as "07:00-17:30"; null for free text such as "Cả ngày" or "T3-CN: 07:30-11:30; ...".
create or replace function f_gio_mo(t text) returns time
    language sql immutable parallel safe
as $$
    select case when m is not null and m[1]::int < 24 and m[2]::int < 60 then make_time(m[1]::int, m[2]::int, 0) end
    from (select regexp_match(btrim(t), '^(\d{1,2}):(\d{2})\s*-\s*(\d{1,2}):(\d{2})$') as m) x
$$;

-- Closing time of the same range; "24:00" is read as 23:59.
create or replace function f_gio_dong(t text) returns time
    language sql immutable parallel safe
as $$
    select case
             when m is null then null
             when m[3]::int = 24 and m[4]::int = 0 then time '23:59'
             when m[3]::int < 24 and m[4]::int < 60 then make_time(m[3]::int, m[4]::int, 0)
           end
    from (select regexp_match(btrim(t), '^(\d{1,2}):(\d{2})\s*-\s*(\d{1,2}):(\d{2})$') as m) x
$$;

-- {lowest, highest} price in dong found in a text such as "từ 230.000đ/người". "Miễn phí" alone is {0,0}; when it
-- appears next to a price ("50.000đ người lớn, miễn phí trẻ em") the lowest price is 0. Null when there is no number
-- and no "miễn phí". Numbers above two billion are ignored.
create or replace function f_gia_range(t text) returns integer[]
    language sql immutable parallel safe
as $$
    select case
             when t is null then null
             when v.n_max is null then case when lower(f_unaccent(t)) like '%mien phi%' then array[0, 0] end
             when lower(f_unaccent(t)) like '%mien phi%' then array[0, v.n_max::int]
             else array[v.n_min::int, v.n_max::int]
           end
    from (select min(n) as n_min, max(n) as n_max
          from (select replace(g[1], '.', '')::bigint as n
                from regexp_matches(coalesce(t, ''), '(\d{1,3}(?:\.\d{3})+|\d+)', 'g') as g) x
          where n <= 2000000000) v
$$;
