using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Hue_travel.Models;
using Hue_travel.Services.Online;
using Hue_travel.Theme;

namespace Hue_travel.Account
{
    /// <summary>
    /// The owner's screen: the places they manage and the status of their requests. Edits are sent as requests that an
    /// admin approves; nothing here changes the data directly.
    /// </summary>
    internal sealed class OwnerTab : AccountTab
    {
        private readonly ListView places = AccountUi.List(AccountUi.Col("Địa điểm của tôi", 260), AccountUi.Col("Nhóm", 110));
        private readonly ListView claims = AccountUi.List(AccountUi.Col("Yêu cầu nhận địa điểm", 260), AccountUi.Col("Trạng thái", 110));
        private readonly List<Poi> owned = new List<Poi>();

        public OwnerTab(ApiClient api) : base(api)
        {
            IconButton edit = AccountUi.Button("Gửi chỉnh sửa", true, 130);
            IconButton announce = AccountUi.Button("Đăng thông báo", false, 130, "campaign");
            IconButton reply = AccountUi.Button("Trả lời đánh giá", false, 140);
            IconButton stats = AccountUi.Button("Thống kê", false, 90);
            edit.Click += async (s, e) => await EditAsync();
            announce.Click += async (s, e) => await AnnounceAsync();
            reply.Click += async (s, e) => await ReplyAsync();
            stats.Click += async (s, e) => await StatsAsync();

            var split = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
            split.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
            split.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
            split.Controls.Add(places, 0, 0);
            split.Controls.Add(claims, 0, 1);
            Controls.Add(split);
            Controls.Add(ButtonRow(edit, announce, reply, stats));
        }

        public override async Task LoadAsync()
        {
            ApiResponse mine = await Api.GetAsync("/v1/owner/places");
            if (Check(mine))
            {
                owned.Clear();
                places.Items.Clear();
                foreach (IDictionary<string, object> d in (JsonHelper.AsList(mine.Json) ?? new List<object>()).OfType<IDictionary<string, object>>())
                {
                    Poi p = PoiJson.ToPoi(d);
                    owned.Add(p);
                    places.Items.Add(new ListViewItem(new[] { p.Name, AccountUi.GroupName(p.Category) }));
                }
            }
            ApiResponse requested = await Api.GetAsync("/v1/owner/claims");
            if (!Check(requested)) return;
            claims.Items.Clear();
            foreach (IDictionary<string, object> d in (JsonHelper.AsList(requested.Json) ?? new List<object>()).OfType<IDictionary<string, object>>())
                claims.Items.Add(new ListViewItem(new[] { JsonHelper.Str(d, "poiName"), AccountUi.ClaimStatus(JsonHelper.Str(d, "status")) }));
        }

        private Poi Selected()
        {
            if (places.SelectedIndices.Count > 0) return owned[places.SelectedIndices[0]];
            Notify(owned.Count == 0 ? "Bạn chưa quản lý địa điểm nào. Mở một địa điểm và bấm \"Tôi là chủ quản\"" : "Hãy chọn một địa điểm trong danh sách", ToastKind.Info);
            return null;
        }

        private async Task EditAsync()
        {
            Poi p = Selected();
            if (p == null) return;
            string[] v = Dialogs.Ask(FindForm(), "Gửi chỉnh sửa: " + p.Name,
                new DialogField("Địa chỉ:", p.Address), new DialogField("Giờ mở cửa:", p.OpeningHours), new DialogField("Giá vé / giá dịch vụ:", p.TicketPrice),
                new DialogField("Số điện thoại:", p.Phone), new DialogField("Website (http:// hoặc https://):", p.Website), new DialogField("Giới thiệu:", p.Summary, 3));
            if (v == null) return;

            var changes = new Dictionary<string, object>();
            AddIfChanged(changes, "address", v[0], p.Address);
            AddIfChanged(changes, "openingHours", v[1], p.OpeningHours);
            AddIfChanged(changes, "priceText", v[2], p.TicketPrice);
            AddIfChanged(changes, "phone", v[3], p.Phone);
            AddIfChanged(changes, "website", v[4], p.Website);
            AddIfChanged(changes, "summary", v[5], p.Summary);
            if (changes.Count == 0) { Notify("Chưa có gì thay đổi", ToastKind.Info); return; }
            Check(await Api.PostAsync("/v1/owner/places/" + Uri.EscapeDataString(p.Id) + "/edit-requests", new Dictionary<string, object> { { "changes", changes } }),
                "Đã gửi yêu cầu chỉnh sửa. Quản trị viên sẽ duyệt");
        }

        private static void AddIfChanged(Dictionary<string, object> changes, string key, string value, string original)
        {
            if ((value ?? "").Trim() != (original ?? "").Trim()) changes[key] = (value ?? "").Trim();
        }

        private async Task AnnounceAsync()
        {
            Poi p = Selected();
            if (p == null) return;
            string[] v = Dialogs.Ask(FindForm(), "Đăng thông báo: " + p.Name, new DialogField("Tiêu đề:"), new DialogField("Nội dung:", "", 4));
            if (v == null) return;
            Check(await Api.PostAsync("/v1/owner/places/" + Uri.EscapeDataString(p.Id) + "/announcements", new Dictionary<string, object> { { "title", v[0] }, { "body", v[1] } }),
                "Đã đăng thông báo");
        }

        private async Task ReplyAsync()
        {
            Poi p = Selected();
            if (p == null) return;
            ApiResponse r = await Api.GetAsync("/v1/pois/" + Uri.EscapeDataString(p.Id) + "/reviews");
            if (!Check(r)) return;
            List<IDictionary<string, object>> reviews = (JsonHelper.List(r.Object, "items") ?? new List<object>()).OfType<IDictionary<string, object>>().ToList();
            if (reviews.Count == 0) { Notify("Địa điểm này chưa có đánh giá", ToastKind.Info); return; }

            int index = Dialogs.Pick(FindForm(), "Chọn đánh giá để trả lời", new[] { "Người đánh giá", "Điểm", "Nhận xét" },
                reviews.Select(d => new[] { JsonHelper.Str(d, "author"), JsonHelper.Str(d, "rating") + " sao", JsonHelper.Str(d, "comment") ?? "" }).ToList());
            if (index < 0) return;
            string[] v = Dialogs.Ask(FindForm(), "Phản hồi", new DialogField("Nội dung phản hồi:", JsonHelper.Str(reviews[index], "ownerReply") ?? "", 4));
            if (v == null) return;
            Check(await Api.PostAsync("/v1/owner/places/" + Uri.EscapeDataString(p.Id) + "/reviews/" + JsonHelper.Long(reviews[index], "id") + "/reply",
                new Dictionary<string, object> { { "reply", v[0] } }), "Đã gửi phản hồi");
        }

        private async Task StatsAsync()
        {
            Poi p = Selected();
            if (p == null) return;
            ApiResponse r = await Api.GetAsync("/v1/owner/places/" + Uri.EscapeDataString(p.Id) + "/stats?days=30");
            if (!Check(r)) return;
            IList<object> days = JsonHelper.List(r.Object, "days") ?? new List<object>();
            double views = days.OfType<IDictionary<string, object>>().Sum(d => JsonHelper.Dbl(d, "views"));
            double adds = days.OfType<IDictionary<string, object>>().Sum(d => JsonHelper.Dbl(d, "routeAdds"));
            MessageBox.Show(FindForm(),
                p.Name + "\n\n30 ngày qua:\n  Lượt xem: " + views + "\n  Lượt đưa vào lộ trình: " + adds + "\n  Điểm đánh giá trung bình: " + JsonHelper.Dbl(r.Object, "averageRating") + " (" + JsonHelper.Long(r.Object, "reviewCount") + " đánh giá)",
                "Thống kê địa điểm", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
