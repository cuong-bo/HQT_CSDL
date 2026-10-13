using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Hue_travel.Account;
using Hue_travel.Services.Data;
using Hue_travel.Services.Online;
using Hue_travel.Theme;

namespace Hue_travel.Admin
{
    /// <summary>
    /// "Địa điểm": look a place up (hidden and closed ones too), change it, add a new one, take one away, look after its photos,
    /// or bring a whole batch in from a CSV or GeoJSON file (shown first, applied only after a yes).
    /// </summary>
    internal sealed class PlacesSection : AdminSection
    {
        private const string Nothing = "Chọn một địa điểm bên trái để sửa, hoặc bấm \"Thêm địa điểm\".";
        private const int PageSize = 50;
        private static readonly string[] Groups = { "heritage", "scenery", "food", "stay", "service", "transport" };
        private static readonly string[] Statuses = { "visible", "hidden", "closed" };
        private static readonly string[] StatusNames = { "Đang hiển thị", "Đang ẩn", "Đóng cửa" };

        private readonly InputField search = new InputField { IconName = "search", PlaceholderText = "Tìm theo tên hoặc mã địa điểm…" };
        private readonly IconButton groupFilter = AccountKit.Secondary("Nhóm: Tất cả", "category");
        private readonly ItemList list = new ItemList { Selectable = true, EmptyText = "Không có địa điểm nào khớp." };
        private readonly IconButton more = AccountKit.Secondary("Xem thêm", "expand_more");
        private readonly Timer typing = new Timer { Interval = 350 };
        private readonly List<ListItem> shown = new List<ListItem>();
        private string filterGroup;
        private int page = 1;
        private string chosenId;
        private int version;

        public PlacesSection(ApiClient api) : base(api)
        {
            ListPane.Caption("Địa điểm");
            IconButton add = AccountKit.Primary("Thêm địa điểm", "add");
            IconButton import = AccountKit.Secondary("Nhập từ tệp…", "download");
            ListPane.Buttons(add, import);
            ListPane.Add(search, 40, 10);
            ListPane.Button(groupFilter, 40);
            ListPane.List(list);
            ListPane.Button(more, 42);
            more.Visible = false;

            add.Click += (s, e) => ShowForm(null);
            import.Click += async (s, e) => await ImportAsync();
            groupFilter.Click += async (s, e) => await ChooseGroupFilterAsync();
            more.Click += async (s, e) => { page++; await FetchAsync(); };
            typing.Tick += async (s, e) => { typing.Stop(); await LoadAsync(); };
            search.TextChanged += (s, e) => { typing.Stop(); typing.Start(); };
            list.ItemClicked += (s, item) => ShowForm((IDictionary<string, object>)item.Data);
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
            string path = "/v1/admin/pois?q=" + Uri.EscapeDataString(search.Text.Trim()) + "&page=" + page;
            if (filterGroup != null) path += "&group=" + filterGroup;
            ApiResponse r = await Api.GetAsync(path);
            if (IsMissing(r))
            {
                // An older server can create and change places but cannot list every one of them.
                list.EmptyText = ServerTooOld;
                list.SetItems(new List<ListItem>());
                more.Visible = false;
                ListPane.Relayout();
                return;
            }
            list.EmptyText = "Không có địa điểm nào khớp.";
            if (!Check(r)) return;
            List<IDictionary<string, object>> rows = Rows(r);
            foreach (IDictionary<string, object> d in rows)
            {
                string status = Str(d, "status");
                shown.Add(new ListItem
                {
                    Title = Str(d, "name"), Subtitle = AccountKit.GroupName(Str(d, "group")) + " · " + Str(d, "id") + (JsonHelper.Long(d, "photos") > 0 ? " · " + JsonHelper.Long(d, "photos") + " ảnh" : ""),
                    Icon = "pin_drop", Data = d, Pill = status == "visible" ? null : status == "hidden" ? "Đang ẩn" : "Đóng cửa", Tone = status == "hidden" ? PillTone.Warn : PillTone.Bad,
                });
            }
            list.SetItems(shown, true);
            more.Visible = rows.Count >= PageSize;
            ListPane.Relayout();
        }

        private async Task ChooseGroupFilterAsync()
        {
            var items = new List<ListItem> { new ListItem { Title = "Tất cả các nhóm", Icon = "category" } };
            items.AddRange(Groups.Select(g => new ListItem { Title = AccountKit.GroupName(g), Icon = "category" }));
            int index = Dialogs.Pick(Window, "Lọc theo nhóm", items, "Lọc");
            if (index < 0) return;
            filterGroup = index == 0 ? null : Groups[index - 1];
            groupFilter.Text = "Nhóm: " + (filterGroup == null ? "Tất cả" : AccountKit.GroupName(filterGroup));
            await LoadAsync();
        }

        // ------------------------------------------------------------------ the form

        private void ShowForm(IDictionary<string, object> d)
        {
            bool isNew = d == null;
            chosenId = isNew ? null : Str(d, "id");
            version++;
            string group = isNew ? "heritage" : Str(d, "group"), status = isNew ? "visible" : Str(d, "status");

            var id = new InputField { IconName = "pin_drop", Text = isNew ? "" : Str(d, "id"), Enabled = isNew, PlaceholderText = "Ví dụ DL099 (2 đến 20 ký tự, không dấu)" };
            var name = new InputField { Text = isNew ? "" : Str(d, "name") };
            var type = new InputField { Text = isNew ? "" : Str(d, "type") };
            var address = new InputField { IconName = "pin_drop", Text = isNew ? "" : Str(d, "address") };
            var hours = new InputField { Text = isNew ? "" : Str(d, "openingHours") };
            var price = new InputField { Text = isNew ? "" : Str(d, "priceText") };
            var phone = new InputField { IconName = "call", Text = isNew ? "" : Str(d, "phone") };
            var website = new InputField { IconName = "open_in_new", Text = isNew ? "" : Str(d, "website") };
            var summary = new InputField { IsMultiline = true, Text = isNew ? "" : Str(d, "summary") };
            var lon = new InputField { Text = isNew ? "" : JsonHelper.Dbl(d, "lon").ToString("0.000000", CultureInfo.InvariantCulture), PlaceholderText = "Ví dụ 107.5786" };
            var lat = new InputField { Text = isNew ? "" : JsonHelper.Dbl(d, "lat").ToString("0.000000", CultureInfo.InvariantCulture), PlaceholderText = "Ví dụ 16.4698" };
            IconButton groupButton = AccountKit.Secondary(AccountKit.GroupName(group), "category");
            IconButton statusButton = AccountKit.Secondary(StatusNames[Array.IndexOf(Statuses, status)], "visibility");

            DetailPane.Clear();
            DetailPane.Caption(isNew ? "Địa điểm mới" : Str(d, "name"));
            DetailPane.Hint(isNew ? "Điền thông tin rồi bấm Lưu. Vị trí theo kinh độ và vĩ độ (độ); địa điểm được gắn vào đường đi gần nhất để tìm đường." : "Mã " + Str(d, "id") + " · phiên bản " + JsonHelper.Long(d, "version"));
            DetailPane.Field("Mã địa điểm", id);
            DetailPane.Field("Tên", name);
            DetailPane.Add(AccountKit.FieldLabel("Nhóm và trạng thái"), 24, 2);
            DetailPane.Buttons(groupButton, statusButton);
            DetailPane.Field("Loại hình", type);
            DetailPane.Field("Địa chỉ", address);
            DetailPane.Field("Giờ mở cửa", hours);
            DetailPane.Field("Giá vé / giá dịch vụ", price);
            DetailPane.Field("Số điện thoại", phone);
            DetailPane.Field("Website (http:// hoặc https://)", website);
            DetailPane.Field("Giới thiệu", summary, 110);
            DetailPane.Field("Kinh độ", lon);
            DetailPane.Field("Vĩ độ", lat);
            IconButton save = AccountKit.Primary(isNew ? "Thêm địa điểm" : "Lưu thay đổi", "check");
            DetailPane.Button(save, 48, 14);

            groupButton.Click += (s, e) =>
            {
                int index = Dialogs.Pick(Window, "Nhóm của địa điểm", Groups.Select(g => new ListItem { Title = AccountKit.GroupName(g), Icon = "category" }).ToList(), "Chọn");
                if (index >= 0) { group = Groups[index]; groupButton.Text = AccountKit.GroupName(group); }
            };
            statusButton.Click += (s, e) =>
            {
                int index = Dialogs.Pick(Window, "Trạng thái", StatusNames.Select(n => new ListItem { Title = n, Icon = "visibility" }).ToList(), "Chọn");
                if (index >= 0) { status = Statuses[index]; statusButton.Text = StatusNames[index]; }
            };
            save.Click += async (s, e) =>
            {
                double longitude, latitude;
                if (!double.TryParse(lon.Text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out longitude) || !double.TryParse(lat.Text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out latitude))
                {
                    Notify("Kinh độ và vĩ độ phải là số (độ).", ToastKind.Error);
                    return;
                }
                var body = new Dictionary<string, object>
                {
                    { "name", name.Text.Trim() }, { "group", group }, { "type", type.Text.Trim() }, { "address", address.Text.Trim() }, { "openingHours", hours.Text.Trim() },
                    { "priceText", price.Text.Trim() }, { "phone", phone.Text.Trim() }, { "website", website.Text.Trim() }, { "summary", summary.Text.Trim() },
                    { "lon", longitude }, { "lat", latitude },
                };
                if (isNew) { body["id"] = id.Text.Trim(); await SaveNewAsync(body); }
                else { body["status"] = status; await SaveAsync(body); }
            };

            if (!isNew)
            {
                DetailPane.Divider();
                DetailPane.Caption("Ảnh");
                BuildPhotos(Str(d, "id"));
                DetailPane.Divider();
                IconButton delete = AccountKit.Danger("Xóa địa điểm", "close");
                DetailPane.Button(delete, 44);
                DetailPane.Hint("Xóa là ẩn địa điểm khỏi mọi danh sách; dữ liệu liên quan (đánh giá, chủ quản) vẫn còn trong cơ sở dữ liệu.");
                delete.Click += async (s, e) => await DeleteAsync(d);
            }
            DetailPane.Relayout();
        }

        private async Task SaveNewAsync(Dictionary<string, object> body)
        {
            if (!Check(await Api.PostAsync("/v1/admin/pois", body), "Đã thêm địa điểm")) return;
            chosenId = (string)body["id"];
            RaisePlacesChanged();
            await LoadAsync();
        }

        private async Task SaveAsync(Dictionary<string, object> body)
        {
            if (!Check(await Api.PutAsync("/v1/admin/pois/" + Uri.EscapeDataString(chosenId), body), "Đã lưu thay đổi")) return;
            RaisePlacesChanged();
            await LoadAsync();
        }

        private async Task DeleteAsync(IDictionary<string, object> d)
        {
            if (!Dialogs.Confirm(Window, "Xóa địa điểm", "Xóa \"" + Str(d, "name") + "\"? Địa điểm biến mất khỏi mọi danh sách và bản đồ.", "Xóa", true)) return;
            if (!Check(await Api.DeleteAsync("/v1/admin/pois/" + Uri.EscapeDataString(Str(d, "id"))), "Đã xóa địa điểm")) return;
            chosenId = null;
            ShowNothing(Nothing);
            RaisePlacesChanged();
            await LoadAsync();
        }

        // ------------------------------------------------------------------ photos of the chosen place

        private void BuildPhotos(string placeId)
        {
            var grid = new PhotoGrid { EmptyText = "Địa điểm này chưa có ảnh nào được duyệt." };
            DetailPane.Add(grid, width => grid.HeightFor(width), 12);
            IconButton add = AccountKit.Secondary("Thêm ảnh (hiển thị ngay)", "add");
            DetailPane.Button(add, 44);
            int mine = version;
            var ignored = LoadPhotosAsync(grid, placeId, mine);
            grid.DeleteClicked += async (s, cell) =>
            {
                if (!Dialogs.Confirm(Window, "Xóa ảnh", "Xóa ảnh này khỏi địa điểm?", "Xóa", true)) return;
                if (Check(await Api.DeleteAsync("/v1/admin/photos/" + cell.Id), "Đã xóa ảnh")) { await LoadPhotosAsync(grid, placeId, mine); await LoadAsync(); }
            };
            add.Click += async (s, e) =>
            {
                using (var dialog = new OpenFileDialog { Title = "Chọn ảnh địa điểm", Filter = "Ảnh (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png", Multiselect = true })
                {
                    if (dialog.ShowDialog(Window) != DialogResult.OK) return;
                    int sent = 0;
                    foreach (string file in dialog.FileNames)
                    {
                        if (Check(await PhotoSync.UploadAsync(Api, placeId, file, true, System.Threading.CancellationToken.None))) sent++; else break;
                    }
                    if (sent > 0) Notify("Đã thêm " + sent + " ảnh.", ToastKind.Success);
                    await LoadPhotosAsync(grid, placeId, mine);
                    await LoadAsync();
                }
            };
        }

        private async Task LoadPhotosAsync(PhotoGrid grid, string placeId, int mine)
        {
            ApiResponse r = await Api.GetAsync("/v1/pois/" + Uri.EscapeDataString(placeId) + "/photos");
            if (mine != version || grid.IsDisposed || !Check(r)) return;
            var cells = Rows(r).Select(d => new PhotoCell { Id = JsonHelper.Long(d, "id"), Status = "approved" }).ToList();
            grid.SetCells(cells);
            DetailPane.Relayout();
            foreach (PhotoCell cell in cells)
            {
                Bitmap image = await PhotoSync.LoadAsync(Api, cell.Id, System.Threading.CancellationToken.None);
                if (mine != version || grid.IsDisposed) { image?.Dispose(); return; }
                grid.SetImage(cell.Id, image);
            }
        }

        // ------------------------------------------------------------------ import from a file

        private async Task ImportAsync()
        {
            string file;
            using (var dialog = new OpenFileDialog { Title = "Chọn tệp địa điểm", Filter = "Tệp địa điểm (*.csv;*.geojson;*.json)|*.csv;*.geojson;*.json|Tất cả|*.*" })
            {
                if (dialog.ShowDialog(Window) != DialogResult.OK) return;
                file = dialog.FileName;
            }
            PlaceFileResult read = PlaceFileReader.Read(file);
            if (read.Rows.Count == 0)
            {
                Dialogs.Message(Window, "Không đọc được địa điểm nào", string.Join("\n", read.Problems.Take(8).DefaultIfEmpty("Tệp không có địa điểm nào.")) + "\n\nTệp CSV: dòng đầu là tên cột (id, group, name, lon, lat, type, address, openingHours, priceText, phone, website, summary). Ô để trống nghĩa là giữ nguyên.");
                return;
            }

            var places = read.Rows.ToArray();
            ApiResponse preview = await Api.PostAsync("/v1/admin/pois/import", new Dictionary<string, object> { { "places", places }, { "dryRun", true } });
            if (!Check(preview)) return;
            IDictionary<string, object> counts = JsonHelper.Obj(preview.Object, "counts");
            long create = JsonHelper.Long(counts, "create"), update = JsonHelper.Long(counts, "update"), unchanged = JsonHelper.Long(counts, "unchanged"), invalid = JsonHelper.Long(counts, "invalid");

            var text = new StringBuilder();
            text.AppendLine("Tệp có " + read.Rows.Count + " địa điểm:");
            text.AppendLine("  Thêm mới: " + create + "   Cập nhật: " + update + "   Không đổi: " + unchanged + "   Lỗi: " + invalid);
            foreach (IDictionary<string, object> row in (JsonHelper.AsList(preview.Object.ContainsKey("rows") ? preview.Object["rows"] : null) ?? new List<object>()).OfType<IDictionary<string, object>>().Where(x => Str(x, "action") == "invalid").Take(6))
                text.AppendLine("  • " + Str(row, "id") + ": " + Str(row, "problem"));
            foreach (string problem in read.Problems.Take(4)) text.AppendLine("  • " + problem);
            if (create + update == 0) { Dialogs.Message(Window, "Nhập địa điểm từ tệp", text + "\nKhông có gì để áp dụng."); return; }
            text.AppendLine("\nÁp dụng " + (create + update) + " thay đổi? Địa điểm không có trong tệp được giữ nguyên.");
            if (!Dialogs.Confirm(Window, "Nhập địa điểm từ tệp", text.ToString(), "Áp dụng")) return;

            if (!Check(await Api.PostAsync("/v1/admin/pois/import", new Dictionary<string, object> { { "places", places }, { "dryRun", false } }), "Đã nhập " + (create + update) + " địa điểm")) return;
            RaisePlacesChanged();
            await LoadAsync();
        }
    }
}
