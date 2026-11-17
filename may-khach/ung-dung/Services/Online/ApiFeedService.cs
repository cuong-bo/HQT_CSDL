using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hue_travel.Services.Online
{
    public sealed class FeedPlaceInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public sealed class FeedPostInfo
    {
        public long Id { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public long AuthorId { get; set; }
        public string AuthorName { get; set; }
        public string Body { get; set; }
        public FeedPlaceInfo Poi { get; set; }
        public IReadOnlyList<FeedPlaceInfo> Route { get; set; }
        public IReadOnlyList<long> MediaIds { get; set; }
        public int LikeCount { get; set; }
        public int CommentCount { get; set; }
        public bool LikedByMe { get; set; }
        public bool Mine { get; set; }
        public string Status { get; set; }

        /// <summary>"vừa xong", "5 phút trước", "3 giờ trước", "2 ngày trước", else the date.</summary>
        public string TimeText => TimeAgo(CreatedAt, DateTimeOffset.Now);

        public static string TimeAgo(DateTimeOffset at, DateTimeOffset now)
        {
            TimeSpan span = now - at;
            if (span < TimeSpan.FromMinutes(1)) return "vừa xong";
            if (span < TimeSpan.FromHours(1)) return (int)span.TotalMinutes + " phút trước";
            if (span < TimeSpan.FromDays(1)) return (int)span.TotalHours + " giờ trước";
            if (span < TimeSpan.FromDays(7)) return (int)span.TotalDays + " ngày trước";
            return at.ToLocalTime().ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        }
    }

    public sealed class FeedPage
    {
        public IReadOnlyList<FeedPostInfo> Items { get; set; }

        /// <summary>The cursor of the next (older) page, or null at the end.</summary>
        public string Next { get; set; }
    }

    public sealed class FeedCommentInfo
    {
        public long Id { get; set; }
        public string AuthorName { get; set; }
        public string Body { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public bool Mine { get; set; }
    }

    public sealed class FeedMe
    {
        public bool Trusted { get; set; }
        public int MaxMediaPerPost { get; set; }
    }

    /// <summary>"Khoảnh khắc": the community feed on the server. Every method returns the server's own message when it fails (503 while it is switched off).</summary>
    public sealed class ApiFeedService
    {
        private static readonly TimeSpan UploadTimeout = TimeSpan.FromSeconds(60);

        public static readonly string[] ReportKinds = { "spam", "inappropriate", "wrong_info", "privacy", "other" };
        public static readonly string[] ReportKindNames = { "Spam, quảng cáo", "Nội dung không phù hợp", "Thông tin sai", "Lộ thông tin cá nhân", "Lý do khác" };

        private readonly ApiClient api;

        public ApiFeedService(ApiClient api)
        {
            this.api = api;
        }

        public async Task<ApiResult<FeedPage>> ListAsync(string cursor, string poiId, CancellationToken ct)
        {
            string path = "/v1/feed?limit=20" + (string.IsNullOrEmpty(cursor) ? "" : "&cursor=" + Uri.EscapeDataString(cursor))
                          + (string.IsNullOrEmpty(poiId) ? "" : "&poi=" + Uri.EscapeDataString(poiId));
            ApiResponse r = await api.GetAsync(path, ct).ConfigureAwait(true);
            if (!r.IsSuccess) return new ApiResult<FeedPage> { Error = r.Error };
            return new ApiResult<FeedPage> { Value = new FeedPage { Items = ParsePosts(JsonHelper.List(r.Object, "items")), Next = JsonHelper.Str(r.Object, "next") } };
        }

        public async Task<int> NewCountAsync(long afterId, CancellationToken ct)
        {
            ApiResponse r = await api.GetAsync("/v1/feed/new-count?after=" + afterId, ct).ConfigureAwait(true);
            return r.IsSuccess ? (int)JsonHelper.Long(r.Object, "count") : 0;
        }

        public async Task<ApiResult<FeedMe>> MeAsync(CancellationToken ct)
        {
            ApiResponse r = await api.GetAsync("/v1/me/feed", ct).ConfigureAwait(true);
            if (!r.IsSuccess) return new ApiResult<FeedMe> { Error = r.Error };
            return new ApiResult<FeedMe>
            {
                Value = new FeedMe { Trusted =JsonHelper.Bool(r.Object, "trusted"), MaxMediaPerPost = Math.Max(1, (int)JsonHelper.Long(r.Object, "maxMediaPerPost")) },
            };
        }

        public Task<string> LikeAsync(long postId, bool like, CancellationToken ct) =>
            ErrorOf(like ? api.PutAsync("/v1/posts/" + postId + "/like", null, ct) : api.DeleteAsync("/v1/posts/" + postId + "/like", ct));

        public Task<string> DeletePostAsync(long postId, CancellationToken ct) => ErrorOf(api.DeleteAsync("/v1/posts/" + postId, ct));

        public Task<string> ReportPostAsync(long postId, string kind, string note, CancellationToken ct) =>
            ErrorOf(api.PostAsync("/v1/posts/" + postId + "/reports", new Dictionary<string, object> { { "kind", kind }, { "content", note } }, ct));

        public async Task<ApiResult<List<FeedCommentInfo>>> CommentsAsync(long postId, CancellationToken ct)
        {
            var all = new List<FeedCommentInfo>();
            long after = 0;
            for (int page = 0; page < 10; page++)   // 300 comments are enough for the window
            {
                ApiResponse r = await api.GetAsync("/v1/posts/" + postId + "/comments?after=" + after, ct).ConfigureAwait(true);
                if (!r.IsSuccess) return new ApiResult<List<FeedCommentInfo>> { Error = r.Error };
                foreach (IDictionary<string, object> d in (JsonHelper.List(r.Object, "items") ?? new List<object>()).OfType<IDictionary<string, object>>())
                {
                    IDictionary<string, object> author = JsonHelper.Obj(d, "author");
                    all.Add(new FeedCommentInfo
                    {
                        Id = JsonHelper.Long(d, "id"), AuthorName = JsonHelper.Str(author, "name"), Body = JsonHelper.Str(d, "body"),
                        CreatedAt = ParseTime(JsonHelper.Str(d, "createdAt")), Mine = JsonHelper.Bool(d, "mine"),
                    });
                }
                if (r.Object == null || !r.Object.ContainsKey("next") || r.Object["next"] == null) break;
                after = JsonHelper.Long(r.Object, "next");
            }
            return new ApiResult<List<FeedCommentInfo>> { Value = all };
        }

        public Task<string> AddCommentAsync(long postId, string body, CancellationToken ct) =>
            ErrorOf(api.PostAsync("/v1/posts/" + postId + "/comments", new Dictionary<string, object> { { "body", body } }, ct));

        public Task<string> DeleteCommentAsync(long commentId, CancellationToken ct) => ErrorOf(api.DeleteAsync("/v1/comments/" + commentId, ct));

        /// <summary>Sends one picture (a JPEG or PNG already shrunk by the caller); returns its id.</summary>
        public async Task<ApiResult<long>> UploadAsync(byte[] image, CancellationToken ct)
        {
            ApiResponse r = await api.PostAsync("/v1/media", new Dictionary<string, object> { { "data", Convert.ToBase64String(image) } }, UploadTimeout, ct).ConfigureAwait(true);
            return r.IsSuccess ? new ApiResult<long> { Value = JsonHelper.Long(r.Object, "id") } : new ApiResult<long> { Error = r.Error };
        }

        /// <summary>Makes a post; the result is "visible" (published) or "pending" (waits for an admin).</summary>
        public async Task<ApiResult<string>> CreateAsync(string body, string poiId, IList<string> routeIds, IList<long> mediaIds, CancellationToken ct)
        {
            var payload = new Dictionary<string, object> { { "body", body }, { "mediaIds", mediaIds.ToArray() } };
            if (!string.IsNullOrEmpty(poiId)) payload["poiId"] = poiId;
            if (routeIds != null && routeIds.Count > 0) payload["route"] = routeIds.ToArray();
            ApiResponse r = await api.PostAsync("/v1/posts", payload, ct).ConfigureAwait(true);
            return r.IsSuccess ? new ApiResult<string> { Value = JsonHelper.Str(r.Object, "status") } : new ApiResult<string> { Error = r.Error };
        }

        /// <summary>The bytes of a picture (JPEG), thumbnail or full size; null when it cannot be had.</summary>
        public async Task<byte[]> MediaAsync(long id, bool thumb, CancellationToken ct)
        {
            ApiResponse r = await api.GetAsync("/v1/media/" + id + "?size=" + (thumb ? "thumb" : "full"), ct).ConfigureAwait(true);
            string data = r.IsSuccess ? JsonHelper.Str(r.Object, "data") : null;
            if (string.IsNullOrEmpty(data)) return null;
            try { return Convert.FromBase64String(data); }
            catch (FormatException) { return null; }
        }

        private static async Task<string> ErrorOf(Task<ApiResponse> request)
        {
            ApiResponse r = await request.ConfigureAwait(true);
            return r.IsSuccess ? null : r.Error;
        }

        public static IReadOnlyList<FeedPostInfo> ParsePosts(IList<object> items)
        {
            var posts = new List<FeedPostInfo>();
            if (items == null) return posts;
            foreach (IDictionary<string, object> d in items.OfType<IDictionary<string, object>>())
            {
                IDictionary<string, object> author = JsonHelper.Obj(d, "author");
                IDictionary<string, object> poi = JsonHelper.Obj(d, "poi");
                var route = new List<FeedPlaceInfo>();
                foreach (IDictionary<string, object> stop in (JsonHelper.List(d, "route") ?? new List<object>()).OfType<IDictionary<string, object>>())
                    route.Add(new FeedPlaceInfo { Id = JsonHelper.Str(stop, "poiId"), Name = JsonHelper.Str(stop, "name") });
                var media = new List<long>();
                foreach (IDictionary<string, object> m in (JsonHelper.List(d, "media") ?? new List<object>()).OfType<IDictionary<string, object>>())
                    media.Add(JsonHelper.Long(m, "id"));
                posts.Add(new FeedPostInfo
                {
                    Id = JsonHelper.Long(d, "id"), CreatedAt = ParseTime(JsonHelper.Str(d, "createdAt")),
                    AuthorId = JsonHelper.Long(author, "id"), AuthorName = JsonHelper.Str(author, "name") ?? "Người dùng",
                    Body = JsonHelper.Str(d, "body") ?? "",
                    Poi = poi == null ? null : new FeedPlaceInfo { Id = JsonHelper.Str(poi, "id"), Name = JsonHelper.Str(poi, "name") },
                    Route = route, MediaIds = media,
                    LikeCount = (int)JsonHelper.Long(d, "likeCount"), CommentCount = (int)JsonHelper.Long(d, "commentCount"),
                    LikedByMe = JsonHelper.Bool(d, "likedByMe"), Mine = JsonHelper.Bool(d, "mine"), Status = JsonHelper.Str(d, "status") ?? "visible",
                });
            }
            return posts;
        }

        private static DateTimeOffset ParseTime(string text)
        {
            DateTimeOffset at;
            return DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out at) ? at : DateTimeOffset.Now;
        }
    }
}
