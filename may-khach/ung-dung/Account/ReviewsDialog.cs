using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Hue_travel.Models;
using Hue_travel.Services.Online;
using Hue_travel.Theme;

namespace Hue_travel.Account
{
    /// <summary>
    /// The reviews of one place in a window over the map: the average, what people wrote (with the owner's answers) and, for a
    /// signed-in person, stars and a comment of their own to send, change or delete.
    /// </summary>
    internal sealed class ReviewsDialog : ModalCard
    {
        private const int DialogWidth = 520;
        private const int ListRows = 3;
        private readonly ApiClient api;
        private readonly Poi poi;
        private readonly bool signedIn;
        private readonly Label summary = new Label { AutoSize = false, Font = ThemeFonts.SemiBold(13), ForeColor = ThemeColors.Slate700, TextAlign = ContentAlignment.MiddleLeft };
        private readonly StarRating average = new StarRating { StarSize = 20, ReadOnly = true };
        private readonly ItemList list = new ItemList { SubtitleLines = 3, EmptyText = "Chưa có đánh giá nào. Hãy là người đầu tiên." };
        private readonly NoAutoScrollPanel scroll = new NoAutoScrollPanel { AutoScroll = true, BackColor = Color.White };
        private readonly StarRating mine = new StarRating { StarSize = 30 };
        private readonly InputField comment = new InputField { IsMultiline = true, PlaceholderText = "Bạn thấy địa điểm này thế nào? (không bắt buộc)" };
        private readonly IconButton delete = AccountKit.Danger("Xóa đánh giá của tôi", "close");
        private readonly Label status = new Label { AutoSize = false, Font = ThemeFonts.Medium(12), ForeColor = ThemeColors.Slate500, TextAlign = ContentAlignment.MiddleLeft };
        private bool hasMine;
        private bool busy;

        private ReviewsDialog(ApiClient api, Poi poi, bool signedIn) : base("Đánh giá: " + Shorten(poi.Name), DialogWidth, signedIn ? "Gửi đánh giá" : "Đóng", false, signedIn)
        {
            this.api = api;
            this.poi = poi;
            this.signedIn = signedIn;
            if (signedIn) Cancel.Text = "Đóng";

            int inner = DialogWidth - Side * 2;
            int y = 0;
            average.SetBounds(0, Ui.S(y + 4), average.WantedWidth, Ui.S(24));
            summary.SetBounds(average.Width + Ui.S(10), Ui.S(y), Ui.S(inner) - average.Width - Ui.S(10), Ui.S(28));
            y += 36;
            scroll.SetBounds(0, Ui.S(y), Ui.S(inner), Ui.S(ListRows * 79 + 2));
            scroll.Controls.Add(list);
            list.SetBounds(0, 0, scroll.ClientSize.Width, list.WantedHeight);
            y += ListRows * 79 + 12;
            Content.Controls.Add(average);
            Content.Controls.Add(summary);
            Content.Controls.Add(scroll);

            if (signedIn)
            {
                var divider = AccountKit.Divider();
                divider.SetBounds(0, Ui.S(y), Ui.S(inner), 1);
                Content.Controls.Add(divider);
                y += 12;
                var label = AccountKit.FieldLabel("Đánh giá của bạn");
                label.SetBounds(0, Ui.S(y), Ui.S(inner), Ui.S(24));
                Content.Controls.Add(label);
                y += 28;
                mine.SetBounds(0, Ui.S(y), mine.WantedWidth, Ui.S(32));
                delete.SetBounds(Ui.S(inner) - Ui.S(190), Ui.S(y - 2), Ui.S(190), Ui.S(36));
                delete.Visible = false;
                Content.Controls.Add(mine);
                Content.Controls.Add(delete);
                y += 40;
                comment.SetBounds(0, Ui.S(y), Ui.S(inner), Ui.S(84));
                Content.Controls.Add(comment);
                y += 90;
                status.SetBounds(0, Ui.S(y), Ui.S(inner), Ui.S(22));
                Content.Controls.Add(status);
                y += 22;
                delete.Click += async (s, e) => await DeleteAsync();
            }
            else
            {
                var hint = AccountKit.Hint("Đăng nhập để viết đánh giá của bạn.");
                hint.SetBounds(0, Ui.S(y), Ui.S(inner), Ui.S(22));
                Content.Controls.Add(hint);
                y += 22;
            }
            Arrange(y);
            Shown += async (s, e) => await LoadAsync();
        }

        private static string Shorten(string name) => name != null && name.Length > 34 ? name.Substring(0, 33) + "…" : name;

        /// <summary>Shows the reviews of a place; returns when the window is closed.</summary>
        public static void ShowFor(IWin32Window owner, ApiClient api, Poi poi, bool signedIn)
        {
            using (var dialog = new ReviewsDialog(api, poi, signedIn)) dialog.ShowDialog(owner);
        }

        private string Path => "/v1/pois/" + Uri.EscapeDataString(poi.Id) + "/reviews";

        private async Task LoadAsync()
        {
            ApiResponse r = await api.GetAsync(Path);
            if (IsDisposed) return;
            if (!r.IsSuccess) { summary.Text = r.Error; return; }
            long count = JsonHelper.Long(r.Object, "count");
            double avg = JsonHelper.Dbl(r.Object, "average");
            average.Value = (int)Math.Round(avg);
            summary.Text = count == 0 ? "Chưa có đánh giá" : avg.ToString("0.0") + " / 5 · " + count + " đánh giá";

            var items = new List<ListItem>();
            hasMine = false;
            foreach (IDictionary<string, object> d in (JsonHelper.List(r.Object, "items") ?? new List<object>()).OfType<IDictionary<string, object>>())
            {
                bool own = JsonHelper.Bool(d, "mine");
                string text = JsonHelper.Str(d, "comment");
                string reply = JsonHelper.Str(d, "ownerReply");
                items.Add(new ListItem
                {
                    Title = JsonHelper.Str(d, "author") + (own ? " (bạn)" : ""), Pill = JsonHelper.Long(d, "rating") + " sao", Tone = own ? PillTone.Info : PillTone.Warn,
                    Subtitle = (string.IsNullOrWhiteSpace(text) ? "(không có nhận xét)" : text) + (string.IsNullOrWhiteSpace(reply) ? "" : "\nChủ quản trả lời: " + reply),
                });
                if (own && signedIn)
                {
                    hasMine = true;
                    mine.Value = (int)JsonHelper.Long(d, "rating");
                    comment.Text = text ?? "";
                }
            }
            list.SetItems(items);
            list.Height = list.WantedHeight;
            scroll.AutoScrollMinSize = new Size(0, list.Height);
            if (signedIn)
            {
                delete.Visible = hasMine;
                Ok.Text = hasMine ? "Cập nhật đánh giá" : "Gửi đánh giá";
            }
        }

        protected override bool CanAccept()
        {
            if (!signedIn) return true;   // "Đóng"
            var ignored = SubmitAsync();
            return false;
        }

        private async Task SubmitAsync()
        {
            if (busy) return;
            if (mine.Value < 1) { status.ForeColor = ThemeColors.Red500; status.Text = "Hãy chọn số sao trước."; return; }
            busy = true;
            status.ForeColor = ThemeColors.Slate500;
            status.Text = "Đang gửi…";
            ApiResponse r = await api.PostAsync(Path, new Dictionary<string, object> { { "rating", mine.Value }, { "comment", comment.Text.Trim() } });
            busy = false;
            if (IsDisposed) return;
            if (!r.IsSuccess) { status.ForeColor = ThemeColors.Red500; status.Text = r.Error; return; }
            status.ForeColor = ThemeColors.Emerald700;
            status.Text = "Đã lưu đánh giá của bạn. Cảm ơn bạn!";
            await LoadAsync();
        }

        private async Task DeleteAsync()
        {
            if (busy) return;
            busy = true;
            ApiResponse r = await api.DeleteAsync(Path);
            busy = false;
            if (IsDisposed) return;
            if (!r.IsSuccess) { status.ForeColor = ThemeColors.Red500; status.Text = r.Error; return; }
            mine.Value = 0;
            comment.Text = "";
            status.ForeColor = ThemeColors.Slate500;
            status.Text = "Đã xóa đánh giá của bạn.";
            await LoadAsync();
        }
    }
}
