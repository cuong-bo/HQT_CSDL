using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hue_travel.Account;
using Hue_travel.Services.Online;
using Hue_travel.Theme;

namespace Hue_travel.Admin
{
    /// <summary>"Nhật ký": who changed what and when. Nothing here can be edited or deleted; a row shows the old and the new values.</summary>
    internal sealed class AuditSection : AdminSection
    {
        private const string Nothing = "Chọn một dòng bên trái để xem giá trị cũ và mới.";
        private const int PageSize = 50;

        private static readonly Dictionary<string, string> Actions = new Dictionary<string, string>
        {
            { "user.create", "Tạo tài khoản" }, { "user.update", "Đổi vai trò / trạng thái tài khoản" }, { "user.reset_password", "Đặt lại mật khẩu" }, { "user.delete", "Xóa tài khoản" },
            { "claim.create", "Xin quản lý địa điểm" }, { "claim.approve", "Duyệt chủ quản" }, { "claim.reject", "Từ chối chủ quản" },
            { "edit_request.create", "Gửi yêu cầu chỉnh sửa" }, { "edit_request.approve", "Duyệt chỉnh sửa" }, { "edit_request.reject", "Từ chối chỉnh sửa" },
            { "photo.upload", "Tải ảnh lên" }, { "photo.approve", "Duyệt ảnh" }, { "photo.reject", "Từ chối ảnh" }, { "photo.delete", "Xóa ảnh" },
            { "poi.create", "Thêm địa điểm" }, { "poi.update", "Sửa địa điểm" }, { "poi.delete", "Xóa địa điểm" }, { "poi.import", "Nhập địa điểm từ tệp" },
            { "review.moderate", "Ẩn đánh giá" }, { "review.reply", "Chủ quản trả lời đánh giá" }, { "report.resolve", "Đóng báo cáo" }, { "announcement.create", "Đăng thông báo" },
        };

        private readonly ItemList list = new ItemList { Selectable = true, SubtitleLines = 1, EmptyText = "Chưa có dòng nhật ký nào." };
        private readonly IconButton more = AccountKit.Secondary("Xem thêm", "expand_more");
        private readonly List<ListItem> shown = new List<ListItem>();
        private int page = 1;

        public AuditSection(ApiClient api) : base(api)
        {
            ListPane.Caption("Nhật ký thay đổi");
            ListPane.Hint("Mọi thay đổi dữ liệu dùng chung đều được ghi lại và không sửa hay xóa được.");
            ListPane.List(list);
            ListPane.Button(more, 42);
            more.Click += async (s, e) => { page++; await FetchAsync(); };
            list.ItemClicked += (s, item) => Show((IDictionary<string, object>)item.Data);
            ShowNothing(Nothing);
        }

        public override async Task LoadAsync()
        {
            page = 1;
            shown.Clear();
            await FetchAsync();
        }

        private async Task FetchAsync()
        {
            ApiResponse r = await Api.GetAsync("/v1/admin/audit?page=" + page);
            if (!Check(r)) return;
            List<IDictionary<string, object>> rows = Rows(r);
            foreach (IDictionary<string, object> d in rows)
            {
                string action = Str(d, "action");
                shown.Add(new ListItem
                {
                    Title = Actions.ContainsKey(action) ? Actions[action] : action, Icon = "check_box", Data = d,
                    Subtitle = AccountKit.Day(Str(d, "at")) + " · " + (string.IsNullOrEmpty(Str(d, "user")) ? "hệ thống" : "@" + Str(d, "user")) + (string.IsNullOrEmpty(Str(d, "entityId")) ? "" : " · " + Str(d, "entityId")),
                });
            }
            list.SetItems(shown, true);
            more.Visible = rows.Count >= PageSize;
            ListPane.Relayout();
        }

        private void Show(IDictionary<string, object> d)
        {
            string action = Str(d, "action");
            DetailPane.Clear();
            DetailPane.Caption(Actions.ContainsKey(action) ? Actions[action] : action);
            DetailPane.Hint(AccountKit.Day(Str(d, "at")) + "\nNgười làm: " + (string.IsNullOrEmpty(Str(d, "user")) ? "hệ thống" : "@" + Str(d, "user")) + "\nĐối tượng: " + Str(d, "entity") + " " + Str(d, "entityId"));
            object before = d.ContainsKey("oldValue") ? d["oldValue"] : null, after = d.ContainsKey("newValue") ? d["newValue"] : null;
            if (before != null) { DetailPane.Divider(); DetailPane.Caption("Trước", 2); DetailPane.Hint(Pretty(before)); }
            if (after != null) { DetailPane.Divider(); DetailPane.Caption("Sau", 2); DetailPane.Hint(Pretty(after)); }
            DetailPane.Relayout();
        }

        // A JSON value as short readable lines ("key: value").
        private static string Pretty(object value)
        {
            var dictionary = value as IDictionary<string, object>;
            if (dictionary == null) return Convert.ToString(value);
            return string.Join("\n", dictionary.Select(kv => kv.Key + ": " + (kv.Value is IDictionary<string, object> || (kv.Value is System.Collections.IEnumerable && !(kv.Value is string)) ? JsonHelper.Serialize(kv.Value) : Convert.ToString(kv.Value))));
        }
    }
}
