using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Hue_travel.Account;
using Hue_travel.Models;
using Hue_travel.Services.Data;
using Hue_travel.Services.Online;
using Hue_travel.Theme;

namespace Hue_travel.Admin
{
    /// <summary>A photo shown whole inside a rounded frame (the admin looks at it before approving).</summary>
    internal sealed class PreviewBox : Control
    {
        private Bitmap image;

        public PreviewBox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Color.White;
        }

        public void SetImage(Bitmap value)
        {
            Bitmap old = image;
            image = value;
            old?.Dispose();
            Invalidate();
        }

        /// <summary>The height that shows the picture in full at the given width (a 3:2 frame while it is still loading).</summary>
        public int HeightFor(int width) => image == null ? width * 2 / 3 : Math.Min(width * 4 / 3, Math.Max(Ui.S(120), (int)(width * image.Height / (double)image.Width)));

        protected override void Dispose(bool disposing)
        {
            if (disposing) image?.Dispose();
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.Clear(BackColor);
            using (GraphicsPath path = Ui.RoundedRect(new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f), Ui.S(10)))
            {
                GraphicsState state = g.Save();
                g.SetClip(path, CombineMode.Intersect);
                using (var brush = new SolidBrush(ThemeColors.Slate100)) g.FillRectangle(brush, ClientRectangle);
                if (image != null) g.DrawImage(image, ClientRectangle);
                g.Restore(state);
                using (var pen = new Pen(ThemeColors.Slate200)) g.DrawPath(pen, path);
            }
        }
    }

    /// <summary>"Chờ duyệt": what waits for an admin, in three lists: owners asking for a place, owners' changes to a place, photos.</summary>
    internal sealed class ApprovalsSection : AdminSection
    {
        private const string Nothing = "Chọn một yêu cầu bên trái để xem chi tiết rồi duyệt hoặc từ chối.";

        private static readonly Dictionary<string, string> FieldNames = new Dictionary<string, string>
        {
            { "dia_chi", "Địa chỉ" }, { "gio_mo_cua", "Giờ mở cửa" }, { "gia_text", "Giá vé / dịch vụ" }, { "sdt", "Số điện thoại" }, { "website", "Website" }, { "mo_ta", "Giới thiệu" },
        };

        private readonly SegmentTabs tabs = new SegmentTabs();
        private readonly ItemList list = new ItemList { Selectable = true, SubtitleLines = 2, EmptyText = "Không có yêu cầu nào đang chờ." };
        private readonly List<ListItem>[] items = { new List<ListItem>(), new List<ListItem>(), new List<ListItem>() };
        private int loadVersion;

        public ApprovalsSection(ApiClient api) : base(api)
        {
            ListPane.Caption("Chờ duyệt");
            tabs.SetTabs("Nhận địa điểm", "Chỉnh sửa", "Ảnh");
            ListPane.Add(tabs, 38, 10);
            ListPane.List(list);
            tabs.SelectedChanged += (s, e) => ShowList();
            list.ItemClicked += async (s, item) => await ShowAsync(item);
            ShowNothing(Nothing);
        }

        public override async Task LoadAsync()
        {
            ApiResponse claims = await Api.GetAsync("/v1/admin/claims");
            ApiResponse edits = await Api.GetAsync("/v1/admin/edit-requests");
            ApiResponse photos = await Api.GetAsync("/v1/admin/photos");
            if (!Check(claims) || !Check(edits) || !Check(photos)) return;

            items[0] = Rows(claims).Select(d => new ListItem
            {
                Title = Str(d, "poiName"), Subtitle = "@" + Str(d, "username") + " xin quản lý địa điểm này\nGửi " + AccountKit.Day(Str(d, "createdAt")), Icon = "verified_user", Pill = "Chờ duyệt", Tone = PillTone.Warn, Data = d,
            }).ToList();
            items[1] = Rows(edits).Select(d => new ListItem
            {
                Title = Str(d, "poiName"), Subtitle = "@" + Str(d, "requestedBy") + " đề nghị sửa " + (JsonHelper.Obj(d, "changes") == null ? 0 : JsonHelper.Obj(d, "changes").Count) + " mục\nGửi " + AccountKit.Day(Str(d, "createdAt")), Icon = "check_box", Pill = "Chờ duyệt", Tone = PillTone.Warn, Data = d,
            }).ToList();
            items[2] = Rows(photos).Select(d => new ListItem
            {
                Title = Str(d, "poiName"), Subtitle = "@" + Str(d, "uploadedBy") + " tải ảnh " + JsonHelper.Long(d, "width") + " x " + JsonHelper.Long(d, "height") + "\nGửi " + AccountKit.Day(Str(d, "createdAt")), Icon = "visibility", Pill = "Chờ duyệt", Tone = PillTone.Warn, Data = d,
            }).ToList();
            for (int i = 0; i < 3; i++) tabs.SetCount(i, items[i].Count);
            ShowList();
        }

        private void ShowList()
        {
            list.SetItems(items[Math.Max(0, tabs.SelectedIndex)]);
            ListPane.Relayout();
            ShowNothing(Nothing);
        }

        private Task ShowAsync(ListItem item)
        {
            var d = (IDictionary<string, object>)item.Data;
            switch (tabs.SelectedIndex)
            {
                case 0: ShowClaim(d); return Task.CompletedTask;
                case 1: ShowEdit(d); return Task.CompletedTask;
                default: return ShowPhotoAsync(d);
            }
        }

        // ------------------------------------------------------------------ claims

        private void ShowClaim(IDictionary<string, object> d)
        {
            DetailPane.Clear();
            DetailPane.Caption(Str(d, "poiName"));
            DetailPane.Hint("@" + Str(d, "username") + " xin được quản lý địa điểm này.\nGửi " + AccountKit.Day(Str(d, "createdAt")));
            DetailPane.Hint("Hệ thống không tự kiểm tra người này có phải chủ địa điểm hay không. Hãy xác minh ngoài hệ thống (gọi điện thoại của địa điểm, xem giấy tờ kinh doanh) rồi mới duyệt. Duyệt xong, tài khoản này trở thành chủ quản và sửa được thông tin, ảnh, thông báo của địa điểm.");
            DetailPane.Divider();
            AddDecision(approve => DecideClaimAsync(d, approve), "Duyệt làm chủ quản");
        }

        private async Task DecideClaimAsync(IDictionary<string, object> d, bool approve)
        {
            string note = approve ? null : AskNote("Từ chối yêu cầu nhận địa điểm");
            if (!approve && note == null) return;
            var body = new Dictionary<string, object> { { "approve", approve }, { "note", note } };
            if (Check(await Api.PostAsync("/v1/admin/claims/" + JsonHelper.Long(d, "userId") + "/" + Uri.EscapeDataString(Str(d, "poiId")) + "/decision", body), approve ? "Đã duyệt" : "Đã từ chối")) await Reload();
        }

        // ------------------------------------------------------------------ edit requests

        private void ShowEdit(IDictionary<string, object> d)
        {
            Poi now = PoiCatalog.All.FirstOrDefault(p => p.Id == Str(d, "poiId"));
            DetailPane.Clear();
            DetailPane.Caption(Str(d, "poiName"));
            DetailPane.Hint("@" + Str(d, "requestedBy") + " đề nghị thay đổi:\nGửi " + AccountKit.Day(Str(d, "createdAt")));
            foreach (KeyValuePair<string, object> change in JsonHelper.Obj(d, "changes") ?? new Dictionary<string, object>())
            {
                string label = FieldNames.ContainsKey(change.Key) ? FieldNames[change.Key] : change.Key;
                string before = now == null ? "" : Current(now, change.Key);
                DetailPane.Hint(label + "\n  Hiện tại: " + (string.IsNullOrWhiteSpace(before) ? "(trống)" : before) + "\n  Đề nghị:  " + Convert.ToString(change.Value), 6);
            }
            DetailPane.Divider();
            AddDecision(approve => DecideEditAsync(d, approve), "Duyệt và áp dụng");
        }

        private static string Current(Poi p, string column)
        {
            switch (column)
            {
                case "dia_chi": return p.Address;
                case "gio_mo_cua": return p.OpeningHours;
                case "gia_text": return p.TicketPrice;
                case "sdt": return p.Phone;
                case "website": return p.Website;
                case "mo_ta": return p.Summary;
                default: return "";
            }
        }

        private async Task DecideEditAsync(IDictionary<string, object> d, bool approve)
        {
            string note = approve ? null : AskNote("Từ chối chỉnh sửa");
            if (!approve && note == null) return;
            var body = new Dictionary<string, object> { { "approve", approve }, { "note", note } };
            if (Check(await Api.PostAsync("/v1/admin/edit-requests/" + JsonHelper.Long(d, "id") + "/decision", body), approve ? "Đã duyệt và áp dụng thay đổi" : "Đã từ chối"))
            {
                await Reload();
                if (approve) RaisePlacesChanged();
            }
        }

        // ------------------------------------------------------------------ photos

        private async Task ShowPhotoAsync(IDictionary<string, object> d)
        {
            int version = ++loadVersion;
            DetailPane.Clear();
            DetailPane.Caption(Str(d, "poiName"));
            DetailPane.Hint("@" + Str(d, "uploadedBy") + " tải lên ảnh " + JsonHelper.Long(d, "width") + " x " + JsonHelper.Long(d, "height") + ".\nGửi " + AccountKit.Day(Str(d, "createdAt")));
            var preview = new PreviewBox();
            DetailPane.Add(preview, width => preview.HeightFor(width), 12);
            DetailPane.Hint("Duyệt thì ảnh hiện cho mọi người ở thẻ thông tin địa điểm. Từ chối nếu ảnh mờ, sai địa điểm, có hình không phù hợp hoặc vi phạm bản quyền.");
            DetailPane.Divider();
            AddDecision(approve => DecidePhotoAsync(d, approve), "Duyệt ảnh");
            DetailPane.Relayout();

            Bitmap image = await PhotoSync.LoadAsync(Api, JsonHelper.Long(d, "id"), System.Threading.CancellationToken.None);
            if (version != loadVersion || preview.IsDisposed) { image?.Dispose(); return; }
            preview.SetImage(image);
            DetailPane.Relayout();
        }

        private async Task DecidePhotoAsync(IDictionary<string, object> d, bool approve)
        {
            string note = approve ? null : AskNote("Từ chối ảnh");
            if (!approve && note == null) return;
            var body = new Dictionary<string, object> { { "approve", approve }, { "note", note } };
            if (Check(await Api.PostAsync("/v1/admin/photos/" + JsonHelper.Long(d, "id") + "/decision", body), approve ? "Đã duyệt ảnh" : "Đã từ chối ảnh")) await Reload();
        }

        // ------------------------------------------------------------------ shared

        private void AddDecision(Func<bool, Task> decide, string approveText)
        {
            IconButton approve = AccountKit.Primary(approveText, "check");
            IconButton reject = AccountKit.Danger("Từ chối", "close");
            DetailPane.Buttons(approve, reject);
            approve.Click += async (s, e) => await decide(true);
            reject.Click += async (s, e) => await decide(false);
            DetailPane.Relayout();
        }

        // The reason is optional but asked for: the person who sent the request sees it.
        private string AskNote(string title)
        {
            string[] v = Dialogs.Ask(Window, title, "Lý do (không bắt buộc) giúp người gửi hiểu vì sao bị từ chối.", "Từ chối", new DialogField("Lý do", "", 3));
            return v == null ? null : v[0].Trim();
        }

        private async Task Reload()
        {
            RaiseChanged();
            await LoadAsync();
        }
    }
}
