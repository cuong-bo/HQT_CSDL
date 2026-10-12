-- Security hardening found by review.

-- A run of 20+ digits in a price text used to overflow bigint and make an admin write fail: cap each digit run at 10.
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
                from regexp_matches(coalesce(t, ''), '(\d{1,3}(?:\.\d{3})+|\d{1,10})', 'g') as g) x
          where n <= 2000000000) v
$$;

-- The API's database role may add to the audit trail but not change or erase it.
do $$
begin
    if exists (select 1 from pg_roles where rolname = 'huegis_app') then
        revoke update, delete, truncate on audit_log from huegis_app;
    end if;
end
$$;
