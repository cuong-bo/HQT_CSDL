-- Opening hours are free text in the source data. The original helpers only matched the ASCII hyphen, so food places written
-- with the typographic dash ("07:00–22:00") kept gio_mo/gio_dong as NULL. Accept all three dashes and fill the rows already loaded.
create or replace function f_gio_mo(t text) returns time
    language sql immutable parallel safe
as $$
    select case when m is not null and m[1]::int < 24 and m[2]::int < 60 then make_time(m[1]::int, m[2]::int, 0) end
    from (select regexp_match(btrim(t), '^(\d{1,2}):(\d{2})\s*[-–—]\s*(\d{1,2}):(\d{2})$') as m) x
$$;

create or replace function f_gio_dong(t text) returns time
    language sql immutable parallel safe
as $$
    select case
             when m is null then null
             when m[3]::int = 24 and m[4]::int = 0 then time '23:59'
             when m[3]::int < 24 and m[4]::int < 60 then make_time(m[3]::int, m[4]::int, 0)
           end
    from (select regexp_match(btrim(t), '^(\d{1,2}):(\d{2})\s*[-–—]\s*(\d{1,2}):(\d{2})$') as m) x
$$;

-- Rows imported before this migration (and any hand-edited row with the same shape) get their parsed times now.
update poi
set gio_mo = coalesce(gio_mo, f_gio_mo(gio_mo_cua)),
    gio_dong = coalesce(gio_dong, f_gio_dong(gio_mo_cua))
where gio_mo_cua is not null
  and (gio_mo is null or gio_dong is null);
