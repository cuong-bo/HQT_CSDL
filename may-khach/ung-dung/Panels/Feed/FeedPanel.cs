using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Hue_travel.Account;
using Hue_travel.Models;
using Hue_travel.Panels.PoiIdentify;
using Hue_travel.Services.Online;
using Hue_travel.Theme;

namespace Hue_travel.Panels.Feed
{
    /// <summary>
    /// "Khoảnh khắc": the community feed in the side slot. Anyone can read; liking, commenting, posting and reporting need an account
    /// (the host's <see cref="SignInGate"/> asks a guest to sign in first). When the server has the feature switched off, the panel says so.
    /// </summary>
    public sealed class FeedPanel : FloatingPanelForm
    {
        private readonly ApiFeedService feed;
        private readonly PanelHeader header = new PanelHeader { IconName = "moments", Text = "Khoảnh khắc", Prominent = true };
        private readonly Panel bar = new Panel { Dock = DockStyle.Top, BackColor = Color.White };
        private readonly IconButton btnPost = AccountKit.Primary("Chia sẻ khoảnh khắc", "add");
        private readonly IconButton btnRefresh = AccountKit.Secondary("", "refresh");
        private readonly Label lblNotice = new Label { Dock = DockStyle.Top, AutoSize = false, Height = 0, ForeColor = ThemeColors.Slate600, Padding = new Padding(16, 6, 16, 6), BackColor = Color.White };
        private readonly FeedView view = new FeedView { Dock = DockStyle.Fill };
        private readonly Label lblNew = new Label { AutoSize = false, Visible = false, TextAlign = ContentAlignment.MiddleCenter, BackColor = ThemeColors.Primary, ForeColor = Color.White, Cursor = Cursors.Hand };
        private readonly System.Windows.Forms.Timer newTimer = new System.Windows.Forms.Timer { Interval = 60000 };
        private CancellationTokenSource loading = new CancellationTokenSource();
        private string next;
        private bool busy, opened, disabled;
        private FeedMe me;

        /// <summary>Runs an action for a signed-in person: a guest is asked to sign in first (set by the main window).</summary>
        public Func<string, Action, bool> SignInGate { get; set; }

        /// <summary>The ids of the route open in the route form (for "đính kèm lộ trình"), or null.</summary>
        public Func<IReadOnlyList<string>> CurrentRoute { get; set; }

        /// <summary>The place open on the map, offered as the place of a new post, or null.</summary>
        public Func<Poi> CurrentPlace { get; set; }

        /// <summary>Whether somebody is signed in (asked without prompting, e.g. to decide if the comment box shows).</summary>
        public Func<bool> IsSignedIn { get; set; }

        public event EventHandler<string> PlaceChosen;
        public event EventHandler<IReadOnlyList<string>> RouteChosen;

        public FeedPanel(ApiFeedService feed)
        {
            this.feed = feed;
            Size = new Size(440, 640);
            header.CloseClicked += (s, e) => RaiseCloseRequested();

            btnRefresh.AutoWidth = false;
            lblNotice.Font = ThemeFonts.Regular(12);
            lblNew.Font = ThemeFonts.SemiBold(12);
            lblNew.Click += async (s, e) => await ReloadAsync();
            bar.Controls.Add(btnPost);
            bar.Controls.Add(btnRefresh);
            bar.Height = Ui.S(64);
            bar.Resize += (s, e) => LayoutBar();
            LayoutBar();

            view.Controls.Add(lblNew);
            Controls.Add(view);
            Controls.Add(lblNotice);
            Controls.Add(bar);
            Controls.Add(header);

            btnPost.Click += (s, e) => NewPost();
            btnRefresh.Click += async (s, e) => { filterPoiId = filterPoiName = null; await ReloadAsync(); };
            view.EndReached += async (s, e) => await LoadMoreAsync();
            view.ThumbNeeded += id => { var ignored = LoadThumbAsync(id); };
            view.LikeClicked += (s, post) => Gate("Đăng nhập để thích bài viết.", () => { var ignored = ToggleLikeAsync(post); });
            view.CommentsClicked += (s, post) => OpenComments(post);
            view.MoreClicked += (s, post) => ShowMenu(post);
            view.PlaceClicked += (s, id) => PlaceChosen?.Invoke(this, id);
            view.RouteClicked += (s, post) => RouteChosen?.Invoke(this, post.Route.Select(r => r.Id).ToList());
            view.PhotoClicked += (s, click) => { var ignored = OpenPhotoAsync(click.Post, click.Index); };
            view.Resize += (s, e) => LayoutNewChip();
            newTimer.Tick += async (s, e) => await CheckNewAsync();
            VisibleChanged += async (s, e) =>
            {
                if (!Visible) { newTimer.Stop(); return; }
                newTimer.Start();
                if (!opened) { opened = true; await ReloadAsync(); }
            };
        }

        // A reply that arrives after the window was closed must not touch its controls.
        private bool Gone => IsDisposed || !IsHandleCreated;

        private void LayoutBar()
        {
            int pad = Ui.S(12), h = Ui.S(40), y = (bar.ClientSize.Height - h) / 2;
            btnRefresh.SetBounds(bar.ClientSize.Width - pad - h, y, h, h);
            btnPost.SetBounds(pad, y, Math.Max(Ui.S(120), btnRefresh.Left - Ui.S(8) - pad), h);
        }

        private void LayoutNewChip() => lblNew.SetBounds((view.Width - Ui.S(190)) / 2, Ui.S(8), Ui.S(190), Ui.S(32));

        private void Gate(string reason, Action action)
        {
            if (SignInGate == null) action();
            else SignInGate(reason, action);
        }

        private void SetNotice(string text)
        {
            lblNotice.Text = text ?? "";
            lblNotice.Height = string.IsNullOrEmpty(text) ? 0 : Ui.S(52);
        }

        // ------------------------------------------------------------------ loading

        private async Task ReloadAsync()
        {
            loading.Cancel();
            loading = new CancellationTokenSource();
            next = null;
            disabled = false;
            busy = false;          // a request still running belongs to the old token; its answer is ignored
            view.Clear();
            lblNew.Visible = false;
            await LoadMoreAsync();
        }

        private string filterPoiId, filterPoiName;

        private string FilterNotice => filterPoiId == null ? null : "Đang xem khoảnh khắc ở: " + filterPoiName + ". Bấm nút làm mới bên phải để xem tất cả.";

        /// <summary>Shows only the posts about one place (from the place card). The refresh button goes back to all posts.</summary>
        public void ShowPlace(Poi poi)
        {
            filterPoiId = poi.Id;
            filterPoiName = poi.Name;
            opened = true;
            var ignored = ReloadAsync();
        }

        private async Task LoadMoreAsync()
        {
            if (busy || disabled || (view.Posts.Count > 0 && next == null)) return;
            busy = true;
            CancellationToken ct = loading.Token;
            view.Footer = "Đang tải…";
            ApiResult<FeedPage> r;
            try { r = await feed.ListAsync(next, filterPoiId, ct); }
            catch (OperationCanceledException) { if (ct == loading.Token) busy = false; return; }
            if (ct != loading.Token || Gone) return;   // a newer reload took over
            busy = false;
            if (!r.Ok)
            {
                disabled = true;   // switched off or no server: no point asking again until the person presses refresh
                view.Footer = "";
                SetNotice(r.Error);
                return;
            }
            SetNotice(FilterNotice);
            view.Append(r.Value.Items);
            next = r.Value.Next;
            view.Footer = view.Posts.Count == 0 ? (filterPoiId == null ? "Chưa có khoảnh khắc nào. Hãy là người đầu tiên chia sẻ!" : "Chưa có khoảnh khắc nào ở địa điểm này.") : next == null ? "Đã xem hết" : "";
        }

        private async Task CheckNewAsync()
        {
            if (view.Posts.Count == 0 || disabled || busy) return;
            long newest = view.Posts.Max(p => p.Id);
            int count;
            try { count = await feed.NewCountAsync(newest, loading.Token); }
            catch (OperationCanceledException) { return; }
            if (Gone || count <= 0) return;
            lblNew.Text = count + " bài mới · bấm để xem";
            LayoutNewChip();
            lblNew.Visible = true;
            lblNew.BringToFront();
        }

        private async Task LoadThumbAsync(long id)
        {
            byte[] bytes;
            try { bytes = await feed.MediaAsync(id, true, loading.Token); }
            catch (OperationCanceledException) { return; }
            if (!Gone) view.SetThumb(id, bytes);
        }

        // ------------------------------------------------------------------ actions

        private void NewPost()
        {
            Gate("Đăng nhập để chia sẻ khoảnh khắc của bạn.", () => { var ignored = ComposeAsync(); });
        }

        private async Task ComposeAsync()
        {
            ApiResult<FeedMe> status = await feed.MeAsync(CancellationToken.None);
            if (Gone) return;
            if (!status.Ok) { Notify(status.Error, ToastKind.Error); return; }
            me = status.Value;
            IReadOnlyList<string> route = CurrentRoute == null ? null : CurrentRoute();
            string result = ComposerDialog.ShowFor(this, feed, me, route, CurrentPlace == null ? null : CurrentPlace());
            if (result == null) return;
            Notify(result == "pending" ? "Bài của bạn đã gửi và đang chờ quản trị viên duyệt." : "Đã đăng khoảnh khắc của bạn.", ToastKind.Success);
            await ReloadAsync();
        }

        private async Task ToggleLikeAsync(FeedPostInfo post)
        {
            bool like = !post.LikedByMe;
            FeedPostInfo shown = With(post, like);
            view.Replace(post, shown);                      // at once; undone below if the server refuses
            string error = await feed.LikeAsync(post.Id, like, CancellationToken.None);
            if (error == null || Gone) return;
            view.Replace(shown, post);
            Notify(error, ToastKind.Error);
        }

        private static FeedPostInfo With(FeedPostInfo post, bool liked) => new FeedPostInfo
        {
            Id = post.Id, CreatedAt = post.CreatedAt, AuthorId = post.AuthorId, AuthorName = post.AuthorName, Body = post.Body, Poi = post.Poi, Route = post.Route,
            MediaIds = post.MediaIds, CommentCount = post.CommentCount, Mine = post.Mine, Status = post.Status,
            LikedByMe = liked, LikeCount = Math.Max(0, post.LikeCount + (liked ? 1 : -1)),
        };

        private void OpenComments(FeedPostInfo post)
        {
            bool signedIn = IsSignedIn == null || IsSignedIn();   // a guest reads the comments; the box to write one needs an account
            bool changed = CommentsDialog.ShowFor(this, feed, post, signedIn);
            if (changed) { var ignored = RefreshPostAsync(post); }
        }

        private async Task RefreshPostAsync(FeedPostInfo post)
        {
            // a comment was added or deleted: the simplest correct thing is to reload the list
            await ReloadAsync();
        }

        private void ShowMenu(FeedPostInfo post)
        {
            var menu = new ContextMenuStrip { Font = ThemeFonts.Regular(12) };
            if (post.Mine) menu.Items.Add("Xóa bài viết", null, (s, e) => DeletePost(post));
            else menu.Items.Add("Báo cáo bài viết", null, (s, e) => Gate("Đăng nhập để báo cáo bài viết.", () => ReportPost(post)));
            menu.Closed += (s, e) => menu.BeginInvoke(new Action(menu.Dispose));
            menu.Show(Cursor.Position);
        }

        private void DeletePost(FeedPostInfo post)
        {
            if (!Dialogs.Confirm(this, "Xóa bài viết", "Xóa bài viết này? Không khôi phục được.", "Xóa", true)) return;
            var ignored = DeleteAsync(post);
        }

        private async Task DeleteAsync(FeedPostInfo post)
        {
            string error = await feed.DeletePostAsync(post.Id, CancellationToken.None);
            if (Gone) return;
            if (error != null) { Notify(error, ToastKind.Error); return; }
            view.Remove(post);
            Notify("Đã xóa bài viết.", ToastKind.Success);
        }

        private void ReportPost(FeedPostInfo post)
        {
            var rows = ApiFeedService.ReportKindNames.Select(n => new ListItem { Title = n }).ToList();
            int index = Dialogs.Pick(this, "Báo cáo bài viết", rows, "Tiếp tục");
            if (index < 0) return;
            string[] values = Dialogs.Ask(this, "Báo cáo bài viết", "Mô tả thêm (không bắt buộc).", "Gửi báo cáo", new DialogField("Chi tiết", "", 3));
            if (values == null) return;
            var ignored = ReportAsync(post, ApiFeedService.ReportKinds[index], values[0].Trim());
        }

        private async Task ReportAsync(FeedPostInfo post, string kind, string note)
        {
            string error = await feed.ReportPostAsync(post.Id, kind, string.IsNullOrEmpty(note) ? null : note, CancellationToken.None);
            if (Gone) return;
            Notify(error ?? "Đã gửi báo cáo. Cảm ơn bạn!", error == null ? ToastKind.Success : ToastKind.Error);
        }

        private async Task OpenPhotoAsync(FeedPostInfo post, int index)
        {
            byte[] bytes;
            try { bytes = await feed.MediaAsync(post.MediaIds[index], false, loading.Token); }
            catch (OperationCanceledException) { return; }
            if (Gone) return;
            if (bytes == null) { Notify("Không tải được ảnh.", ToastKind.Error); return; }
            string path = Path.Combine(Path.GetTempPath(), "hue-travel-moment-" + post.MediaIds[index] + ".jpg");
            try { File.WriteAllBytes(path, bytes); }
            catch (IOException) { Notify("Không mở được ảnh.", ToastKind.Error); return; }
            catch (UnauthorizedAccessException) { Notify("Không mở được ảnh.", ToastKind.Error); return; }
            PhotoViewer.ShowOver(FindForm() ?? this, new[] { path }, 0, post.AuthorName);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                loading.Cancel();
                newTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
