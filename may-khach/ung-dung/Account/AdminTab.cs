using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Hue_travel.Services.Online;
using Hue_travel.Theme;

namespace Hue_travel.Account
{
    /// <summary>The admin console: pending requests, reports, accounts and the audit trail, one section at a time.</summary>
    internal sealed class AdminTab : AccountTab
    {
        private sealed class Section
        {
            public string Title;
            public string Path;
            public string[] Columns;
            public Func<IDictionary<string, object>, string[]> Row;
            public Func<IDictionary<string, object>, string> Key;      // what the action calls need from the row (id, "userId/poiId", ...)
        }

        private readonly ComboBox sections = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList, Font = ThemeFonts.Medium(12) };
        private readonly ListView list = AccountUi.List(AccountUi.Col("", 100));
        private readonly Label summary = AccountUi.Caption("");
        private readonly IconButton approve = AccountUi.Button("Duyệt", true, 90, "check");
        private readonly IconButton reject = AccountUi.Button("Từ chối", false, 90, "close");
        private readonly IconButton lockUser = AccountUi.Button("Khóa / mở khóa", false, 130);
        private readonly IconButton reset = AccountUi.Button("Đặt lại mật khẩu", false, 140, "lock_reset");
        private readonly IconButton role = AccountUi.Button("Đổi vai trò", false, 110);
        private readonly IconButton create = AccountUi.Button("Tạo tài khoản", false, 120, "person_add");
        private readonly IconButton resolve = AccountUi.Button("Đã xử lý", true, 100, "check");
        private readonly IconButton hide = AccountUi.Button("Ẩn đánh giá", false, 110);
        private readonly List<Section> all;
        private List<IDictionary<string, object>> rows = new List<IDictionary<string, object>>();
        private Section loaded;   // the section the list on screen really belongs to

        public AdminTab(ApiClient api) : base(api)
        {
            all = new List<Section>
            {
                new Section { Title = "Yêu cầu nhận địa điểm", Path = "/v1/admin/claims", Columns = new[] { "Người gửi", "Địa điểm", "Ngày" },
                    Row = d => new[] { Str(d, "username"), Str(d, "poiName"), AccountUi.Day(Str(d, "createdAt")) }, Key = d => Str(d, "userId") + "/" + Uri.EscapeDataString(Str(d, "poiId")) },
                new Section { Title = "Yêu cầu chỉnh sửa", Path = "/v1/admin/edit-requests", Columns = new[] { "Người gửi", "Địa điểm", "Thay đổi" },
                    Row = d => new[] { Str(d, "requestedBy"), Str(d, "poiName"), ChangesText(d) }, Key = d => Str(d, "id") },
                new Section { Title = "Đánh giá", Path = "/v1/admin/reviews", Columns = new[] { "Người đánh giá", "Điểm", "Nhận xét" },
                    Row = d => new[] { Str(d, "author"), Str(d, "rating") + " sao", Str(d, "comment") }, Key = d => Str(d, "id") },
                new Section { Title = "Báo sai thông tin", Path = "/v1/admin/reports", Columns = new[] { "Địa điểm", "Loại", "Nội dung" },
                    Row = d => new[] { Str(d, "poiName"), Str(d, "kind"), Str(d, "content") }, Key = d => Str(d, "id") },
                new Section { Title = "Người dùng", Path = "/v1/admin/users", Columns = new[] { "Tên đăng nhập", "Vai trò", "Trạng thái" },
                    Row = d => new[] { Str(d, "username"), Str(d, "role"), Str(d, "status") }, Key = d => Str(d, "id") },
                new Section { Title = "Nhật ký thay đổi", Path = "/v1/admin/audit", Columns = new[] { "Thời gian", "Ai", "Việc" },
                    Row = d => new[] { AccountUi.Day(Str(d, "at")), Str(d, "user"), Str(d, "action") + " " + Str(d, "entity") + " " + Str(d, "entityId") }, Key = d => Str(d, "id") },
            };
            foreach (Section s in all) sections.Items.Add(s.Title);
            sections.SelectedIndexChanged += async (s, e) => await LoadSectionAsync();

            approve.Click += async (s, e) => await DecideAsync(true);
            reject.Click += async (s, e) => await DecideAsync(false);
            resolve.Click += async (s, e) => await ResolveAsync();
            hide.Click += async (s, e) => await HideReviewAsync();
            lockUser.Click += async (s, e) => await ToggleLockAsync();
            reset.Click += async (s, e) => await ResetPasswordAsync();
            role.Click += async (s, e) => await ChangeRoleAsync();
            create.Click += async (s, e) => await CreateUserAsync();

            summary.Dock = DockStyle.Top;
            Controls.Add(list);
            Controls.Add(ButtonRow(approve, reject, resolve, hide, lockUser, reset, role, create));
            Controls.Add(summary);
            Controls.Add(sections);
        }

        private static string Str(IDictionary<string, object> d, string key) => JsonHelper.Str(d, key) ?? "";

        private static string ChangesText(IDictionary<string, object> d)
        {
            IDictionary<string, object> c = JsonHelper.Obj(d, "changes");
            return c == null ? "" : string.Join("; ", c.Select(kv => kv.Key + " = " + kv.Value));
        }

        private Section Current => sections.SelectedIndex >= 0 ? all[sections.SelectedIndex] : null;

        public override async Task LoadAsync()
        {
            ApiResponse r = await Api.GetAsync("/v1/admin/stats");
            if (Check(r))
            {
                IDictionary<string, object> s = r.Object;
                summary.Text = "Chờ duyệt: " + JsonHelper.Long(s, "pendingClaims") + " nhận địa điểm, " + JsonHelper.Long(s, "pendingEditRequests") + " chỉnh sửa; "
                               + JsonHelper.Long(s, "openReports") + " báo sai; " + JsonHelper.Long(s, "users") + " tài khoản.";
            }
            if (sections.SelectedIndex < 0) sections.SelectedIndex = 0; else await LoadSectionAsync();
        }

        private async Task LoadSectionAsync()
        {
            Section section = Current;
            if (section == null) return;
            ApiResponse r = await Api.GetAsync(section.Path);
            if (!ReferenceEquals(section, Current)) return;   // another section was chosen while this one loaded
            if (!Check(r)) return;
            loaded = section;

            list.BeginUpdate();
            list.Columns.Clear();
            foreach (string c in section.Columns) list.Columns.Add(c, Math.Max(80, (list.Width - 24) / section.Columns.Length));
            list.Items.Clear();
            rows = (JsonHelper.AsList(r.Json) ?? new List<object>()).OfType<IDictionary<string, object>>().ToList();
            foreach (IDictionary<string, object> d in rows) list.Items.Add(new ListViewItem(section.Row(d)) { Tag = section.Key(d) });
            list.EndUpdate();

            bool decide = section.Path.EndsWith("claims") || section.Path.EndsWith("edit-requests");
            approve.Visible = reject.Visible = decide;
            resolve.Visible = section.Path.EndsWith("reports");
            hide.Visible = section.Path.EndsWith("reviews");
            lockUser.Visible = reset.Visible = role.Visible = create.Visible = section.Path.EndsWith("users");
        }

        private async Task DecideAsync(bool yes)
        {
            string key = SelectedTag(list);
            if (key == null || loaded == null) return;
            string path = loaded.Path.EndsWith("claims") ? "/v1/admin/claims/" + key + "/decision" : "/v1/admin/edit-requests/" + key + "/decision";
            string note = null;
            if (!yes)
            {
                string[] v = Dialogs.Ask(FindForm(), "Lý do từ chối (không bắt buộc)", new DialogField("Ghi chú:"));
                if (v == null) return;
                note = v[0];
            }
            if (Check(await Api.PostAsync(path, new Dictionary<string, object> { { "approve", yes }, { "note", note } }), yes ? "Đã duyệt" : "Đã từ chối")) await LoadAsync();
        }

        private async Task ResolveAsync()
        {
            string key = SelectedTag(list);
            if (key != null && Check(await Api.PostAsync("/v1/admin/reports/" + key + "/resolve", null), "Đã đánh dấu xử lý")) await LoadAsync();
        }

        private async Task HideReviewAsync()
        {
            string key = SelectedTag(list);
            if (key != null && Check(await Api.PutAsync("/v1/admin/reviews/" + key, new Dictionary<string, object> { { "status", "hidden" } }), "Đã ẩn đánh giá")) await LoadAsync();
        }

        private IDictionary<string, object> SelectedRow() => list.SelectedIndices.Count == 0 ? null : rows[list.SelectedIndices[0]];

        private async Task ToggleLockAsync()
        {
            IDictionary<string, object> row = SelectedRow();
            if (row == null) return;
            string next = Str(row, "status") == "locked" ? "active" : "locked";
            if (Check(await Api.PutAsync("/v1/admin/users/" + Str(row, "id"), new Dictionary<string, object> { { "status", next } }), next == "locked" ? "Đã khóa tài khoản" : "Đã mở khóa")) await LoadSectionAsync();
        }

        private async Task ResetPasswordAsync()
        {
            IDictionary<string, object> row = SelectedRow();
            if (row == null) return;
            if (MessageBox.Show(FindForm(), "Đặt lại mật khẩu của " + Str(row, "username") + "? Mật khẩu tạm chỉ hiện một lần.", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            ApiResponse r = await Api.PostAsync("/v1/admin/users/" + Str(row, "id") + "/reset-password", null);
            if (!Check(r)) return;
            string temp = JsonHelper.Str(r.Object, "temporaryPassword") ?? "(máy chủ không trả về mật khẩu, hãy đặt lại lần nữa)";
            // Shown, not copied: the clipboard history of Windows would keep it.
            MessageBox.Show(FindForm(), "Mật khẩu tạm của " + Str(row, "username") + ":\n\n" + temp + "\n\nChỉ hiện một lần. Người dùng phải đổi ngay khi đăng nhập.", "Mật khẩu tạm", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async Task ChangeRoleAsync()
        {
            IDictionary<string, object> row = SelectedRow();
            if (row == null) return;
            var roles = new[] { "user", "owner", "admin" };
            int index = Dialogs.Pick(FindForm(), "Vai trò của " + Str(row, "username"), new[] { "Vai trò" }, roles.Select(x => new[] { x }).ToList());
            if (index < 0) return;
            if (Check(await Api.PutAsync("/v1/admin/users/" + Str(row, "id"), new Dictionary<string, object> { { "role", roles[index] } }), "Đã đổi vai trò")) await LoadSectionAsync();
        }

        private async Task CreateUserAsync()
        {
            string[] v = Dialogs.Ask(FindForm(), "Tạo tài khoản (chủ quản)", new DialogField("Tên đăng nhập:"), new DialogField("Tên hiển thị:"), new DialogField("Mật khẩu tạm (tối thiểu 8 ký tự):", "", 1, true));
            if (v == null) return;
            var body = new Dictionary<string, object> { { "username", v[0] }, { "displayName", v[1] }, { "password", v[2] }, { "role", "owner" } };
            if (Check(await Api.PostAsync("/v1/admin/users", body), "Đã tạo tài khoản. Người dùng phải đổi mật khẩu khi đăng nhập")) await LoadSectionAsync();
        }
    }
}
