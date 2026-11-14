# Kế hoạch Feeds: bản tin cộng đồng chia sẻ khoảnh khắc du lịch Huế

Tài liệu để bạn tự code. Đường dẫn tính từ gốc repo. Phần nào tôi chưa đọc mã thì ghi "đọc trước khi sửa". Mã SQL và C# trong tài liệu là **khung minh họa, chưa chạy thử** (khác với `huong-dan-trien-khai-giao-dien.md`, nơi mã mẫu đã biên dịch).

---

## 1. Tên, mục tiêu, phạm vi

| | |
|---|---|
| Tên kỹ thuật | `Feeds` (thư mục `Feeds/`, tiền tố API `/v1/feed`, `/v1/posts`) |
| Tên chức năng (đã chốt) | **"Khoảnh khắc"**: nhãn tab, tiêu đề panel, thông báo, tài liệu. Khớp câu "Hành trình lưu giữ những khoảnh khắc đẹp" ở màn đăng nhập |
| Lưu ý chiều rộng nhãn | Thanh dọc `NavRail` chỉ rộng 64-80 px và `OnPaint` vẽ nhãn **một dòng** (`NavRail.cs:169-171`, font 11). "Khoảnh khắc" dài 11 ký tự nên **sẽ tràn**: xem Mục 6.9 để chọn cách xử lý |
| Người dùng chính | Du khách và người dân Huế muốn chia sẻ trải nghiệm tại một địa điểm |
| Khác biệt so với mạng xã hội chung | Bài **gắn với địa điểm trên bản đồ** và có thể **đính kèm lộ trình** để người khác dùng lại |

**Giả định tôi đã dùng** vì bạn chưa trả lời hai câu hỏi trước: (1) hiện là **đồ án/demo**, nhóm người dùng nhỏ; (2) **chưa có tên miền và HTTPS**. Kế hoạch được thiết kế để chạy được trong điều kiện đó, và có **công tắc `Feed:Enabled`** cùng **danh sách điều kiện trước khi mở công khai** (Mục 10). Nếu thực tế khác, các mục bị ảnh hưởng ghi rõ.

### MVP (làm)
Đăng bài (chữ, tối đa 4 ảnh, gắn một địa điểm, tùy chọn đính kèm lộ trình); xem bảng tin mới nhất trước, lọc theo địa điểm; thích; bình luận một cấp; xóa bài/bình luận của mình; báo cáo bài và bình luận; quản trị viên duyệt bài của người mới, xử lý báo cáo; khách chỉ xem.

### Để sau (không làm trong MVP)
Video, theo dõi người dùng, thông báo đẩy, hashtag, bình luận lồng nhau, xếp hạng bài thịnh hành, ảnh đại diện, chặn người dùng, nhắn tin riêng, lớp "Khoảnh khắc" trên bản đồ (có ghi ở Giai đoạn 4).

---

## 2. Những gì mã hiện tại cho phép tái dùng (đã đọc)

| Có sẵn | Ở đâu | Dùng cho Feeds |
|---|---|---|
| Hàm `Reencode` công khai: giải mã JPEG/PNG, xoay đúng chiều, thu nhỏ ≤ 1600 px, nén JPEG 82, **bỏ hết EXIF** (kể cả GPS), ≥ 200×200, ≤ 40 triệu điểm ảnh, ≤ 3 MB | `PhotoEndpoints.cs:146-187` (SkiaSharp) | Xử lý mọi ảnh bài đăng. **Vấn đề riêng tư EXIF đã được giải quyết sẵn** |
| Bộ hàm truy vấn `ExecAsync/ScalarAsync/ListAsync/ExistsAsync/Text` | `Data/Sql.cs` | Viết endpoint theo đúng phong cách hiện có |
| `db.RecordAsync(userId, action, entity, entityId, old, new, ct)` | `Audit/AuditLog.cs` | Ghi nhật ký mọi thao tác kiểm duyệt |
| `user.UserId()`, nhóm endpoint `RequireAuthorization()` / `("admin")` | `SocialEndpoints.cs:36`, `AdminEndpoints.cs:44` | Phân quyền |
| Trạng thái `visible/hidden` của đánh giá, bảng `report`, hàng đợi duyệt ảnh `pending/approved/rejected` | `V006`, `V010` | Mô hình kiểm duyệt (làm theo, **không dùng lại bảng**, xem 3.3) |
| Cách phục vụ ảnh: JSON ký, trường `data` là base64 | `PhotoEndpoints.cs:74` | Ảnh bài đăng (giữ cùng cơ chế để máy khách ít phải sửa) |
| Ngoại lệ kích thước thân yêu cầu | `Program.cs:115-123` + `PhotoEndpoints.IsUploadRequest` | Thêm một ngoại lệ tương tự cho tải ảnh bài đăng |
| Hộp chọn địa điểm `Dialogs.PickPlaces(...)` | `Account/Dialogs.cs` (dùng ở `LoginForm.cs:280`) | Chọn địa điểm khi soạn bài |
| Lộ trình đã lưu của người dùng (`itinerary`, `itinerary_item`) | `V006`, `SocialEndpoints.cs:42-45` | Nguồn để đính kèm lộ trình |
| `FloatingPanelForm.AttachToSide`, `PressFeedback`, `PoiThumbs` (nếu đã làm hướng dẫn giao diện), `ReviewsDialog` | máy khách | Khung giao diện |

**Điều kiện làm khó** (cũng đã đọc):
- **Tài khoản chỉ có tên đăng nhập + mật khẩu**, không email, không OTP (`V002`), và ai cũng tự đăng ký được. Spam bằng tài khoản mới rẻ → cần phân cấp tin cậy (Mục 5).
- **Giới hạn tốc độ theo IP**, chưa có theo tài khoản (`Program.cs:73-85`).
- **Ảnh địa điểm nằm trong PostgreSQL (`bytea`)** có chủ đích: "in every backup and need no extra volume" (`V010`). Mục 3.2 quyết định có theo cách đó cho Feeds không.
- **`ApiClient` có timeout cứng 15 giây** và đọc cả thân vào bộ nhớ; tải ảnh 3 MB (base64 ~4 MB) có thể vượt. Cùng một chỗ sửa với kế hoạch chatbot (Bước 11 của `ke-hoach-chatbot-deepseek.md`): **làm một lần, dùng chung**.
- **Chữ ký ECDSA đệm cả thân phản hồi trong bộ nhớ** (`ResponseSigning.cs`): danh sách bảng tin **không được nhúng ảnh**, chỉ trả mã ảnh; ảnh lấy riêng từng cái.

---

## 3. Thiết kế dữ liệu

### 3.1 Migration `co-so-du-lieu/migrations/V012__feed.sql`
Số `V011` đã dành cho bản sửa giờ mở cửa (Mục 3 của `ke-hoach-chinh-sua.md`); dùng `V012`. **Đọc `run.sh`** để chắc cách nó áp migration (theo tên, một lần).

```sql
-- Bản tin cộng đồng: bài đăng, ảnh, thích, bình luận, báo cáo.
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
    created_at    timestamptz not null default now(),
    edited_at     timestamptz,
    check (length(body) > 0 or shared_route is not null)  -- bài phải có chữ, hoặc ít nhất lộ trình (ảnh kiểm ở API)
);
create index post_feed_idx   on post (created_at desc, id desc) where status = 'visible';
create index post_poi_idx    on post (poi_id, created_at desc, id desc) where status = 'visible';
create index post_author_idx on post (author_id, created_at desc);
create index post_pending_idx on post (created_at) where status = 'pending';

create table post_media (
    id           bigint generated always as identity primary key,
    post_id      bigint references post (id) on delete cascade,   -- null: đã tải lên nhưng chưa gắn vào bài
    uploaded_by  bigint not null references app_user (id) on delete cascade,
    position     smallint not null default 0,
    data         bytea not null check (octet_length(data)  between 1 and 3000000),   -- ảnh đầy đủ (≤ 1600 px)
    thumb        bytea not null check (octet_length(thumb) between 1 and 250000),    -- ảnh thu nhỏ (≤ 640 px)
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

-- Đếm thích/bình luận bằng trigger: không để hai người bấm cùng lúc làm sai số.
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
```
Lưu ý: xóa người dùng (`AdminPlaces.DeleteUserAsync`) kéo theo xóa bài, thích, bình luận của họ nhờ `on delete cascade`: đó là hành vi chấp nhận được, nhưng ghi vào `VAN-HANH.md`.

### 3.2 Quyết định: lưu ảnh ở đâu
| | Trong PostgreSQL (`bytea`) | Trên đĩa (volume) |
|---|---|---|
| Hợp với | Đồ án/demo, vài trăm bài | Cộng đồng thật, hàng nghìn bài |
| Ưu | Cùng cơ chế ảnh địa điểm; **sao lưu một chỗ**; không thêm volume hay mã phục vụ tệp | Cơ sở dữ liệu gọn; Caddy phục vụ tĩnh |
| Nhược | Phình nhanh: 1.000 bài × 3 ảnh × ~350 KB ≈ 1 GB trong DB và trong mỗi bản sao lưu | Phải sao lưu thêm volume; thêm mã dọn tệp mồ côi |

**MVP: trong PostgreSQL**, đúng triết lý của `V010`, kèm hai biện pháp giữ nhỏ: ảnh đầy đủ ≤ 1600 px và **ảnh thu nhỏ ≤ 640 px (~40 KB)** là thứ danh sách thực sự tải; tối đa 4 ảnh mỗi bài. **Ngưỡng chuyển** sang đĩa: khi `select pg_size_pretty(sum(octet_length(data)+octet_length(thumb))) from post_media` vượt ~2 GB, hoặc bản sao lưu thành gánh nặng. Để việc chuyển dễ, **mọi truy cập ảnh đi qua một lớp** `MediaStore` (Mục 4.3): đổi nơi lưu chỉ sửa lớp đó.

### 3.3 Vì sao không dùng lại bảng `report` và `review`
`report.poi_id` bắt buộc khác null và `kind` chỉ có bốn giá trị của địa điểm; `review` có ràng buộc `unique (user_id, poi_id)` (một đánh giá mỗi địa điểm) khác hẳn bình luận. Dùng bảng mới, nhưng **theo cùng mô hình** để mục Kiểm duyệt của quản trị viên mở rộng tự nhiên.

---

## 4. Máy chủ (`may-chu/api/Feeds/`)

### 4.1 Tệp và đăng ký
- `Feeds/FeedOptions.cs` (mục `Feed` trong cấu hình, như `RateLimitSettings`)
- `Feeds/FeedEndpoints.cs` (bảng tin, bài, thích, bình luận, báo cáo)
- `Feeds/MediaEndpoints.cs` (tải ảnh lên, lấy ảnh)
- `Feeds/MediaStore.cs` (lớp duy nhất đụng tới nơi lưu ảnh)
- `Feeds/FeedAdminEndpoints.cs` (duyệt, báo cáo, ẩn/gỡ)
- `Feeds/FeedRules.cs` (hạn mức, phân cấp tin cậy, làm sạch chữ)
- `Program.cs`: `builder.Services.Configure<FeedOptions>(builder.Configuration.GetSection("Feed"));`, `builder.Services.AddSingleton<MediaStore>();`, `app.MapFeedEndpoints(); app.MapMediaEndpoints(); app.MapFeedAdminEndpoints();` cạnh dòng 129-138.

```csharp
public sealed class FeedOptions
{
    public const string SectionName = "Feed";
    public bool Enabled { get; set; } = false;               // MẶC ĐỊNH TẮT; bật bằng Feed__Enabled=true
    public int NewAccountHours { get; set; } = 24;           // tài khoản dưới ngưỡng này, hoặc chưa có bài nào được duyệt, là "người mới"
    public int ApprovedPostsToTrust { get; set; } = 2;
    public int PostsPerDayNew { get; set; } = 3;
    public int PostsPerDay { get; set; } = 15;
    public int CommentsPerDay { get; set; } = 100;
    public int MediaPerDay { get; set; } = 30;
    public int MaxMediaPerPost { get; set; } = 4;
    public int AutoHideAfterReports { get; set; } = 3;       // số người khác nhau báo cáo thì tự ẩn chờ quản trị viên
}
```
Khi `Enabled == false` mọi endpoint của Feeds trả **503** "Tính năng này chưa được bật." (không chặn máy chủ khởi động). `appsettings.json`: chỉ thêm `"Feed": { }`; `docker-compose.yml`: thêm `Feed__Enabled: ${FEED_ENABLED:-false}`; `.env.example` thêm `FEED_ENABLED=false`.

### 4.2 Hợp đồng API

| Phương thức, đường dẫn | Quyền | Ghi chú |
|---|---|---|
| `GET /v1/feed?cursor=&limit=20&poi=` | công khai (có token thì biết `likedByMe`) | Phân trang theo **con trỏ**; chỉ `status='visible'` |
| `GET /v1/feed/new-count?after={postId}` | công khai | Số bài mới hơn, cho chip "N bài mới" |
| `GET /v1/posts/{id}` | công khai | Một bài (mở từ thẻ địa điểm); bài `pending/hidden` chỉ tác giả và quản trị viên thấy |
| `GET /v1/me/posts` | đăng nhập | Bài của tôi, mọi trạng thái |
| `POST /v1/media` | đăng nhập, `heavy` | Tải **một** ảnh `{ "data": base64 }`, trả `{ id }` |
| `GET /v1/media/{id}?size=thumb\|full` | công khai nếu ảnh thuộc bài `visible` | Trả `{ id, contentType, data }` (base64) như `/v1/photos/{id}` |
| `POST /v1/posts` | đăng nhập, `feed` | `{ body, poiId?, route?: ["DL021",...], mediaIds?: [..≤4] }` → `201 { id, status }` |
| `DELETE /v1/posts/{id}` | tác giả hoặc admin | |
| `PUT /v1/posts/{id}/like`, `DELETE …/like` | đăng nhập, `feed` | Lặp lại không lỗi (idempotent); trả `204` |
| `GET /v1/posts/{id}/comments?cursor=` | công khai | Cũ trước, 30 bình luận mỗi trang |
| `POST /v1/posts/{id}/comments` | đăng nhập, `feed` | `{ body }` |
| `DELETE /v1/comments/{id}` | tác giả bình luận, tác giả bài, admin | |
| `POST /v1/posts/{id}/reports`, `POST /v1/comments/{id}/reports` | đăng nhập, `feed` | `{ kind, content? }` |
| `POST /v1/me/feed-terms` | đăng nhập | Ghi `feed_terms_at = now()` khi người dùng đồng ý quy tắc |
| `GET /v1/admin/feed/pending`, `POST /v1/admin/posts/{id}/decision` | admin | duyệt/từ chối bài `pending` |
| `GET /v1/admin/feed/reports`, `POST /v1/admin/feed/reports/{id}/resolve` | admin | `{ action: "dismiss" \| "hide" \| "remove" }` |
| `PUT /v1/admin/posts/{id}/status` | admin | khôi phục bài bị ẩn |

**Mục bảng tin trả về** (một bài):
```json
{
  "id": 481, "createdAt": "2026-11-13T08:21:00+07:00",
  "author": { "id": 12, "name": "Minh Anh" },
  "body": "Sáng sớm ở Đại Nội, chưa có ai...",
  "poi": { "id": "DL021", "name": "Đại Nội" },
  "route": [ { "poiId": "DL021", "name": "Đại Nội" }, { "poiId": "DL024", "name": "Chùa Thiên Mụ" } ],
  "media": [ { "id": 903, "width": 1600, "height": 1067 } ],
  "likeCount": 14, "commentCount": 3, "likedByMe": false, "mine": false, "status": "visible"
}
```
Trường `route` là **bản chụp** tại lúc đăng (jsonb), không phải khóa ngoại: lộ trình riêng tư của người đăng có thể bị xóa hay sửa sau đó mà bài vẫn đúng.

**Truy vấn bảng tin (phân trang con trỏ):**
```sql
select p.id, p.body, p.created_at, u.display_name, p.author_id, p.poi_id, poi.ten, p.shared_route,
       p.like_count, p.comment_count,
       exists (select 1 from post_like l where l.post_id = p.id and l.user_id = @me) as liked
from post p
join app_user u on u.id = p.author_id
left join poi on poi.id = p.poi_id
where p.status = 'visible'
  and (@poi::text is null or p.poi_id = @poi)
  and (@cc::timestamptz is null or (p.created_at, p.id) < (@cc, @ci))
order by p.created_at desc, p.id desc
limit @n + 1;       -- lấy thừa 1 dòng: có dòng thừa thì còn trang sau
```
Con trỏ = chuỗi mờ `base64("{ticks}:{id}")` của dòng cuối; tuyệt đối **không** dùng số trang (bài mới chen vào làm trang lệch). Ảnh của cả trang lấy bằng **một** truy vấn `select id, post_id, width, height from post_media where post_id = any(@ids) order by post_id, position`, không truy vấn từng bài (tránh N+1). Cột `poi.ten` là tên cột thật (đã thấy ở `SocialEndpoints.cs:169`); các tên cột khác của `poi` **đọc `V003__geo_tables.sql` trước**.

### 4.3 `MediaStore` và tải ảnh
```csharp
public sealed class MediaStore
{
    // Đây là chỗ duy nhất biết ảnh nằm ở đâu. Chuyển sang đĩa sau này chỉ sửa lớp này.
    public Task<long> SaveAsync(long userId, byte[] full, byte[] thumb, int w, int h, CancellationToken ct);
    public Task<(string ContentType, byte[] Data)?> ReadAsync(long id, bool thumb, CancellationToken ct);
}
```
Tải ảnh (`POST /v1/media`):
1. Kiểm `Feed:Enabled`, đăng nhập, người dùng đã `feed_terms_at` (nếu chưa: `403` "Hãy đồng ý quy tắc cộng đồng trước").
2. Hạn mức: `select count(*) from post_media where uploaded_by=@u and created_at > now() - interval '1 day'` < `MediaPerDay`; số ảnh **mồ côi** (chưa gắn bài) của người đó ≤ 8.
3. Giải base64, dài ≤ `PhotoEndpoints.MaxBodyBytes`.
4. **Gọi `PhotoEndpoints.Reencode(source)`** để có ảnh đầy đủ. Tạo ảnh thu nhỏ 640 px bằng cách gọi lại một hàm tương tự với `MaxSide = 640`: **tách** `Reencode` thành `Reencode(source, maxSide, quality)` và để bản cũ gọi `Reencode(source, 1600, 82)` (đây là chỗ duy nhất phải sửa mã cũ; chạy lại `ApiTests` sau đó).
5. Lưu và trả `{ id }`.

**Ngoại lệ kích thước thân**: `Program.cs:115-123` hiện chỉ nới giới hạn cho đường dẫn tải ảnh địa điểm và nhập địa điểm. Thêm `|| MediaEndpoints.IsUploadRequest(context.Request)` (POST đúng `/v1/media`), **chỉ** đường dẫn đó, đừng nới cho cả `/v1/posts`.

**Lấy ảnh** (`GET /v1/media/{id}`): chỉ trả khi ảnh thuộc bài `visible`, hoặc người hỏi là người tải lên/quản trị viên (như `CanSeeUnapprovedAsync` ở `PhotoEndpoints.cs:77`). Ảnh mồ côi không công khai.

### 4.4 Tạo bài (`POST /v1/posts`)
Thứ tự kiểm (trả `400/403/429` với câu tiếng Việt như các endpoint khác):
1. `Feed:Enabled`; đã đồng ý quy tắc.
2. `body` đã làm sạch (`FeedRules.CleanText`): bỏ ký tự điều khiển, ép chuỗi xuống dòng, cắt khoảng trắng; ≤ 2000 ký tự. **Không bao giờ coi là HTML.**
3. Phải có chữ, hoặc ảnh, hoặc lộ trình.
4. `poiId` (nếu có) tồn tại và `visible` (dùng lại kiểu `PoiExistsAsync` ở `SocialEndpoints.cs:211`).
5. `route` (nếu có): 2-12 mã địa điểm khác nhau, **đều tồn tại**; máy chủ **tự tra tên** để dựng jsonb, không tin tên máy khách gửi.
6. `mediaIds`: ≤ 4, **đều do chính người này tải lên** và còn mồ côi (không gắn bài của người khác).
7. Hạn mức bài mỗi ngày: người mới `PostsPerDayNew`, người đã tin cậy `PostsPerDay`.
8. **Trạng thái ban đầu**: người mới → `pending`; người đã tin cậy → `visible`. Một bộ lọc từ khóa đơn giản (danh sách trong cấu hình) đẩy bài vào `pending` thay vì từ chối.
9. Trong **một giao dịch**: chèn `post`, gắn `post_media.post_id` và `position`, ghi `audit` (`post.create`).

**Người mới / đã tin cậy**: tuổi tài khoản ≥ `NewAccountHours` **và** có ≥ `ApprovedPostsToTrust` bài đã `visible` do quản trị viên duyệt (đếm từ `audit_log` hoặc thêm cột `post.approved_by`; **chọn thêm cột** `approved_by bigint` cho dễ truy vấn). Quản trị viên và chủ quản đã duyệt coi là tin cậy.

### 4.5 Thích, bình luận, báo cáo
- Thích: `insert into post_like ... on conflict do nothing`; bỏ thích: `delete`. Số đếm do trigger lo.
- Chỉ thích/bình luận bài `visible`.
- Bình luận: hạn mức `CommentsPerDay`; làm sạch chữ; tác giả bài **không** bị chặn bình luận bài mình.
- Báo cáo: `kind` thuộc bốn giá trị, một người một lần cho mỗi thứ (khóa `unique` đã chặn), tối đa 20 báo cáo mở mỗi người (giống `MaxOpenReports` ở `SocialEndpoints.cs:27`). **Tự ẩn**: sau khi chèn, `select count(distinct user_id) from feed_report where post_id=@p and status='open'` ≥ `AutoHideAfterReports` thì `update post set status='hidden'` và ghi `audit` (`post.autohide`). Bài bị ẩn vẫn nằm đó chờ quản trị viên quyết định (khôi phục hoặc gỡ).
- Tác giả không báo cáo bài của chính mình.

### 4.6 Giới hạn tốc độ
Thêm chính sách `"feed"` cạnh `"heavy"` (`Program.cs:84`): ví dụ 40 yêu cầu/phút/IP; thêm `FeedPerMinute` vào `RateLimitSettings` và `ApiFixture` (`RateLimit:FeedPerMinute = 1000`). `POST /v1/media` dùng `"heavy"` như tải ảnh địa điểm. Hạn mức **theo tài khoản** (bài/bình luận/ảnh mỗi ngày) nằm ở `FeedRules`, vì bộ giới hạn theo IP không chặn được người dùng đổi IP hoặc dùng chung IP.

### 4.7 Dọn dẹp định kỳ
Ảnh mồ côi (đã tải nhưng không đăng) tích tụ. Thêm `Feeds/FeedMaintenance : BackgroundService` chạy mỗi giờ: `delete from post_media where post_id is null and created_at < now() - interval '1 day'`. **Đọc trước:** máy chủ chưa có `BackgroundService` nào (`Program.cs` không đăng ký); nếu thấy phức tạp, chạy câu xóa này khi có yêu cầu `POST /v1/media` (xác suất 1/50), vẫn đủ cho quy mô đồ án.

### 4.8 Quản trị
Ghi `audit` cho: `post.create`, `post.delete`, `post.approve`, `post.reject`, `post.hide`, `post.remove`, `post.restore`, `comment.delete`, `feed.report.resolve`. Quản trị viên khóa tài khoản người vi phạm bằng chức năng sẵn có (`UpdateUserAsync`).

---

## 5. Chống lạm dụng (phần quyết định chất lượng cộng đồng)

| Mối đe dọa | Biện pháp |
|---|---|
| Spam từ tài khoản tạo hàng loạt | Phân cấp tin cậy (4.4); bài người mới phải duyệt; hạn mức theo tài khoản; hạn mức ảnh |
| Nội dung xúc phạm, lừa đảo | Báo cáo + tự ẩn từ 3 người; quản trị viên gỡ; khóa tài khoản |
| Lộ thông tin cá nhân | EXIF đã bị bỏ (`Reencode`); loại báo cáo `privacy`; cảnh báo ở hộp soạn bài ("đừng đăng số điện thoại, biển số xe, ảnh người khác khi chưa được phép") |
| Liên kết độc hại | Hiển thị liên kết **như chữ thường**, không tự mở; không tải trước nội dung liên kết |
| Tiêm lệnh vào chatbot | **Không** đưa bài/bình luận vào công cụ của chatbot ở bản đầu (đã ghi ở `ke-hoach-chatbot-deepseek.md`) |
| Cố tình đốt tài nguyên (ảnh lớn) | `Reencode` đã chặn kích thước; hạn mức ảnh mỗi ngày; thân yêu cầu chỉ nới cho `/v1/media` |
| Tranh cãi pháp lý | **Quy tắc cộng đồng** hiển thị lần đầu đăng bài (cột `feed_terms_at`); nút báo cáo trên mọi bài; quy trình gỡ nội dung có thời hạn (Mục 10) |

**Quy tắc cộng đồng**: soạn một văn bản ngắn (≤ 10 dòng) lưu ở máy chủ hoặc trong ứng dụng; nội dung gợi ý: nói đúng sự thật về nơi mình đã đến; tôn trọng người khác và nơi thờ tự; không đăng nội dung vi phạm pháp luật, quảng cáo trá hình, thông tin cá nhân của người khác; quản trị viên có thể ẩn hoặc gỡ nội dung và khóa tài khoản.

---

## 6. Máy khách WinForms

### 6.1 Tệp mới
```
Services/Online/ApiFeedService.cs      (gọi API, phân tích JSON bằng JsonHelper như ApiRouteService)
Services/Data/FeedMediaCache.cs        (bộ đệm ảnh đĩa + bộ nhớ)
Panels/Feed/FeedPanel.cs               (panel nổi, rộng 440)
Panels/Feed/FeedList.cs                (danh sách cuộn, thẻ cao thấp khác nhau, tự vẽ)
Panels/Feed/PostCardPainter.cs         (dựng bố cục và vẽ MỘT thẻ bài; dùng cho cả đo và vẽ)
Panels/Feed/ComposerDialog.cs          (soạn bài)
Panels/Feed/CommentsDialog.cs          (xem và viết bình luận)
Panels/Feed/FeedFormat.cs              ("2 giờ trước", viết tắt số, màu ảnh đại diện theo tên)
```
Nhớ khai báo từng tệp trong `Hue_travel.csproj` (kiểu cũ liệt kê từng tệp).

### 6.2 `ApiFeedService`
```csharp
public sealed class ApiFeedService
{
    public Task<FeedPage> GetFeedAsync(string cursor, string poiId, CancellationToken ct);     // GET /v1/feed
    public Task<int> CountNewAsync(long afterPostId, CancellationToken ct);                    // GET /v1/feed/new-count
    public Task<long> UploadMediaAsync(byte[] jpeg, CancellationToken ct);                     // POST /v1/media (timeout dài)
    public Task<PostResult> CreatePostAsync(NewPost post, CancellationToken ct);               // POST /v1/posts
    public Task SetLikeAsync(long postId, bool like, CancellationToken ct);                    // PUT/DELETE /v1/posts/{id}/like
    public Task<CommentPage> GetCommentsAsync(long postId, string cursor, CancellationToken ct);
    public Task AddCommentAsync(long postId, string body, CancellationToken ct);
    public Task DeleteAsync(long postId, CancellationToken ct);
    public Task ReportAsync(long postId, string kind, string content, CancellationToken ct);
    public Task AcceptTermsAsync(CancellationToken ct);
}
```
Lỗi trả về cho người dùng đọc: dùng `ApiClient.ErrorText` như các màn khác. Cần **timeout riêng cho tải ảnh** (≥ 60 giây): cùng việc sửa `ApiClient` ở Bước 11 của kế hoạch chatbot (`http.Timeout = Infinite` + `CancelAfter` theo từng yêu cầu). Nếu chưa làm chatbot, làm việc đó **ở đây**, một lần.

### 6.3 `FeedMediaCache`
Làm theo mẫu `PoiPhotos` (đã đọc: bộ đệm giới hạn 48, tải lười, thu nhỏ một lần):
- đĩa: `%LOCALAPPDATA%\HueTravel\feed\{mediaId}_thumb.jpg` và `_full.jpg` (ảnh bài đã đăng **không đổi**, nên đệm vĩnh viễn; dọn khi thư mục > 200 MB, xóa cũ nhất);
- bộ nhớ: tối đa ~60 ảnh đã giải mã; **sao chép** bitmap vào bộ đệm của mình (đừng giữ tham chiếu tới bitmap của `PoiPhotos`, nó có thể bị `Dispose` khi đệm đầy);
- `Task<Bitmap> GetAsync(long mediaId, bool thumb, CancellationToken ct)`; trong lúc chờ trả `null` để thẻ vẽ ô giữ chỗ; khi tải xong gọi sự kiện `Loaded` để `FeedList` vẽ lại;
- tải **song song tối đa 3** ảnh (`SemaphoreSlim`), hủy khi cuộn khỏi vùng nhìn.

### 6.4 `FeedList`: danh sách cuộn tự vẽ
Phần khó nhất. Thẻ có chiều cao thay đổi (độ dài chữ, số ảnh, có lộ trình hay không), nên **không** dùng một `Control` cho mỗi bài.

- Một `Control` duy nhất tự vẽ (như `PoiRowList`), giữ `List<PostVm>`; mỗi `PostVm` lưu `Height` đã đo cho bề ngang hiện tại. Đo lại khi đổi cỡ.
- `PostCardPainter.Layout(Graphics, PostVm, width)` trả các hình chữ nhật (đầu thẻ, chữ, lưới ảnh, chip địa điểm, thẻ lộ trình, hàng nút) **dùng chung cho vẽ và kiểm tra nhấp chuột** (tránh lệch giữa chỗ vẽ và chỗ bấm).
- Chỉ vẽ các thẻ giao với vùng nhìn; cuộn bằng bánh xe, thanh cuộn mảnh (copy phần thanh cuộn của `PoiRowList`, dòng 343-351).
- Đến cuối danh sách thì tự tải trang kế (`nextCursor`); dòng "Đang tải thêm…" ở đáy.
- Chip "N bài mới" nổi ở đầu: bấm để tải lại từ đầu.
- Trạng thái rỗng/lỗi/ngoại tuyến: ghi riêng ở 6.8.

**Bố cục thẻ bài** (độ rộng panel 440 → thẻ ~408):
```
╭─────────────────────────────────────────╮
│ (MA)  Minh Anh                    ⋯      │  ảnh đại diện tròn (chữ cái đầu, màu theo tên), tên, "2 giờ trước", menu
│       2 giờ trước · 📍 Đại Nội           │  chip địa điểm: bấm thì bay tới địa điểm
│ Sáng sớm ở Đại Nội, chưa có ai...        │  chữ tối đa 6 dòng + "Xem thêm"
│ ╭──────────────╮╭──────────────╮         │  lưới ảnh 1/2/3/4 ảnh, bo RadiusMd
│ │              ││              │         │
│ ╰──────────────╯╰──────────────╯         │
│ ╭─ Lộ trình: Đại Nội → Thiên Mụ → …  ─╮  │  thẻ lộ trình (nếu có) + nút "Dùng lộ trình này"
│ ♡ 14   💬 3                       ⚑      │  thích, bình luận, báo cáo
╰─────────────────────────────────────────╯
```
Lưới ảnh: 1 ảnh → ngang tỉ lệ ảnh thật (cao tối đa 280); 2 ảnh → hai ô vuông cạnh nhau; 3 ảnh → một lớn bên trái + hai nhỏ xếp dọc bên phải; 4 ảnh → lưới 2×2. Bấm ảnh mở `PhotoViewer` (đã có) với danh sách ảnh đầy đủ của bài.

### 6.5 Hộp soạn bài `ComposerDialog`
- Ô chữ nhiều dòng (≤ 2000, đếm còn lại), gợi ý "Bạn đã thấy gì ở Huế?".
- **Thêm ảnh** (tối đa 4): `OpenFileDialog` (JPEG/PNG), có thể chọn nhiều tệp; hiện ảnh thu nhỏ có nút xóa.
- **Thu nhỏ ảnh ở máy khách trước khi gửi**: cạnh dài ≤ 1600 px, JPEG chất lượng 82, bằng `System.Drawing` (vẽ lại sang bitmap mới là bỏ luôn EXIF). Mục đích: tiết kiệm băng thông và nằm dưới giới hạn base64 8 MB. Máy chủ vẫn xử lý lại (không tin máy khách).
- **Gắn địa điểm**: nút mở `Dialogs.PickPlaces(this, "Bạn đã ở đâu?", 1, ..., "Xong")`. Nếu mở soạn bài **từ thẻ địa điểm** thì điền sẵn.
- **Đính kèm lộ trình**: nút "Chia sẻ lộ trình" chọn từ lộ trình đang dựng (`TripPlan`) hoặc từ lộ trình đã lưu (`GET /v1/itineraries`). Gửi danh sách mã địa điểm; máy chủ tự dựng tên.
- Lần đầu: hiện **quy tắc cộng đồng** với nút "Tôi đồng ý" (gọi `AcceptTermsAsync`).
- Nút Đăng: khóa trong lúc tải; hiện "Đang tải ảnh 2/4…" từng ảnh (tải **tuần tự**, mỗi ảnh một yêu cầu, rồi mới gọi `CreatePostAsync`); lỗi giữa chừng thì giữ nội dung, cho thử lại **chỉ ảnh lỗi**.
- Nếu máy chủ trả `pending`: thông báo "Bài của bạn đã gửi và sẽ hiện sau khi quản trị viên duyệt."

### 6.6 Bình luận
`CommentsDialog` mở khi bấm biểu tượng bình luận (làm theo cấu trúc `ReviewsDialog`: **đọc `Account/ReviewsDialog.cs` trước**): danh sách cũ trước, ô nhập ở dưới, xóa bình luận của mình, báo cáo bình luận của người khác.

### 6.7 Khách và quyền (phụ thuộc Mục 1 của `ke-hoach-chinh-sua.md`)
Khách **xem** bảng tin và bình luận. Thích, bình luận, đăng, báo cáo đều đi qua `RunSignedIn(reason, action)` (mở form đăng nhập rồi làm tiếp). **Làm Mục 1 trước.** Chế độ ngoại tuyến: tab hiện "Cần kết nối máy chủ để xem Khoảnh khắc." (không thông báo lỗi kỹ thuật).

### 6.8 Trạng thái cần xử lý
| Tình huống | Hiển thị |
|---|---|
| Đang tải lần đầu | 3 thẻ khung xương (skeleton) |
| Rỗng | Biểu tượng + "Chưa có khoảnh khắc nào. Hãy là người đầu tiên chia sẻ." + nút Đăng |
| Rỗng theo địa điểm | "Chưa ai chia sẻ về nơi này." + nút "Chia sẻ khoảnh khắc ở đây" |
| Mất mạng giữa chừng | Giữ danh sách đã có, hiện thanh "Không kết nối được" + nút thử lại |
| Bài đang chờ duyệt (của tôi) | Nhãn "Đang chờ duyệt" trên thẻ, chỉ mình tôi thấy |
| Bài bị ẩn (của tôi) | Nhãn "Đã bị ẩn bởi quản trị viên" |
| Ảnh lỗi | Ô xám có biểu tượng ảnh hỏng, không báo lỗi to |

### 6.9 Nối vào cửa sổ chính
- **Nhãn "Khoảnh khắc" trên `NavRail`.** Mảng hiện có (`NavRail.cs:20-22`) là `Icons`, `Labels`, `Tips`; thêm phần tử mới vào **cả ba** theo đúng chỉ số: biểu tượng `explore` hoặc một biểu tượng tự vẽ (ví dụ máy ảnh/ảnh, xem `IconFont.cs`), nhãn `"Khoảnh khắc"`, gợi ý `"Chia sẻ và xem khoảnh khắc du lịch Huế"`. Vì nhãn dài hơn các nhãn khác, chọn **một** trong ba cách (từ ít sửa đến nhiều sửa):
  1. Giảm font nhãn 11 → 10 và rộng thanh dọc 80 (như hiện tại; nếu đã làm hướng dẫn giao diện, thanh là 64 thì nâng lên 72). Đo bằng `TextRenderer.MeasureText("Khoảnh khắc", ...)` rồi chọn.
  2. Vẽ nhãn **hai dòng** ("Khoảnh" / "khắc") chỉ cho mục này: bỏ cờ `SingleLine` và tăng chiều cao nhãn ở `NavRail.cs:168-171`; tăng `ItemHeight` 76 → 84 cho cả thanh để các mục đều nhau.
  3. Bỏ nhãn khi thanh hẹp, chỉ hiện biểu tượng + gợi ý khi rê chuột (đã có `Tips`); nhất quán nhưng kém dễ hiểu với người mới.
  Tôi nghiêng về cách 1; nếu chữ vẫn chạm mép thì cách 2.
- `Shell/NavRail.cs`: thêm mục thứ năm. **Đọc `MainForm.cs` quanh `LayerSlot`** trước: `sidePanels[]` hiện là `{ explore, routeOptimizer, travelGuide, account, layer }` theo chỉ số `NavRail.Explore/Route/Guide/Profile` (0-3) và `LayerSlot`. Cách ít rủi ro: thêm `public const int Feed = 3;`, đổi `Profile = 4`, `LayerSlot = 5`, và sửa mảng `sidePanels` cho khớp; tìm mọi chỗ dùng chỉ số bằng `Grep "NavRail\.|LayerSlot|sidePanels"`. Thanh dọc cao thêm 76 px; cập nhật `WantedHeight` nếu đã làm hướng dẫn giao diện.
- `Shell/MainForm.Designer.cs`: tạo `FeedPanel` (kích cỡ 440×640) và `AttachToSide(this, SideLeft, SideTop, SideBottom)` như các panel khác.
- `Panels/PoiIdentify/PoiIdentifyCard.cs`: thêm sự kiện `FeedRequested` và nút "Khoảnh khắc" (biểu tượng `photo_camera`: chưa có trong font tập con, vẽ tay hoặc dùng `explore`); `MainForm` mở `FeedPanel` đã lọc theo địa điểm đó.
- **Sự kiện từ bài**: `PlaceClicked` → `ShowOnMap(poi)` + `ShowPoiCard(poi)` (đã có ở `MainForm.cs:127`); `RouteChosen(title, places)` → `routeOptimizerPanel.LoadTourAsync(places)` giống `TourChosen` (`MainForm.cs:221-225`), **bỏ qua mã địa điểm không còn trong `PoiCatalog`**.

### 6.10 Quản trị
`Admin/ModerationSection.cs` (đã có tab đánh giá và báo cáo; **đọc nó trước**) thêm hai tab: "Bài chờ duyệt" (xem chữ + ảnh, Duyệt / Từ chối) và "Báo cáo bài viết" (xem bài/bình luận bị báo cáo, chọn Bỏ qua / Ẩn / Gỡ, xem số người báo cáo). Dùng lại các endpoint ở 4.2.

---

## 7. Kiểm thử

### Máy chủ (`may-chu/kiem-thu`, theo mẫu `ApiFixture` và các bộ `AccountFeaturesTests`, `RolesTests`; cần `HUE_TEST_DB`)
| Ca | Mong đợi |
|---|---|
| `Feed:Enabled=false` | mọi endpoint Feeds trả 503, endpoint khác bình thường |
| Chưa đồng ý quy tắc → tải ảnh / đăng | 403 |
| Đăng bài chữ hợp lệ (người đã tin cậy) | 201, `visible`, hiện trong bảng tin |
| Người mới đăng | 201, `pending`, **không** hiện trong bảng tin công khai, hiện ở `/v1/me/posts` |
| Quản trị viên duyệt | bài thành `visible`, `audit` có dòng `post.approve` |
| Bài rỗng (không chữ, ảnh, lộ trình) | 400 |
| Chữ > 2000 ký tự | 400 |
| `poiId` không tồn tại | 404/400 |
| `route` có địa điểm lạ, hoặc trùng, hoặc > 12 | 400; tên trong jsonb do máy chủ dựng (gửi tên giả, kết quả vẫn là tên thật) |
| `mediaIds` của người khác / đã gắn vào bài khác | 400 |
| Ảnh quá lớn, không phải ảnh, nhỏ hơn 200 px | 400 (kế thừa `Reencode`) |
| Ảnh sau khi lưu | **không còn EXIF** (đọc lại, kiểm không có metadata) |
| Vượt hạn mức bài/ngày, ảnh/ngày, bình luận/ngày | 429/400 kèm câu tiếng Việt |
| Thích hai lần liên tiếp | `like_count` vẫn là 1; bỏ thích hai lần: 0, không âm |
| Hai người thích cùng lúc | `like_count` đúng (trigger) |
| Bình luận rồi ẩn bình luận | `comment_count` giảm |
| Báo cáo cùng bài hai lần bởi một người | lần hai bị từ chối |
| 3 người khác nhau báo cáo | bài thành `hidden`, `audit` có `post.autohide` |
| Bài `hidden`/`pending` | người lạ không thấy qua `GET /v1/posts/{id}` và `GET /v1/media/{id}` |
| Người dùng bị khóa | không đăng/bình luận được (token bị từ chối) |
| Xóa người dùng | bài, thích, bình luận của họ biến mất |
| Phân trang con trỏ | hai trang liên tiếp không trùng, không sót, kể cả khi có bài mới chen vào giữa |
| Chữ ký phản hồi | `X-Sig` hợp lệ trên mọi phản hồi của Feeds |
| `UseResponseSigning` với ảnh | phản hồi ảnh (base64 vài trăm KB) vẫn ký đúng |

Kèm **chạy lại toàn bộ test cũ** sau khi tách `Reencode`.
Kiểm `EXPLAIN` cho truy vấn bảng tin trên dữ liệu thử ≥ 10.000 bài: phải dùng `post_feed_idx`, không quét toàn bảng.

### Máy khách (thử tay)
1. Khách mở tab: thấy bảng tin; bấm thích → hiện form đăng nhập; đăng nhập xong thì **thích được ngay** (việc được làm tiếp).
2. Đăng bài chữ; đăng bài 1, 2, 3, 4 ảnh (kiểm bố cục lưới); ảnh dọc, ảnh ngang, ảnh PNG, ảnh 12 MB (phải được thu nhỏ trước khi gửi).
3. Rút mạng khi đang tải ảnh thứ 3 → báo lỗi, giữ nội dung, thử lại chỉ ảnh lỗi.
4. Bài người mới: thấy nhãn "Đang chờ duyệt"; quản trị viên duyệt xong thì hiện ra sau lần tải lại.
5. Cuộn danh sách 100 bài: mượt, ảnh tải dần, không giật, bộ nhớ không tăng mãi (xem Task Manager).
6. Bấm chip địa điểm → bản đồ bay tới và hiện thẻ địa điểm.
7. Bài có lộ trình → "Dùng lộ trình này" mở form lộ trình đã nạp điểm (bỏ qua điểm không còn).
8. Đổi kích cỡ cửa sổ/panel: thẻ đo lại, không cắt chữ; 125% và 150% DPI.
9. Báo cáo bài; xóa bài của mình; xóa bình luận của mình.
10. Ngoại tuyến: tab báo cần kết nối, không treo.

---

## 8. Các giai đoạn thực hiện

Mỗi giai đoạn kết thúc bằng bản chạy được; **làm xong và thử bằng `curl` ở máy chủ trước khi đụng giao diện** (như kế hoạch chatbot).

### Giai đoạn 0. Nền và quyết định (không viết mã chức năng)
- [ ] Chốt: cách hiện nhãn "Khoảnh khắc" trên thanh dọc (Mục 6.9), phạm vi MVP, **có HTTPS chưa**, chính sách kiểm duyệt (ai là người duyệt, trong bao lâu), nội dung quy tắc cộng đồng.
- [ ] Làm **Mục 1** (cổng đăng nhập cho khách) và **sửa `ApiClient`** (timeout theo yêu cầu) nếu chưa.
- [ ] Nếu muốn giao diện đồng bộ: có sẵn nút, chip, `PoiThumbs`, token từ `huong-dan-trien-khai-giao-dien.md` (Bước 1-5).
- [ ] Đọc: `run.sh`, `V003__geo_tables.sql`, `ReviewsDialog.cs`, `ModerationSection.cs`, `MainForm.cs` quanh `LayerSlot`.

### Giai đoạn 1. Máy chủ: đọc được và đăng được
- [ ] `V012__feed.sql` + chạy thử trên cơ sở dữ liệu thử; kiểm trigger bằng tay.
- [ ] `FeedOptions`, công tắc `Feed:Enabled`, đăng ký trong `Program.cs`.
- [ ] Tách `Reencode(source, maxSide, quality)`; test cũ vẫn xanh.
- [ ] `MediaStore`, `POST /v1/media`, `GET /v1/media/{id}`; ngoại lệ kích thước thân.
- [ ] `POST /v1/posts`, `GET /v1/feed`, `GET /v1/posts/{id}`, `DELETE`, `GET /v1/me/posts`.
- [ ] `FeedRules` (làm sạch chữ, người mới, hạn mức).
- [ ] Test máy chủ (bảng Mục 7, phần bài/ảnh/phân trang).
- **Nghiệm thu:** bằng `curl` đăng nhập, tải ảnh, đăng bài, đọc bảng tin có con trỏ.

### Giai đoạn 2. Máy chủ: tương tác và kiểm duyệt
- [ ] Thích, bình luận, báo cáo, tự ẩn.
- [ ] Endpoint quản trị: hàng đợi duyệt, xử lý báo cáo, khôi phục.
- [ ] `audit` đầy đủ; hạn mức theo tài khoản; chính sách `"feed"`.
- [ ] Test phần còn lại của Mục 7; `EXPLAIN` trên 10.000 bài.

### Giai đoạn 3. Máy khách: xem bảng tin
- [ ] `ApiFeedService` (đọc), `FeedMediaCache`.
- [ ] `FeedList` + `PostCardPainter` (chưa có tương tác ngoài cuộn và bấm ảnh).
- [ ] `FeedPanel`, nối vào `NavRail`/`MainForm`; khách xem được.
- [ ] Trạng thái rỗng/lỗi/ngoại tuyến; chip "N bài mới" (thăm dò mỗi 45 giây).

### Giai đoạn 4. Máy khách: tương tác và đăng bài
- [ ] Thích, bình luận (`CommentsDialog`), báo cáo, xóa.
- [ ] `ComposerDialog`: chữ, ảnh (thu nhỏ trước), địa điểm; quy tắc cộng đồng lần đầu.
- [ ] Đính kèm và dùng lại lộ trình.
- [ ] Nối vào thẻ địa điểm (`FeedRequested`) và chip địa điểm trên bài.
- [ ] Hai tab kiểm duyệt trong `ModerationSection`.

### Giai đoạn 5. Hoàn thiện
- [ ] Điều kiện mở công khai (Mục 10).
- [ ] Cập nhật `VAN-HANH.md` (cách bật, sao lưu tăng thêm bao nhiêu, cách xử lý báo cáo, cách xóa tài khoản và hệ quả).
- [ ] Tài liệu: `so-do.md` thêm bảng mới.

### Để sau (Giai đoạn 6+)
Lớp "Khoảnh khắc" trên bản đồ (đếm bài theo địa điểm, điểm sáng trên bản đồ); chuyển ảnh sang đĩa khi vượt ngưỡng; theo dõi người dùng; thông báo; ảnh đại diện; video (chỉ nhận liên kết); bình luận lồng nhau; hashtag.

---

## 9. Phối hợp với các kế hoạch khác

| Việc chung | Làm thế nào |
|---|---|
| Mục 1 (khách) | **Điều kiện tiên quyết** của Giai đoạn 4 |
| `ApiClient` timeout theo yêu cầu | Một chỗ sửa dùng chung với chatbot; ai làm trước thì người sau bỏ qua |
| `V011` giờ mở cửa | Feeds dùng `V012`; nếu bạn làm Feeds **trước** `V011`, đổi tên số cho đúng thứ tự chạy |
| `NavRail` thêm mục | Chatbot đổi nhãn "Hướng dẫn" thành "Trợ lý"; giao diện đổi kích cỡ thanh dọc. Làm cùng lượt, đừng sửa ba lần |
| Hướng dẫn giao diện | Thẻ bài, nút, chip, bo góc dùng token ở đó; nếu chưa làm, dùng tạm thang hiện có rồi đổi sau |
| Chatbot | Không nối bài đăng vào công cụ của chatbot |
| Lộ trình đã lưu | Feeds chỉ **đọc** lộ trình của chính người đăng khi chọn đính kèm, và lưu bản chụp |

---

## 10. Điều kiện trước khi mở công khai

Nếu chỉ chạy nội bộ/đồ án, bỏ qua được phần lớn. **Trước khi cho người lạ dùng:**
- [ ] **HTTPS**: có tên miền, bật Caddy tự cấp chứng chỉ (hiện `auto_https off` vì chưa có tên miền). Mạng xã hội gửi mật khẩu, ảnh và bình luận; HTTP trần không chấp nhận được.
- [ ] Có người **thật sự xử lý** hàng đợi duyệt và báo cáo, trong thời hạn nói rõ.
- [ ] Quy tắc cộng đồng được viết và hiển thị.
- [ ] Sao lưu cơ sở dữ liệu đã tính phần ảnh (DB sẽ lớn hơn nhiều); thử khôi phục một lần.
- [ ] Đã kiểm hạn mức, tự ẩn, khóa tài khoản trên môi trường thử với dữ liệu giả.
- [ ] **Kiểm tra quy định pháp luật về dịch vụ mạng xã hội và trách nhiệm với nội dung người dùng tại Việt Nam.** Tôi nhớ có Nghị định 147/2024/NĐ-CP về quản lý và cung cấp dịch vụ Internet và thông tin trên mạng (có điều khoản xác thực tài khoản và gỡ nội dung theo yêu cầu), nhưng **chưa xác minh** điều khoản cụ thể và phạm vi áp dụng cho một dự án quy mô nhỏ; đây không phải tư vấn pháp lý, hãy hỏi người có chuyên môn.
- [ ] `Feed__Enabled=true` chỉ bật sau khi các mục trên xong.

---

## 11. Rủi ro

| Rủi ro | Cách giảm |
|---|---|
| Spam và nội dung xấu (không có email/OTP) | Phân cấp tin cậy, duyệt bài người mới, hạn mức theo tài khoản, tự ẩn từ 3 báo cáo, `Feed:Enabled` tắt mặc định |
| DB phình do ảnh | Ảnh ≤ 1600 px, ≤ 4 ảnh/bài, thumb 640 px; theo dõi dung lượng; `MediaStore` cho phép chuyển sang đĩa |
| Phản hồi ký nặng và chậm | Danh sách **không** nhúng ảnh; ảnh lấy riêng; bộ đệm đĩa ở máy khách |
| `FeedList` tự vẽ phức tạp, dễ giật | Chỉ vẽ phần nhìn thấy; đo chiều cao một lần mỗi bề ngang; bộ đệm ảnh có giới hạn; thử 100+ bài sớm (Giai đoạn 3) |
| Tải ảnh lỗi giữa chừng gây rác | Ảnh mồ côi dọn mỗi giờ; tải tuần tự và thử lại từng ảnh |
| Sửa `Reencode` làm hỏng ảnh địa điểm | Tách tham số, giữ mặc định cũ; chạy lại test cũ |
| Đếm thích/bình luận sai khi đồng thời | Trigger, không đếm ở mã ứng dụng |
| Phân trang lệch khi có bài mới | Con trỏ `(created_at, id)`, không số trang |
| Xóa tài khoản kéo theo mất bài | Ghi rõ trong `VAN-HANH.md`; xem xét "ẩn danh hóa" thay vì xóa nếu cần giữ bình luận |
| Rủi ro pháp lý khi công khai | Mục 10; hoãn công khai đến khi kiểm tra xong |
| HTTP trần | Điều kiện bắt buộc trước khi công khai |
| Quá lớn so với thời gian | Giai đoạn 1-3 đã cho một bản tin **đọc được**; bỏ Giai đoạn 4 vẫn có sản phẩm trình bày được |

---

## 12. Danh sách việc (tóm tắt)

- [ ] 0 Chốt quyết định; làm Mục 1 và sửa `ApiClient`
- [ ] 1 `V012__feed.sql` + trigger
- [ ] 2 `FeedOptions` + công tắc + `Program.cs`
- [ ] 3 Tách `Reencode`; `MediaStore`; `/v1/media`; ngoại lệ thân yêu cầu
- [ ] 4 Bài: tạo, đọc bảng tin con trỏ, xóa, bài của tôi; `FeedRules`
- [ ] 5 Thích, bình luận, báo cáo, tự ẩn
- [ ] 6 Quản trị: duyệt, báo cáo, khôi phục; `audit`
- [ ] 7 Test máy chủ + `EXPLAIN` trên 10.000 bài
- [ ] 8 `ApiFeedService` + `FeedMediaCache`
- [ ] 9 `FeedList` + `PostCardPainter` + `FeedPanel` + nối `NavRail`
- [ ] 10 Thích/bình luận/báo cáo/xóa ở máy khách
- [ ] 11 `ComposerDialog` (ảnh, địa điểm, lộ trình, quy tắc)
- [ ] 12 Tích hợp thẻ địa điểm + chip địa điểm + dùng lại lộ trình
- [ ] 13 Hai tab kiểm duyệt trong quản trị
- [ ] 14 Thử tay (Mục 7), dọn ảnh mồ côi, tài liệu vận hành
- [ ] 15 Điều kiện mở công khai (Mục 10)
