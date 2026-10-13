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
        private readonly List<ListItem>[] items = { new List<ListItem>(), new List<ListItem>() };

        public ModerationSection(ApiClient api) : base(api)
        {
            ListPane.Caption("Kiểm duyệt");
            tabs.SetTabs("Đánh giá", "Báo sai thông tin");
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
