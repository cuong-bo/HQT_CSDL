using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Hue_travel.Models;
using Hue_travel.Services.Data;
using Hue_travel.Services.Online;
using Hue_travel.Theme;

namespace Hue_travel.Account
{
    internal sealed class FavoritesTab : AccountTab
    {
        private readonly ListView list = AccountUi.List(AccountUi.Col("Địa điểm", 300), AccountUi.Col("Nhóm", 120));
        private readonly List<Poi> pois = new List<Poi>();

        public event EventHandler<Poi> ShowOnMapRequested;
        public event EventHandler FavoritesChanged;

        public FavoritesTab(ApiClient api) : base(api)
        {
            IconButton show = AccountUi.Button("Xem trên bản đồ", true, 150, "place");
            IconButton remove = AccountUi.Button("Bỏ yêu thích", false, 120, "close");
            show.Click += (s, e) => { Poi p = Selected(); if (p != null) ShowOnMapRequested?.Invoke(this, p); };
            remove.Click += async (s, e) => await RemoveAsync();
            Controls.Add(list);
            Controls.Add(ButtonRow(show, remove));
        }

        private Poi Selected() => list.SelectedIndices.Count == 0 ? null : pois[list.SelectedIndices[0]];

        public override async Task LoadAsync()
        {
            pois.Clear();
            list.Items.Clear();   // never show what the previous account had
            ApiResponse r = await Api.GetAsync("/v1/favorites");
            if (!Check(r)) return;
            foreach (IDictionary<string, object> d in (JsonHelper.AsList(r.Json) ?? new List<object>()).OfType<IDictionary<string, object>>())
            {
                Poi p = PoiJson.ToPoi(d);
                pois.Add(p);
                list.Items.Add(new ListViewItem(new[] { p.Name, AccountUi.GroupName(p.Category) }));
            }
        }

        private async Task RemoveAsync()
        {
            Poi p = Selected();
            if (p == null) return;
            if (!Check(await Api.DeleteAsync("/v1/favorites/" + Uri.EscapeDataString(p.Id)))) return;
            await LoadAsync();
            FavoritesChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    internal sealed class ItinerariesTab : AccountTab
    {
        private readonly ListView list = AccountUi.List(AccountUi.Col("Tên", 170), AccountUi.Col("Các điểm", 300));
        private readonly List<long> ids = new List<long>();

        /// <summary>Gives the place ids of the route currently planned in the route panel (supplied by the main window).</summary>
        public Func<IReadOnlyList<string>> CurrentRouteStopIds { get; set; }

        public ItinerariesTab(ApiClient api) : base(api)
        {
            IconButton save = AccountUi.Button("Lưu lộ trình đang lập", true, 180, "bookmark_add");
            IconButton delete = AccountUi.Button("Xóa", false, 70, "close");
            save.Click += async (s, e) => await SaveCurrentAsync();
            delete.Click += async (s, e) => await DeleteAsync();
            Controls.Add(list);
            Controls.Add(ButtonRow(save, delete));
        }

        public override async Task LoadAsync()
        {
            ids.Clear();
            list.Items.Clear();
            ApiResponse r = await Api.GetAsync("/v1/itineraries");
            if (!Check(r)) return;
            foreach (IDictionary<string, object> d in (JsonHelper.AsList(r.Json) ?? new List<object>()).OfType<IDictionary<string, object>>())
            {
                ids.Add(JsonHelper.Long(d, "id"));
                IEnumerable<string> names = (JsonHelper.List(d, "poiIds") ?? new List<object>()).Select(o => NameOf(Convert.ToString(o)));
                list.Items.Add(new ListViewItem(new[] { JsonHelper.Str(d, "name"), string.Join(" → ", names) }));
            }
        }

        private static string NameOf(string id)
        {
            Poi p = PoiCatalog.All.FirstOrDefault(x => x.Id == id);
            return p != null ? p.Name : id;
        }

        private async Task SaveCurrentAsync()
        {
            IReadOnlyList<string> stops = CurrentRouteStopIds?.Invoke();
            if (stops == null || stops.Count == 0)
            {
                Notify("Hãy lập một lộ trình trong bảng Tìm đường ngắn nhất trước", ToastKind.Info);
                return;
            }
            string[] values = Dialogs.Ask(FindForm(), "Lưu lộ trình", new DialogField("Tên lộ trình:", "Lộ trình " + DateTime.Now.ToString("dd/MM")));
            if (values == null) return;
            var body = new Dictionary<string, object> { { "name", values[0] }, { "poiIds", stops.ToArray() } };
            if (Check(await Api.PostAsync("/v1/itineraries", body), "Đã lưu lộ trình vào tài khoản")) await LoadAsync();
        }

        private async Task DeleteAsync()
        {
            if (list.SelectedIndices.Count == 0) return;
            if (Check(await Api.DeleteAsync("/v1/itineraries/" + ids[list.SelectedIndices[0]]))) await LoadAsync();
        }
    }

    internal sealed class PasswordTab : AccountTab
    {
        private readonly InputField current = Field("Mật khẩu hiện tại");
        private readonly InputField next = Field("Mật khẩu mới (tối thiểu 8 ký tự)");
        private readonly InputField again = Field("Nhập lại mật khẩu mới");

        /// <summary>Raised after the password changed: the server ended every session, so the person signs in again.</summary>
        public event EventHandler PasswordChanged;

        public PasswordTab(ApiClient api) : base(api)
        {
            IconButton change = AccountUi.Button("Đổi mật khẩu", true, 150, "lock_reset");
            change.Click += async (s, e) => await ChangeAsync();
            int y = 12;
            foreach (InputField f in new[] { current, next, again })
            {
                Label caption = AccountUi.Caption(f.Tag.ToString());
                caption.Location = new System.Drawing.Point(12, y);
                f.Location = new System.Drawing.Point(12, y + 22);
                Controls.Add(caption);
                Controls.Add(f);
                y += 66;
            }
            change.Location = new System.Drawing.Point(12, y);
            Controls.Add(change);
            Label hint = AccountUi.Caption("Sau khi đổi, mọi phiên đăng nhập cũ bị đóng và bạn đăng nhập lại.");
            hint.Location = new System.Drawing.Point(12, y + 40);
            Controls.Add(hint);
        }

        private static InputField Field(string caption)
        {
            return new InputField { IconName = "lock", IsPassword = true, Revealable = true, Tag = caption, Size = new System.Drawing.Size(380, 32) };
        }

        public override Task LoadAsync() => Task.CompletedTask;

        private async Task ChangeAsync()
        {
            if (next.Text != again.Text) { Notify("Hai lần nhập mật khẩu mới chưa giống nhau", ToastKind.Error); return; }
            if (next.Text.Length < Hue_travel.Auth.AuthValidation.MinPasswordLength)
            {
                Notify("Mật khẩu mới cần tối thiểu " + Hue_travel.Auth.AuthValidation.MinPasswordLength + " ký tự", ToastKind.Error);
                return;
            }
            var body = new Dictionary<string, object> { { "currentPassword", current.Text }, { "newPassword", next.Text } };
            if (!Check(await Api.PostAsync("/v1/me/password", body), "Đã đổi mật khẩu. Hãy đăng nhập lại")) return;
            current.Text = next.Text = again.Text = "";
            PasswordChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
