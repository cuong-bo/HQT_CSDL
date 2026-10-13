using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Hue_travel.Models;
using Hue_travel.Services.Data;
using Hue_travel.Services.Online;
using Hue_travel.Theme;

namespace Hue_travel.Account
{
    /// <summary>
    /// "Địa điểm của tôi": the places the person manages, the requests still waiting for an admin, and a button to ask for more.
    /// A place opens in its own screen (<see cref="PlaceManageTab"/>).
    /// </summary>
    internal sealed class OwnerPlacesTab : AccountTab
    {
        private const int MaxClaimsAtOnce = 5;
        private readonly ItemList places = new ItemList { TrailingIcon = "chevron_right", TrailingHotColor = ThemeColors.Primary, EmptyText = "Bạn chưa quản lý địa điểm nào.\nGửi yêu cầu bên dưới; quản trị viên sẽ xác minh rồi duyệt." };
        private readonly ItemList claims = new ItemList { EmptyText = "Không có yêu cầu nào đang chờ." };

        /// <summary>Raised with the place whose row was pressed: the panel opens its management screen.</summary>
        public event EventHandler<Poi> OpenPlaceRequested;

        /// <summary>The number of places and waiting requests, so the menu can say it.</summary>
        public int PlaceCount { get; private set; }

        public int PendingCount { get; private set; }

        public OwnerPlacesTab(ApiClient api) : base(api)
        {
            AddHint("Chủ quản cập nhật thông tin, ảnh, thông báo và trả lời đánh giá cho địa điểm của mình. Thay đổi thông tin và ảnh được quản trị viên duyệt trước khi hiển thị.");
            AddCaption("Địa điểm đang quản lý");
            AddList(places);
            AddCaption("Yêu cầu nhận địa điểm");
            AddList(claims);
            IconButton ask = AccountKit.Secondary("Đăng ký quản lý thêm địa điểm", "add");
            AddButton(ask);
            ask.Click += async (s, e) => await AskAsync();
            places.ItemClicked += (s, item) => OpenPlaceRequested?.Invoke(this, (Poi)item.Data);
        }

        public override async Task LoadAsync()
        {
            ApiResponse mine = await Api.GetAsync("/v1/owner/places");
            if (Check(mine))
            {
                var items = new List<ListItem>();
                foreach (IDictionary<string, object> d in (JsonHelper.AsList(mine.Json) ?? new List<object>()).OfType<IDictionary<string, object>>())
                {
                    Poi p = PoiJson.ToPoi(d);
                    items.Add(new ListItem { Title = p.Name, Subtitle = AccountUi.GroupName(p.Category) + (string.IsNullOrWhiteSpace(p.Address) ? "" : " · " + p.Address), Icon = "pin_drop", Data = p });
                }
                places.SetItems(items);
                PlaceCount = items.Count;
            }
            ApiResponse requested = await Api.GetAsync("/v1/owner/claims");
            if (Check(requested))
            {
                var items = new List<ListItem>();
                foreach (IDictionary<string, object> d in (JsonHelper.AsList(requested.Json) ?? new List<object>()).OfType<IDictionary<string, object>>())
                {
                    string status = JsonHelper.Str(d, "status");
                    if (status == "approved") continue;   // it is in the list above
                    items.Add(new ListItem
                    {
                        Title = JsonHelper.Str(d, "poiName"), Subtitle = "Gửi ngày " + AccountKit.Day(JsonHelper.Str(d, "createdAt")), Icon = "verified_user",
                        Pill = AccountKit.RequestStatusName(status), Tone = status == "rejected" ? PillTone.Bad : PillTone.Warn, Data = JsonHelper.Str(d, "poiId"),
                    });
                }
                claims.SetItems(items);
                PendingCount = items.Count(i => i.Tone == PillTone.Warn);
            }
            Stack.Relayout();
        }

        private async Task AskAsync()
        {
            var known = new HashSet<string>(claims.Items.Select(i => (string)i.Data).Concat(places.Items.Select(i => ((Poi)i.Data).Id)));
            IReadOnlyList<Poi> chosen = Dialogs.PickPlaces(FindForm(), "Chọn địa điểm bạn quản lý", MaxClaimsAtOnce, new string[0], "Gửi yêu cầu");
            if (chosen == null) return;
            int sent = 0;
            foreach (Poi p in chosen.Where(p => !known.Contains(p.Id)))
            {
                if (Check(await Api.PostAsync("/v1/owner/claims", new Dictionary<string, object> { { "poiId", p.Id } }))) sent++;
            }
            if (sent > 0) Notify("Đã gửi " + sent + " yêu cầu. Quản trị viên sẽ xác minh với bạn rồi duyệt.", ToastKind.Success);
            else if (chosen.Count > 0) Notify("Bạn đã gửi yêu cầu hoặc đang quản lý các địa điểm này.", ToastKind.Info);
            await LoadAsync();
        }
    }

    /// <summary>
    /// One place of an owner: its information (changes wait for an admin), photos, reviews to answer, announcements and the
    /// numbers of the last 30 days, in five tabs of one screen.
    /// </summary>
    internal sealed class PlaceManageTab : AccountTab
    {
        private readonly Label title = AccountKit.Caption("");
        private readonly SegmentTabs tabs = new SegmentTabs();
        private readonly List<Control>[] sections = { new List<Control>(), new List<Control>(), new List<Control>(), new List<Control>(), new List<Control>() };
        private readonly bool[] loaded = new bool[5];
        private Poi place;

        // information
        private readonly InputField address = new InputField { IconName = "pin_drop" };
        private readonly InputField hours = new InputField { IconName = "schedule" };
        private readonly InputField price = new InputField { IconName = "sell" };
        private readonly InputField phone = new InputField { IconName = "call" };
        private readonly InputField website = new InputField { IconName = "open_in_new" };
        private readonly InputField summary = new InputField { IsMultiline = true };

        // photos
        private readonly PhotoGrid photos = new PhotoGrid { EmptyText = "Địa điểm này chưa có ảnh nào do bạn tải lên." };

        // reviews
        private readonly ItemList reviews = new ItemList { SubtitleLines = 3, EmptyText = "Địa điểm này chưa có đánh giá." };

        // announcements
        private readonly InputField announceTitle = new InputField { IconName = "campaign" };
        private readonly InputField announceBody = new InputField { IsMultiline = true };
        private readonly ItemList announcements = new ItemList { SubtitleLines = 2, EmptyText = "Chưa có thông báo nào đang hiệu lực." };

        // numbers
        private readonly ItemList stats = new ItemList { EmptyText = "Chưa có số liệu." };

        public PlaceManageTab(ApiClient api) : base(api)
        {
            Stack.Add(title, 28, 8);
            tabs.SetTabs("Thông tin", "Ảnh", "Đánh giá", "Thông báo", "Số liệu");
            Stack.Add(tabs, 38, 14);
            tabs.SelectedChanged += async (s, e) => await ShowSectionAsync();

            BuildInfo();
            BuildPhotos();
            BuildReviews();
            BuildAnnouncements();
            BuildStats();
            ApplySection();
        }

        // Adds rows to the stack and remembers which tab they belong to.
        private void In(int section, Control control, int designHeight, int gap = 10, bool indented = true)
        {
            Stack.Add(control, designHeight, gap, indented);
            sections[section].Add(control);
        }

        private void In(int section, Control control, Func<int, int> height, int gap = 10, bool indented = true)
        {
            Stack.Add(control, height, gap, indented);
            sections[section].Add(control);
        }

        private void InField(int section, string label, InputField field, int height = 40)
        {
            In(section, AccountKit.FieldLabel(label), 24, 2);
            In(section, field, height, 12);
        }

        private void InHint(int section, string text)
        {
            Label label = AccountKit.Hint(text);
            In(section, label, width => HintHeight(label, width), 10);
        }

        // ------------------------------------------------------------------ building the five tabs

        private void BuildInfo()
        {
            InHint(0, "Thay đổi được gửi cho quản trị viên duyệt, sau khi duyệt mới hiển thị cho mọi người. Tên và vị trí địa điểm do quản trị viên giữ.");
            InField(0, "Địa chỉ", address);
            InField(0, "Giờ mở cửa", hours);
            InField(0, "Giá vé / giá dịch vụ", price);
            InField(0, "Số điện thoại", phone);
            InField(0, "Website (http:// hoặc https://)", website);
            InField(0, "Giới thiệu", summary, 120);
            IconButton send = AccountKit.Primary("Gửi yêu cầu chỉnh sửa", "check");
            In(0, send, 48);
            send.Click += async (s, e) => await SendEditAsync();
        }

        private void BuildPhotos()
        {
            InHint(1, "Ảnh bạn tải lên hiển thị cho mọi người sau khi quản trị viên duyệt. Mỗi địa điểm có tối đa 8 ảnh, định dạng JPEG hoặc PNG, mỗi ảnh tối đa 6 MB.");
            In(1, photos, width => photos.HeightFor(width), 12);
            IconButton add = AccountKit.Primary("Thêm ảnh", "add");
            In(1, add, 48);
            add.Click += async (s, e) => await AddPhotoAsync();
            photos.DeleteClicked += async (s, cell) => await DeletePhotoAsync(cell);
        }

        private void BuildReviews()
        {
            InHint(2, "Bấm vào một đánh giá để viết phản hồi. Phản hồi hiển thị công khai bên dưới đánh giá.");
            In(2, reviews, width => reviews.WantedHeight, 10, false);
            reviews.ItemClicked += async (s, item) => await ReplyAsync(item);
        }

        private void BuildAnnouncements()
        {
            InHint(3, "Thông báo hiển thị trong thông tin địa điểm (khuyến mãi, giờ mở cửa đặc biệt, đóng cửa tạm thời...).");
            InField(3, "Tiêu đề", announceTitle);
            InField(3, "Nội dung", announceBody, 110);
            IconButton post = AccountKit.Primary("Đăng thông báo", "campaign");
            In(3, post, 48, 16);
            post.Click += async (s, e) => await AnnounceAsync();
            In(3, AccountKit.FieldLabel("Đang hiệu lực"), 24, 2);
            In(3, announcements, width => announcements.WantedHeight, 10, false);
        }

        private void BuildStats()
        {
            InHint(4, "Số liệu 30 ngày gần nhất của địa điểm.");
            In(4, stats, width => stats.WantedHeight, 10, false);
        }

        // ------------------------------------------------------------------ showing a place

        /// <summary>Opens the screen for a place (the panel calls this before showing it).</summary>
        public void SetPlace(Poi poi)
        {
            place = poi;
            title.Text = poi.Name;
            address.Text = poi.Address;
            hours.Text = poi.OpeningHours;
            price.Text = poi.TicketPrice;
            phone.Text = poi.Phone;
            website.Text = poi.Website;
            summary.Text = poi.Summary;
            announceTitle.Text = announceBody.Text = "";
            Array.Clear(loaded, 0, loaded.Length);
            reviews.SetItems(new ListItem[0]);
            announcements.SetItems(new ListItem[0]);
            stats.SetItems(new ListItem[0]);
            photos.SetCells(new PhotoCell[0]);
            tabs.SelectedIndex = 0;
            ApplySection();
        }

        public override Task LoadAsync() => ShowSectionAsync();

        private void ApplySection()
        {
            for (int i = 0; i < sections.Length; i++)
                foreach (Control c in sections[i]) c.Visible = i == tabs.SelectedIndex;
            Stack.Relayout();
        }

        private async Task ShowSectionAsync()
        {
            ApplySection();
            if (place == null) return;
            int index = tabs.SelectedIndex;
            if (loaded[index]) return;
            loaded[index] = true;
            switch (index)
            {
                case 1: await LoadPhotosAsync(); break;
                case 2: await LoadReviewsAsync(); break;
                case 3: await LoadAnnouncementsAsync(); break;
                case 4: await LoadStatsAsync(); break;
            }
        }

        private string Path(string tail) => "/v1/owner/places/" + Uri.EscapeDataString(place.Id) + tail;

        // ------------------------------------------------------------------ information

        private async Task SendEditAsync()
        {
            var changes = new Dictionary<string, object>();
            AddIfChanged(changes, "address", address.Text, place.Address);
            AddIfChanged(changes, "openingHours", hours.Text, place.OpeningHours);
            AddIfChanged(changes, "priceText", price.Text, place.TicketPrice);
            AddIfChanged(changes, "phone", phone.Text, place.Phone);
            AddIfChanged(changes, "website", website.Text, place.Website);
            AddIfChanged(changes, "summary", summary.Text, place.Summary);
            if (changes.Count == 0) { Notify("Chưa có gì thay đổi.", ToastKind.Info); return; }
            Check(await Api.PostAsync(Path("/edit-requests"), new Dictionary<string, object> { { "changes", changes } }),
                "Đã gửi yêu cầu chỉnh sửa. Quản trị viên sẽ duyệt.");
        }

        private static void AddIfChanged(Dictionary<string, object> changes, string key, string value, string original)
        {
            if ((value ?? "").Trim() != (original ?? "").Trim()) changes[key] = (value ?? "").Trim();
        }

        // ------------------------------------------------------------------ photos

        private async Task LoadPhotosAsync()
        {
            ApiResponse r = await Api.GetAsync(Path("/photos"));
            if (!Check(r)) return;
            var cells = new List<PhotoCell>();
            foreach (IDictionary<string, object> d in (JsonHelper.AsList(r.Json) ?? new List<object>()).OfType<IDictionary<string, object>>())
                cells.Add(new PhotoCell { Id = JsonHelper.Long(d, "id"), Status = JsonHelper.Str(d, "status") });
            photos.SetCells(cells);
            Stack.Relayout();
            foreach (PhotoCell cell in cells.ToList())
            {
                Bitmap image = await PhotoSync.LoadAsync(Api, cell.Id, System.Threading.CancellationToken.None);
                if (IsDisposed || place == null) { image?.Dispose(); return; }
                photos.SetImage(cell.Id, image);
            }
        }

        private async Task AddPhotoAsync()
        {
            using (var dialog = new OpenFileDialog { Title = "Chọn ảnh địa điểm", Filter = "Ảnh (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png", Multiselect = true })
            {
                if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
                int sent = 0;
                foreach (string file in dialog.FileNames)
                {
                    ApiResponse r = await PhotoSync.UploadAsync(Api, place.Id, file, false, System.Threading.CancellationToken.None);
                    if (Check(r)) sent++; else break;
                }
                if (sent > 0) Notify("Đã tải lên " + sent + " ảnh. Ảnh hiển thị sau khi quản trị viên duyệt.", ToastKind.Success);
                await LoadPhotosAsync();
            }
        }

        private async Task DeletePhotoAsync(PhotoCell cell)
        {
            if (!Dialogs.Confirm(FindForm(), "Xóa ảnh", "Xóa ảnh này khỏi địa điểm?", "Xóa", true)) return;
            if (Check(await Api.DeleteAsync(Path("/photos/" + cell.Id)), "Đã xóa ảnh")) await LoadPhotosAsync();
        }

        // ------------------------------------------------------------------ reviews

        private async Task LoadReviewsAsync()
        {
            ApiResponse r = await Api.GetAsync("/v1/pois/" + Uri.EscapeDataString(place.Id) + "/reviews");
            if (!Check(r)) return;
            var items = new List<ListItem>();
            foreach (IDictionary<string, object> d in (JsonHelper.List(r.Object, "items") ?? new List<object>()).OfType<IDictionary<string, object>>())
            {
                string comment = JsonHelper.Str(d, "comment");
                string reply = JsonHelper.Str(d, "ownerReply");
                items.Add(new ListItem
                {
                    Title = JsonHelper.Str(d, "author"), Pill = JsonHelper.Long(d, "rating") + " sao", Tone = PillTone.Warn, Data = d,
                    Subtitle = (string.IsNullOrWhiteSpace(comment) ? "(không có nhận xét)" : comment) + (string.IsNullOrWhiteSpace(reply) ? "" : "\nBạn trả lời: " + reply),
                });
            }
            reviews.SetItems(items);
            Stack.Relayout();
        }

        private async Task ReplyAsync(ListItem item)
        {
            var d = (IDictionary<string, object>)item.Data;
            string[] v = Dialogs.Ask(FindForm(), "Phản hồi đánh giá", "Đánh giá của " + JsonHelper.Str(d, "author") + ": " + (JsonHelper.Str(d, "comment") ?? "(không có nhận xét)"), "Gửi phản hồi",
                new DialogField("Nội dung phản hồi", JsonHelper.Str(d, "ownerReply") ?? "", 4));
            if (v == null) return;
            if (Check(await Api.PostAsync(Path("/reviews/" + JsonHelper.Long(d, "id") + "/reply"), new Dictionary<string, object> { { "reply", v[0] } }), "Đã gửi phản hồi")) await LoadReviewsAsync();
        }

        // ------------------------------------------------------------------ announcements

        private async Task LoadAnnouncementsAsync()
        {
            ApiResponse r = await Api.GetAsync("/v1/pois/" + Uri.EscapeDataString(place.Id) + "/announcements");
            if (!Check(r)) return;
            var items = new List<ListItem>();
            foreach (IDictionary<string, object> d in (JsonHelper.AsList(r.Json) ?? new List<object>()).OfType<IDictionary<string, object>>())
                items.Add(new ListItem { Title = JsonHelper.Str(d, "title"), Subtitle = JsonHelper.Str(d, "body"), Icon = "campaign" });
            announcements.SetItems(items);
            Stack.Relayout();
        }

        private async Task AnnounceAsync()
        {
            if (announceTitle.Text.Trim().Length == 0 || announceBody.Text.Trim().Length == 0) { Notify("Hãy nhập tiêu đề và nội dung thông báo.", ToastKind.Info); return; }
            var body = new Dictionary<string, object> { { "title", announceTitle.Text.Trim() }, { "body", announceBody.Text.Trim() } };
            if (!Check(await Api.PostAsync(Path("/announcements"), body), "Đã đăng thông báo")) return;
            announceTitle.Text = announceBody.Text = "";
            await LoadAnnouncementsAsync();
        }

        // ------------------------------------------------------------------ numbers

        private async Task LoadStatsAsync()
        {
            ApiResponse r = await Api.GetAsync(Path("/stats?days=30"));
            if (!Check(r)) return;
            IList<object> days = JsonHelper.List(r.Object, "days") ?? new List<object>();
            double views = days.OfType<IDictionary<string, object>>().Sum(d => JsonHelper.Dbl(d, "views"));
            double adds = days.OfType<IDictionary<string, object>>().Sum(d => JsonHelper.Dbl(d, "routeAdds"));
            double average = JsonHelper.Dbl(r.Object, "averageRating");
            stats.SetItems(new[]
            {
                new ListItem { Title = views + " lượt xem", Subtitle = "Số lần mở thông tin địa điểm trong 30 ngày qua", Icon = "visibility" },
                new ListItem { Title = adds + " lần đưa vào lộ trình", Subtitle = "Khách thêm địa điểm vào lộ trình tham quan", Icon = "route" },
                new ListItem { Title = (average > 0 ? average.ToString("0.0") + " / 5 sao" : "Chưa có điểm"), Subtitle = JsonHelper.Long(r.Object, "reviewCount") + " đánh giá đang hiển thị", Icon = "star" },
            });
            Stack.Relayout();
        }
    }
}
