using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Hue_travel.Account;
using Hue_travel.Services.Online;
using Hue_travel.Theme;

namespace Hue_travel.Admin
{
    /// <summary>"Người dùng": find an account, change its role, disable or enable it, give it a new temporary password, delete it, or make a new one.</summary>
    internal sealed class UsersSection : AdminSection
    {
        private static readonly string[] Roles = { "user", "owner", "admin" };
        private static readonly string[] RoleFilters = { null, "user", "owner", "admin" };
        private const string Nothing = "Chọn một tài khoản bên trái để xem và xử lý.";

        private readonly long selfId;
        private readonly InputField search = new InputField { IconName = "search", PlaceholderText = "Tìm theo tên đăng nhập hoặc tên hiển thị…" };
        private readonly SegmentTabs roleTabs = new SegmentTabs();
        private readonly ItemList list = new ItemList { Selectable = true, SubtitleLines = 1, EmptyText = "Không có tài khoản nào khớp." };
        private readonly Timer typing = new Timer { Interval = 350 };
        private long chosenId;

        public UsersSection(ApiClient api, long selfId) : base(api)
        {
            this.selfId = selfId;
            ListPane.Caption("Người dùng");
            IconButton create = AccountKit.Secondary("Tạo tài khoản", "person_add");
            ListPane.Button(create, 42);
            ListPane.Add(search, 40, 10);
            roleTabs.SetTabs("Tất cả", "Người dùng", "Chủ quản", "Quản trị");
            ListPane.Add(roleTabs, 38, 10);
            ListPane.List(list);

            create.Click += async (s, e) => await CreateAsync();
            typing.Tick += async (s, e) => { typing.Stop(); await LoadAsync(); };
            search.TextChanged += (s, e) => { typing.Stop(); typing.Start(); };
            roleTabs.SelectedChanged += async (s, e) => await LoadAsync();
            list.ItemClicked += (s, item) => Show((IDictionary<string, object>)item.Data);
            ShowNothing(Nothing);
        }

        public override async Task LoadAsync()
        {
            string path = "/v1/admin/users?q=" + Uri.EscapeDataString(search.Text.Trim());
            string role = RoleFilters[Math.Max(0, roleTabs.SelectedIndex)];
            if (role != null) path += "&role=" + role;
            ApiResponse r = await Api.GetAsync(path);
            if (!Check(r)) return;
            var items = new List<ListItem>();
            foreach (IDictionary<string, object> d in Rows(r))
            {
                string status = Str(d, "status");
                int owned = (int)JsonHelper.Long(d, "ownedPlaces"), pending = (int)JsonHelper.Long(d, "pendingClaims");
                string extra = owned > 0 ? " · " + owned + " địa điểm" : pending > 0 ? " · chờ duyệt " + pending + " địa điểm" : "";
                items.Add(new ListItem
                {
                    Title = Str(d, "displayName"), Subtitle = "@" + Str(d, "username") + " · " + AccountKit.RoleName(Str(d, "role")) + extra, Data = d,
                    Pill = status == "locked" ? "Vô hiệu hóa" : status == "must_change_password" ? "Chờ đổi MK" : "Hoạt động",
                    Tone = status == "locked" ? PillTone.Bad : status == "must_change_password" ? PillTone.Warn : PillTone.Good,
                });
            }
            list.SetItems(items);
            ListPane.Relayout();
            int keep = items.FindIndex(i => JsonHelper.Long((IDictionary<string, object>)i.Data, "id") == chosenId);
            if (keep >= 0) { list.SelectedIndex = keep; Show((IDictionary<string, object>)items[keep].Data); }
            else if (chosenId != 0) { chosenId = 0; ShowNothing(Nothing); }
        }

        private void Show(IDictionary<string, object> d)
        {
            chosenId = JsonHelper.Long(d, "id");
            string status = Str(d, "status"), role = Str(d, "role");
            bool self = chosenId == selfId, locked = status == "locked";
            DetailPane.Clear();
            DetailPane.Caption(Str(d, "displayName"));
            DetailPane.Hint("@" + Str(d, "username") + " · " + AccountKit.RoleName(role) + " · " + AccountKit.StatusName(status));
            DetailPane.Hint("Tạo ngày " + AccountKit.Day(Str(d, "createdAt")) + "\nĐăng nhập gần nhất: " + (string.IsNullOrEmpty(Str(d, "lastLoginAt")) ? "chưa lần nào" : AccountKit.Day(Str(d, "lastLoginAt")))
                + "\nĐịa điểm quản lý: " + JsonHelper.Long(d, "ownedPlaces") + (JsonHelper.Long(d, "pendingClaims") > 0 ? " (còn " + JsonHelper.Long(d, "pendingClaims") + " yêu cầu chờ duyệt)" : ""));
            DetailPane.Divider();
            if (self)
            {
                DetailPane.Hint("Đây là tài khoản của bạn. Bạn không tự đổi vai trò, vô hiệu hóa hay xóa chính mình; hãy nhờ một quản trị viên khác.");
                DetailPane.Relayout();
                return;
            }

            IconButton changeRole = AccountKit.Secondary("Đổi vai trò", "verified_user");
            IconButton toggle = locked ? AccountKit.Secondary("Kích hoạt lại", "check") : AccountKit.Danger("Vô hiệu hóa", "lock");
            IconButton reset = AccountKit.Secondary("Đặt lại mật khẩu", "lock_reset");
            IconButton delete = AccountKit.Danger("Xóa tài khoản", "close");
            DetailPane.Buttons(changeRole, toggle);
            DetailPane.Buttons(reset, delete);
            delete.Enabled = locked;
            DetailPane.Hint(locked ? "Tài khoản đã vô hiệu hóa: không đăng nhập được. Xóa sẽ bỏ luôn yêu thích, lộ trình và đánh giá của người này (nhật ký vẫn giữ)."
                : "Vô hiệu hóa chặn đăng nhập ngay nhưng giữ nguyên dữ liệu; muốn xóa hẳn phải vô hiệu hóa trước.");
            changeRole.Click += async (s, e) => await ChangeRoleAsync(d);
            toggle.Click += async (s, e) => await SetStatusAsync(d, locked ? "active" : "locked");
            reset.Click += async (s, e) => await ResetAsync(d);
            delete.Click += async (s, e) => await DeleteAsync(d);
            DetailPane.Relayout();
        }

        private async Task CreateAsync()
        {
            string[] v = Dialogs.Ask(Window, "Tạo tài khoản", "Người này phải đổi mật khẩu ở lần đăng nhập đầu tiên.", "Tạo tài khoản",
                new DialogField("Tên đăng nhập", "", 1, false, "3 đến 40 ký tự: chữ, số, _ hoặc ."), new DialogField("Tên hiển thị"),
                new DialogField("Mật khẩu tạm (tối thiểu 8 ký tự)", "", 1, true));
            if (v == null) return;
            int role = Dialogs.Pick(Window, "Vai trò của tài khoản mới", RoleItems(), "Tạo");
            if (role < 0) return;
            var body = new Dictionary<string, object> { { "username", v[0].Trim() }, { "displayName", v[1].Trim() }, { "password", v[2] }, { "role", Roles[role] } };
            if (Check(await Api.PostAsync("/v1/admin/users", body), "Đã tạo tài khoản")) { RaiseChanged(); await LoadAsync(); }
        }

        private static List<ListItem> RoleItems() => new List<ListItem>
        {
            new ListItem { Title = "Người dùng", Subtitle = "Xem bản đồ, lưu yêu thích, viết đánh giá", Icon = "account_circle" },
            new ListItem { Title = "Chủ quản", Subtitle = "Quản lý địa điểm đã được duyệt", Icon = "pin_drop" },
            new ListItem { Title = "Quản trị viên", Subtitle = "Toàn quyền quản trị", Icon = "verified_user" },
        };

        private async Task ChangeRoleAsync(IDictionary<string, object> d)
        {
            int index = Dialogs.Pick(Window, "Vai trò của " + Str(d, "displayName"), RoleItems(), "Đổi vai trò");
            if (index < 0 || Roles[index] == Str(d, "role")) return;
            if (Roles[index] == "admin" && !Dialogs.Confirm(Window, "Cấp quyền quản trị", "Quản trị viên xem và sửa được mọi dữ liệu. Cấp quyền này cho \"" + Str(d, "displayName") + "\"?", "Cấp quyền", true)) return;
            if (Check(await Api.PutAsync("/v1/admin/users/" + chosenId, new Dictionary<string, object> { { "role", Roles[index] } }), "Đã đổi vai trò")) { RaiseChanged(); await LoadAsync(); }
        }

        private async Task SetStatusAsync(IDictionary<string, object> d, string status)
        {
            if (status == "locked" && !Dialogs.Confirm(Window, "Vô hiệu hóa tài khoản", "\"" + Str(d, "displayName") + "\" sẽ bị đăng xuất và không đăng nhập được nữa cho đến khi bạn kích hoạt lại.", "Vô hiệu hóa", true)) return;
            if (Check(await Api.PutAsync("/v1/admin/users/" + chosenId, new Dictionary<string, object> { { "status", status } }), status == "locked" ? "Đã vô hiệu hóa tài khoản" : "Đã kích hoạt lại tài khoản")) { RaiseChanged(); await LoadAsync(); }
        }

        private async Task ResetAsync(IDictionary<string, object> d)
        {
            if (!Dialogs.Confirm(Window, "Đặt lại mật khẩu", "Tạo mật khẩu tạm mới cho \"" + Str(d, "displayName") + "\"? Mật khẩu cũ không dùng được nữa.", "Đặt lại")) return;
            ApiResponse r = await Api.PostAsync("/v1/admin/users/" + chosenId + "/reset-password", null);
            if (!Check(r)) return;
            string temporary = JsonHelper.Str(r.Object, "temporaryPassword") ?? "(máy chủ không trả về mật khẩu, hãy đặt lại lần nữa)";
            // Shown, not copied: the clipboard history of Windows would keep it.
            Dialogs.Message(Window, "Mật khẩu tạm", "Mật khẩu tạm của " + Str(d, "username") + ":\n\n" + temporary + "\n\nChỉ hiện một lần. Người này phải đổi mật khẩu ngay khi đăng nhập.");
            RaiseChanged();
        }

        private async Task DeleteAsync(IDictionary<string, object> d)
        {
            if (!Dialogs.Confirm(Window, "Xóa tài khoản", "Xóa hẳn tài khoản \"" + Str(d, "username") + "\"? Không hoàn tác được.", "Xóa", true)) return;
            if (Check(await Api.DeleteAsync("/v1/admin/users/" + chosenId), "Đã xóa tài khoản")) { chosenId = 0; RaiseChanged(); await LoadAsync(); ShowNothing(Nothing); }
        }
    }
}
