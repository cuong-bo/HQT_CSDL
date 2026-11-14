using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hue_travel.Account;
using Hue_travel.Services.Online;
using Hue_travel.Theme;

namespace Hue_travel.Admin
{
    /// <summary>"Kiểm duyệt": reviews that can be hidden when they break the rules, and reports of wrong information to settle.</summary>
    internal sealed class ModerationSection : AdminSection
    {
        private const string Nothing = "Chọn một mục bên trái để xem chi tiết và xử lý.";
        private static readonly Dictionary<string, string> Kinds = new Dictionary<string, string>
        {
            { "wrong_info", "Sai thông tin" }, { "closed", "Đã đóng cửa" }, { "wrong_location", "Sai vị trí" }, { "other", "Khác" },
        };

        private readonly SegmentTabs tabs = new SegmentTabs();
        private readonly ItemList list = new ItemList { Selectable = true, SubtitleLines = 3, EmptyText = "Không có gì cần xử lý." };
        private static readonly Dictionary<string, string> FeedKinds = new Dictionary<string, string>
        {
            { "spam", "Spam, quảng cáo" }, { "inappropriate", "Không phù hợp" }, { "wrong_info", "Thông tin sai" }, { "privacy", "Lộ thông tin cá nhân" }, { "other", "Khác" },
        };

        private readonly List<ListItem>[] items = { new List<ListItem>(), new List<ListItem>(), new List<ListItem>(), new List<ListItem>() };

        public ModerationSection(ApiClient api) : base(api)
        {
            ListPane.Caption("Kiểm duyệt");
            tabs.SetTabs("Đánh giá", "Báo sai thông tin", "Bài chờ duyệt", "Báo cáo bài");
            ListPane.Add(tabs, 38, 10);
            ListPane.List(list);
            tabs.SelectedChanged += (s, e) => ShowList();
            list.ItemClicked += (s, item) => Show((IDictionary<string, object>)item.Data);
            ShowNothing(Nothing);
        }

        public override async Task LoadAsync()
        {
            ApiResponse reviews = await Api.GetAsync("/v1/admin/reviews");
            ApiResponse reports = await Api.GetAsync("/v1/admin/reports");
            if (!Check(reviews) || !Check(reports)) return;
            items[0] = Rows(reviews).Select(d => new ListItem
            {
                Title = Str(d, "author"), Subtitle = (string.IsNullOrWhiteSpace(Str(d, "comment")) ? "(không có nhận xét)" : Str(d, "comment")) + "\n" + AccountKit.Day(Str(d, "createdAt")),
                Pill = JsonHelper.Long(d, "rating") + " sao", Tone = PillTone.Warn, Icon = "star", Data = d,
            }).ToList();
            items[1] = Rows(reports).Select(d => new ListItem
            {
                Title = Str(d, "poiName"), Subtitle = (Kinds.ContainsKey(Str(d, "kind")) ? Kinds[Str(d, "kind")] : Str(d, "kind")) + " · @" + Str(d, "reporter") + "\n" + (string.IsNullOrWhiteSpace(Str(d, "content")) ? "(không mô tả)" : Str(d, "content")),
                Pill = "Đang mở", Tone = PillTone.Warn, Icon = "campaign", Data = d,
            }).ToList();
            tabs.SetCount(1, items[1].Count);

            // "Khoảnh khắc" may be switched off on the server (503): then these two tabs are simply empty, and the others still work.
            ApiResponse pending = await Api.GetAsync("/v1/admin/feed/pending");
            ApiResponse feedReports = await Api.GetAsync("/v1/admin/feed/reports");
            items[2] = pending.IsSuccess ? (pending.Json as IList<object> ?? new List<object>()).OfType<IDictionary<string, object>>().Select(d => new ListItem
            {
                Title = JsonHelper.Str(JsonHelper.Obj(d, "author"), "name"),
                Subtitle = (string.IsNullOrWhiteSpace(Str(d, "body")) ? "(chỉ có ảnh hoặc lộ trình)" : Str(d, "body")) + "\n" + AccountKit.Day(Str(d, "createdAt")),
                Pill = "Chờ duyệt", Tone = PillTone.Warn, Icon = "pin_drop", Data = d,
            }).ToList() : new List<ListItem>();
            items[3] = feedReports.IsSuccess ? (feedReports.Json as IList<object> ?? new List<object>()).OfType<IDictionary<string, object>>().Select(d => new ListItem
            {
                Title = (d.ContainsKey("commentId") && d["commentId"] != null ? "Bình luận của " : "Bài của ") + Str(d, "author"),
                Subtitle = (FeedKinds.ContainsKey(Str(d, "kind")) ? FeedKinds[Str(d, "kind")] : Str(d, "kind")) + " · @" + Str(d, "reporter") + "\n" + Str(d, "text"),
                Pill = "Đang mở", Tone = PillTone.Warn, Icon = "campaign", Data = d,
            }).ToList() : new List<ListItem>();
            tabs.SetCount(2, items[2].Count);
            tabs.SetCount(3, items[3].Count);
            ShowList();
        }

        private void ShowList()
        {
            list.SetItems(items[Math.Max(0, tabs.SelectedIndex)]);
            ListPane.Relayout();
            ShowNothing(Nothing);
        }

        private void Show(IDictionary<string, object> d)
        {
            DetailPane.Clear();
            if (tabs.SelectedIndex == 2) { ShowPendingPost(d); return; }
            if (tabs.SelectedIndex == 3) { ShowFeedReport(d); return; }
            if (tabs.SelectedIndex == 0)
            {
                DetailPane.Caption("Đánh giá của " + Str(d, "author"));
                DetailPane.Hint(JsonHelper.Long(d, "rating") + " sao · " + AccountKit.Day(Str(d, "createdAt")) + "\nĐịa điểm: " + PlaceName(Str(d, "poiId")));
                DetailPane.Hint(string.IsNullOrWhiteSpace(Str(d, "comment")) ? "(không có nhận xét)" : "\"" + Str(d, "comment") + "\"");
                DetailPane.Divider();
                IconButton hide = AccountKit.Danger("Ẩn đánh giá này", "visibility_off");
                DetailPane.Button(hide, 44);
                DetailPane.Hint("Đánh giá bị ẩn không còn hiện cho mọi người và không tính vào điểm trung bình. Chỉ ẩn khi đánh giá xúc phạm, quảng cáo hoặc sai sự thật.");
                hide.Click += async (s, e) => await HideAsync(d);
            }
            else
            {
                DetailPane.Caption(Str(d, "poiName"));
                DetailPane.Hint((Kinds.ContainsKey(Str(d, "kind")) ? Kinds[Str(d, "kind")] : Str(d, "kind")) + " · báo bởi @" + Str(d, "reporter") + "\n" + AccountKit.Day(Str(d, "createdAt")));
                DetailPane.Hint(string.IsNullOrWhiteSpace(Str(d, "content")) ? "(không có mô tả)" : "\"" + Str(d, "content") + "\"");
                DetailPane.Divider();
                IconButton resolve = AccountKit.Primary("Đã xử lý", "check");
                DetailPane.Button(resolve, 48);
                DetailPane.Hint("Sửa thông tin địa điểm ở mục Địa điểm (nếu báo cáo đúng), rồi bấm \"Đã xử lý\" để đóng báo cáo.");
                resolve.Click += async (s, e) => await ResolveAsync(d);
            }
            DetailPane.Relayout();
        }

        private void ShowPendingPost(IDictionary<string, object> d)
        {
            IList<object> media = JsonHelper.List(d, "media");
            IList<object> route = JsonHelper.List(d, "route");
            IDictionary<string, object> poi = JsonHelper.Obj(d, "poi");
            DetailPane.Caption("Bài của " + JsonHelper.Str(JsonHelper.Obj(d, "author"), "name"));
            DetailPane.Hint(AccountKit.Day(Str(d, "createdAt")) + " · " + (media == null ? 0 : media.Count) + " ảnh"
                            + (poi == null ? "" : "\nĐịa điểm: " + JsonHelper.Str(poi, "name"))
                            + (route == null || route.Count == 0 ? "" : "\nCó đính kèm lộ trình"));
            DetailPane.Hint(string.IsNullOrWhiteSpace(Str(d, "body")) ? "(không có chữ)" : "\"" + Str(d, "body") + "\"");
            DetailPane.Divider();
            IconButton approve = AccountKit.Primary("Duyệt và đăng", "check");
            IconButton reject = AccountKit.Danger("Từ chối", "close");
            DetailPane.Button(approve, 48);
            DetailPane.Button(reject, 44);
            DetailPane.Hint("Tài khoản có đủ bài được duyệt sẽ tự đăng được mà không cần chờ.");
            approve.Click += async (s, e) => await DecideAsync(d, true);
            reject.Click += async (s, e) => await DecideAsync(d, false);
            DetailPane.Relayout();
        }

        private void ShowFeedReport(IDictionary<string, object> d)
        {
            bool comment = d.ContainsKey("commentId") && d["commentId"] != null;
            DetailPane.Caption((comment ? "Bình luận của " : "Bài của ") + Str(d, "author"));
            DetailPane.Hint((FeedKinds.ContainsKey(Str(d, "kind")) ? FeedKinds[Str(d, "kind")] : Str(d, "kind")) + " · báo bởi @" + Str(d, "reporter") + "\n" + AccountKit.Day(Str(d, "createdAt")));
            DetailPane.Hint("\"" + Str(d, "text") + "\"");
            if (!string.IsNullOrWhiteSpace(Str(d, "content"))) DetailPane.Hint("Người báo cáo viết: " + Str(d, "content"));
            DetailPane.Divider();
            IconButton dismiss = AccountKit.Secondary("Bỏ qua (báo cáo sai)", "check");
            IconButton hide = AccountKit.Secondary("Ẩn", "visibility_off");
            IconButton remove = AccountKit.Danger("Gỡ hẳn", "close");
            DetailPane.Button(dismiss, 44);
            DetailPane.Button(hide, 44);
            DetailPane.Button(remove, 44);
            DetailPane.Hint("Nội dung bị ẩn không còn hiện cho mọi người nhưng còn trong hệ thống; gỡ hẳn thì không khôi phục được. Mọi báo cáo về cùng một nội dung đóng cùng lúc.");
            dismiss.Click += async (s, e) => await ResolveFeedAsync(d, "dismiss");
            hide.Click += async (s, e) => await ResolveFeedAsync(d, "hide");
            remove.Click += async (s, e) => await ResolveFeedAsync(d, "remove");
            DetailPane.Relayout();
        }

        private async Task DecideAsync(IDictionary<string, object> d, bool approve)
        {
            if (!approve && !Dialogs.Confirm(Window, "Từ chối bài viết", "Từ chối và gỡ bài viết này?", "Từ chối", true)) return;
            var body = new Dictionary<string, object> { { "approve", approve } };
            if (Check(await Api.PostAsync("/v1/admin/posts/" + JsonHelper.Long(d, "id") + "/decision", body), approve ? "Đã duyệt bài" : "Đã từ chối bài")) { RaiseChanged(); await LoadAsync(); }
        }

        private async Task ResolveFeedAsync(IDictionary<string, object> d, string action)
        {
            if (action == "remove" && !Dialogs.Confirm(Window, "Gỡ hẳn", "Gỡ hẳn nội dung này? Không khôi phục được.", "Gỡ hẳn", true)) return;
            var body = new Dictionary<string, object> { { "action", action } };
            if (Check(await Api.PostAsync("/v1/admin/feed/reports/" + JsonHelper.Long(d, "id") + "/resolve", body), "Đã xử lý báo cáo")) { RaiseChanged(); await LoadAsync(); }
        }

        private static string PlaceName(string id)
        {
            var poi = Hue_travel.Services.Data.PoiCatalog.All.FirstOrDefault(p => p.Id == id);
            return poi == null ? id : poi.Name;
        }

        private async Task HideAsync(IDictionary<string, object> d)
        {
            if (!Dialogs.Confirm(Window, "Ẩn đánh giá", "Ẩn đánh giá của \"" + Str(d, "author") + "\"?", "Ẩn", true)) return;
            if (Check(await Api.PutAsync("/v1/admin/reviews/" + JsonHelper.Long(d, "id"), new Dictionary<string, object> { { "status", "hidden" } }), "Đã ẩn đánh giá")) { RaiseChanged(); await LoadAsync(); }
        }

        private async Task ResolveAsync(IDictionary<string, object> d)
        {
            if (Check(await Api.PostAsync("/v1/admin/reports/" + JsonHelper.Long(d, "id") + "/resolve", null), "Đã đóng báo cáo")) { RaiseChanged(); await LoadAsync(); }
        }
    }
}
