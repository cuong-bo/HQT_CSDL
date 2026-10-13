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
    /// <summary>"Địa điểm yêu thích": the places the person starred. A row shows the place on the map; the cross un-stars it.</summary>
    internal sealed class FavoritesTab : AccountTab
    {
        private readonly ItemList list = new ItemList { TrailingIcon = "close", EmptyText = "Bạn chưa lưu địa điểm yêu thích nào.\nMở một địa điểm trên bản đồ và bấm \"Yêu thích\"." };

        public event EventHandler<Poi> ShowOnMapRequested;
        public event EventHandler FavoritesChanged;

        public FavoritesTab(ApiClient api) : base(api)
        {
            AddHint("Bấm vào một địa điểm để xem trên bản đồ. Bấm dấu × để bỏ khỏi danh sách yêu thích.");
            AddList(list);
            list.ItemClicked += (s, item) => ShowOnMapRequested?.Invoke(this, (Poi)item.Data);
            list.TrailingClicked += async (s, item) => await RemoveAsync((Poi)item.Data);
        }

        public override async Task LoadAsync()
        {
            list.SetItems(new ListItem[0]);   // never show what the previous account had
            ApiResponse r = await Api.GetAsync("/v1/favorites");
            if (!Check(r)) return;
            var items = new List<ListItem>();
            foreach (IDictionary<string, object> d in (JsonHelper.AsList(r.Json) ?? new List<object>()).OfType<IDictionary<string, object>>())
            {
                Poi p = PoiJson.ToPoi(d);
                items.Add(new ListItem { Title = p.Name, Subtitle = AccountUi.GroupName(p.Category) + (string.IsNullOrWhiteSpace(p.Address) ? "" : " · " + p.Address), Icon = "star", Data = p });
            }
            list.SetItems(items);
            Stack.Relayout();
        }

        private async Task RemoveAsync(Poi p)
        {
            if (!Check(await Api.DeleteAsync("/v1/favorites/" + Uri.EscapeDataString(p.Id)))) return;
            await LoadAsync();
            FavoritesChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>"Lộ trình đã lưu": itineraries kept on the account. A row opens the trip in the route form again; the cross deletes it.</summary>
    internal sealed class ItinerariesTab : AccountTab
    {
        private readonly ItemList list = new ItemList { TrailingIcon = "close", SubtitleLines = 2, EmptyText = "Chưa có lộ trình nào được lưu.\nLập lộ trình trong mục Lộ trình rồi bấm \"Lưu lộ trình đang lập\"." };

        /// <summary>Gives the place ids of the route currently planned in the route panel (supplied by the main window).</summary>
        public Func<IReadOnlyList<string>> CurrentRouteStopIds { get; set; }

        /// <summary>Raised with the places of a saved trip (the first is the start) when its row is pressed.</summary>
        public event EventHandler<IReadOnlyList<Poi>> UseRequested;

        public ItinerariesTab(ApiClient api) : base(api)
        {
            IconButton save = AccountKit.Primary("Lưu lộ trình đang lập", "bookmark_add");
            AddButton(save, 48);
            AddHint("Bấm vào một lộ trình để mở lại trong mục Lộ trình và tìm đường. Dấu × xóa lộ trình.");
            AddList(list);
            save.Click += async (s, e) => await SaveCurrentAsync();
            list.ItemClicked += (s, item) => Use((Saved)item.Data);
            list.TrailingClicked += async (s, item) => await DeleteAsync((Saved)item.Data);
        }

        private sealed class Saved
        {
            public long Id;
            public string Name;
            public List<Poi> Places;
        }

        public override async Task LoadAsync()
        {
            list.SetItems(new ListItem[0]);
            ApiResponse r = await Api.GetAsync("/v1/itineraries");
            if (!Check(r)) return;
            var items = new List<ListItem>();
            foreach (IDictionary<string, object> d in (JsonHelper.AsList(r.Json) ?? new List<object>()).OfType<IDictionary<string, object>>())
            {
                var places = (JsonHelper.List(d, "poiIds") ?? new List<object>()).Select(o => Find(Convert.ToString(o))).Where(p => p != null).ToList();
                var saved = new Saved { Id = JsonHelper.Long(d, "id"), Name = JsonHelper.Str(d, "name"), Places = places };
                items.Add(new ListItem { Title = saved.Name, Subtitle = places.Count + " điểm: " + string.Join(" → ", places.Select(p => p.Name)), Icon = "route", Data = saved });
            }
            list.SetItems(items);
            Stack.Relayout();
        }

        private static Poi Find(string id) => PoiCatalog.All.FirstOrDefault(x => x.Id == id);

        private void Use(Saved saved)
        {
            if (saved.Places.Count < 2) { Notify("Lộ trình này cần ít nhất hai địa điểm (điểm bắt đầu và một điểm đến).", ToastKind.Info); return; }
            UseRequested?.Invoke(this, saved.Places);
        }

        private async Task SaveCurrentAsync()
        {
            IReadOnlyList<string> stops = CurrentRouteStopIds?.Invoke();
            if (stops == null || stops.Count == 0)
            {
                Notify("Hãy lập một lộ trình trong mục Lộ trình trước.", ToastKind.Info);
                return;
            }
            string[] values = Dialogs.Ask(FindForm(), "Lưu lộ trình", new DialogField("Tên lộ trình", "Lộ trình " + DateTime.Now.ToString("dd/MM")));
            if (values == null) return;
            var body = new Dictionary<string, object> { { "name", values[0] }, { "poiIds", stops.ToArray() } };
            if (Check(await Api.PostAsync("/v1/itineraries", body), "Đã lưu lộ trình vào tài khoản")) await LoadAsync();
        }

        private async Task DeleteAsync(Saved saved)
        {
            if (!Dialogs.Confirm(FindForm(), "Xóa lộ trình", "Xóa lộ trình \"" + saved.Name + "\"?", "Xóa", true)) return;
            if (Check(await Api.DeleteAsync("/v1/itineraries/" + saved.Id))) await LoadAsync();
        }
    }

    /// <summary>"Đánh giá của tôi": the reviews the person wrote. A row opens that place's reviews; the cross deletes the review.</summary>
    internal sealed class MyReviewsTab : AccountTab
    {
        private readonly ItemList list = new ItemList { TrailingIcon = "close", SubtitleLines = 3, EmptyText = "Bạn chưa đánh giá địa điểm nào.\nMở một địa điểm trên bản đồ và bấm \"Đánh giá\"." };

        /// <summary>Raised with the place of a row that was pressed, to open its reviews.</summary>
        public event EventHandler<Poi> OpenRequested;

        public MyReviewsTab(ApiClient api) : base(api)
        {
            AddHint("Bấm vào một đánh giá để xem hoặc sửa. Dấu × xóa đánh giá của bạn.");
            AddList(list);
            list.ItemClicked += (s, item) => { Poi p = PoiCatalog.All.FirstOrDefault(x => x.Id == (string)item.Data); if (p != null) OpenRequested?.Invoke(this, p); };
            list.TrailingClicked += async (s, item) => await DeleteAsync((string)item.Data, item.Title);
        }

        public override async Task LoadAsync()
        {
            list.SetItems(new ListItem[0]);
            ApiResponse r = await Api.GetAsync("/v1/me/reviews");
            if (!Check(r)) return;
            var items = new List<ListItem>();
            foreach (IDictionary<string, object> d in (JsonHelper.AsList(r.Json) ?? new List<object>()).OfType<IDictionary<string, object>>())
            {
                string comment = JsonHelper.Str(d, "comment");
                string reply = JsonHelper.Str(d, "ownerReply");
                bool hidden = JsonHelper.Str(d, "status") == "hidden";
                string text = (string.IsNullOrWhiteSpace(comment) ? "(không có nhận xét)" : comment) + (string.IsNullOrWhiteSpace(reply) ? "" : "\nChủ quản trả lời: " + reply);
                items.Add(new ListItem
                {
                    Title = JsonHelper.Str(d, "poiName"), Subtitle = text, Icon = "star", Data = JsonHelper.Str(d, "poiId"),
                    Pill = hidden ? "Đã bị ẩn" : JsonHelper.Long(d, "rating") + " sao", Tone = hidden ? PillTone.Bad : PillTone.Warn,
                });
            }
            list.SetItems(items);
            Stack.Relayout();
        }

        private async Task DeleteAsync(string poiId, string name)
        {
            if (!Dialogs.Confirm(FindForm(), "Xóa đánh giá", "Xóa đánh giá của bạn về \"" + name + "\"?", "Xóa", true)) return;
            if (Check(await Api.DeleteAsync("/v1/pois/" + Uri.EscapeDataString(poiId) + "/reviews"), "Đã xóa đánh giá")) await LoadAsync();
        }
    }

    /// <summary>"Thông tin tài khoản": the name shown with the person's reviews.</summary>
    internal sealed class ProfileTab : AccountTab
    {
        private readonly AuthService auth;
        private readonly InputField userName = new InputField { IconName = "account_circle", Enabled = false };
        private readonly InputField displayName = new InputField { IconName = "account_circle" };

        /// <summary>The display name changed: the profile card shows the new one.</summary>
        public event EventHandler Changed;

        public ProfileTab(ApiClient api, AuthService auth) : base(api)
        {
            this.auth = auth;
            AddHint("Tên hiển thị xuất hiện cạnh đánh giá của bạn. Tên đăng nhập không đổi được.");
            AddField("Tên đăng nhập", userName);
            AddField("Tên hiển thị", displayName);
            IconButton save = AccountKit.Primary("Lưu thay đổi", "check");
            AddButton(save, 48);
            save.Click += async (s, e) => await SaveAsync();
        }

        public override Task LoadAsync()
        {
            userName.Text = auth.CurrentUser?.Username ?? "";
            displayName.Text = auth.CurrentUser?.DisplayName ?? "";
            return Task.CompletedTask;
        }

        private async Task SaveAsync()
        {
            string name = displayName.Text.Trim();
            if (name.Length < 1 || name.Length > 60) { displayName.HasError = true; Notify("Tên hiển thị từ 1 đến 60 ký tự.", ToastKind.Error); return; }
            if (!Check(await Api.PutAsync("/v1/me", new Dictionary<string, object> { { "displayName", name } }), "Đã cập nhật tên hiển thị")) return;
            auth.UpdateDisplayName(name);
            Changed?.Invoke(this, EventArgs.Empty);
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
            AddHint("Sau khi đổi mật khẩu, mọi phiên đăng nhập cũ bị đóng và bạn đăng nhập lại bằng mật khẩu mới.");
            AddField("Mật khẩu hiện tại", current);
            AddField("Mật khẩu mới", next);
            AddField("Nhập lại mật khẩu mới", again);
            IconButton change = AccountKit.Primary("Đổi mật khẩu", "lock_reset");
            AddButton(change, 48);
            change.Click += async (s, e) => await ChangeAsync();
        }

        private static InputField Field(string placeholder) => new InputField { IconName = "lock", IsPassword = true, Revealable = true, PlaceholderText = placeholder };

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
