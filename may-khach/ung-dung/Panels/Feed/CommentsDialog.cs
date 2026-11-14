using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Hue_travel.Account;
using Hue_travel.Services.Online;
using Hue_travel.Theme;

namespace Hue_travel.Panels.Feed
{
    /// <summary>The comments of one post in a window over the map; a signed-in person can add one, or delete their own by pressing it.</summary>
    internal sealed class CommentsDialog : ModalCard
    {
        private const int DialogWidth = 520;
        private const int ListRows = 4;
        private readonly ApiFeedService feed;
        private readonly FeedPostInfo post;
        private readonly bool signedIn;
        private readonly CancellationTokenSource closing = new CancellationTokenSource();
        private readonly ItemList list = new ItemList { SubtitleLines = 3, EmptyText = "Chưa có bình luận nào. Hãy là người đầu tiên." };
        private readonly NoAutoScrollPanel scroll = new NoAutoScrollPanel { AutoScroll = true, BackColor = Color.White };
        private readonly InputField input = new InputField { IsMultiline = true, PlaceholderText = "Viết bình luận…" };
        private readonly Label status = new Label { AutoSize = false, Font = ThemeFonts.Medium(12), ForeColor = ThemeColors.Slate500, TextAlign = ContentAlignment.MiddleLeft };
        private bool busy;

        /// <summary>True when a comment was added or deleted, so the feed can refresh its count.</summary>
        public bool Changed { get; private set; }

        private CommentsDialog(ApiFeedService feed, FeedPostInfo post, bool signedIn)
            : base("Bình luận", DialogWidth, signedIn ? "Gửi" : "Đóng", false, signedIn)
        {
            this.feed = feed;
            this.post = post;
            this.signedIn = signedIn;
            if (signedIn) Cancel.Text = "Đóng";

            int inner = DialogWidth - Side * 2, y = 0;
            scroll.SetBounds(0, Ui.S(y), Ui.S(inner), Ui.S(ListRows * 79 + 2));
            scroll.Controls.Add(list);
            list.SetBounds(0, 0, scroll.ClientSize.Width, list.WantedHeight);
            Content.Controls.Add(scroll);
            y += ListRows * 79 + 10;

            if (signedIn)
            {
                input.SetBounds(0, Ui.S(y), Ui.S(inner), Ui.S(84));
                Content.Controls.Add(input);
                y += 90;
            }
            status.SetBounds(0, Ui.S(y), Ui.S(inner), Ui.S(22));
            status.Text = signedIn ? "Bấm vào bình luận của bạn để xóa nó." : "Đăng nhập để bình luận.";
            Content.Controls.Add(status);
            y += 22;
            list.ItemClicked += async (s, item) => await DeleteIfMineAsync(item);
            Arrange(y);
            Shown += async (s, e) => await LoadAsync();
            FormClosed += (s, e) => closing.Cancel();
        }

        public static bool ShowFor(IWin32Window owner, ApiFeedService feed, FeedPostInfo post, bool signedIn)
        {
            using (var dialog = new CommentsDialog(feed, post, signedIn))
            {
                dialog.ShowDialog(owner);
                return dialog.Changed;
            }
        }

        private void Show(string text, bool error)
        {
            status.ForeColor = error ? ThemeColors.Red500 : ThemeColors.Slate500;
            status.Text = text;
        }

        private async Task LoadAsync()
        {
            ApiResult<List<FeedCommentInfo>> r;
            try { r = await feed.CommentsAsync(post.Id, closing.Token); }
            catch (OperationCanceledException) { return; }
            if (IsDisposed) return;
            if (!r.Ok) { Show(r.Error, true); return; }
            list.SetItems(r.Value.Select(c => new ListItem
            {
                Title = c.AuthorName + (c.Mine ? " (bạn)" : "") + " · " + FeedPostInfo.TimeAgo(c.CreatedAt, DateTimeOffset.Now),
                Subtitle = c.Body, Tone = c.Mine ? PillTone.Info : PillTone.Warn, Data = c.Mine ? (object)c.Id : null,
            }));
            list.Height = list.WantedHeight;
            scroll.AutoScrollMinSize = new Size(0, list.Height);
        }

        protected override bool CanAccept()
        {
            if (!signedIn) return true;   // "Đóng"
            var ignored = SendAsync();
            return false;
        }

        private async Task SendAsync()
        {
            string text = input.Text.Trim();
            if (busy || text.Length == 0) { if (text.Length == 0) Show("Hãy viết nội dung bình luận.", true); return; }
            busy = true;
            Show("Đang gửi…", false);
            string error;
            try { error = await feed.AddCommentAsync(post.Id, text, closing.Token); }
            catch (OperationCanceledException) { return; }
            busy = false;
            if (IsDisposed) return;
            if (error != null) { Show(error, true); return; }
            input.Text = "";
            Changed = true;
            Show("Đã gửi bình luận.", false);
            await LoadAsync();
        }

        private async Task DeleteIfMineAsync(ListItem item)
        {
            if (busy || !signedIn || !(item.Data is long)) return;
            if (!Dialogs.Confirm(this, "Xóa bình luận", "Xóa bình luận này của bạn?", "Xóa", true)) return;
            busy = true;
            string error;
            try { error = await feed.DeleteCommentAsync((long)item.Data, closing.Token); }
            catch (OperationCanceledException) { return; }
            busy = false;
            if (IsDisposed) return;
            if (error != null) { Show(error, true); return; }
            Changed = true;
            Show("Đã xóa bình luận.", false);
            await LoadAsync();
        }
    }
}
