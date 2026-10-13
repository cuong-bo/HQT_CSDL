using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Hue_travel.Account;
using Hue_travel.Services.Online;
using Hue_travel.Theme;

namespace Hue_travel.Admin
{
    /// <summary>"Tổng quan": the numbers an admin looks at first; a row opens the page that deals with it.</summary>
    internal sealed class OverviewSection : UserControl
    {
        private readonly ApiClient api;
        private readonly VStack stack = new VStack { Dock = DockStyle.Fill };
        private readonly ItemList list = new ItemList { Selectable = false, SubtitleLines = 1, EmptyText = "Đang tải số liệu…" };

        public OverviewSection(ApiClient api)
        {
            this.api = api;
            Dock = DockStyle.Fill;
            BackColor = Color.White;
            stack.Caption("Tổng quan");
            stack.Hint("Việc đang chờ bạn xử lý và quy mô hiện tại của hệ thống. Bấm một dòng để mở mục tương ứng.");
            stack.List(list);
            Controls.Add(stack);
            list.ItemClicked += (s, item) => Navigate?.Invoke(this, (string)item.Data);
        }

        /// <summary>Raised with the key of the page to open.</summary>
        public event EventHandler<string> Navigate;

        public event EventHandler<ToastMessage> MessageRaised;

        /// <summary>Raised with the number of things waiting for a decision, for the menu.</summary>
        public event EventHandler<IDictionary<string, long>> Counted;

        public async Task LoadAsync()
        {
            ApiResponse r = await api.GetAsync("/v1/admin/stats");
            if (!r.IsSuccess) { MessageRaised?.Invoke(this, new ToastMessage(r.Error, ToastKind.Error)); return; }
            IDictionary<string, object> d = r.Object;
            long claims = JsonHelper.Long(d, "pendingClaims"), edits = JsonHelper.Long(d, "pendingEditRequests"), photos = JsonHelper.Long(d, "pendingPhotos"), reports = JsonHelper.Long(d, "openReports");
            list.SetItems(new List<ListItem>
            {
                Row("Yêu cầu nhận địa điểm", claims, "verified_user", "approvals", true),
                Row("Đề nghị chỉnh sửa địa điểm", edits, "check_box", "approvals", true),
                Row("Ảnh chờ duyệt", photos, "visibility", "approvals", true),
                Row("Báo sai thông tin chưa xử lý", reports, "campaign", "moderation", true),
                Row("Tài khoản", JsonHelper.Long(d, "users"), "account_circle", "users", false),
                Row("Địa điểm", JsonHelper.Long(d, "places"), "pin_drop", "places", false),
                Row("Đánh giá", JsonHelper.Long(d, "reviews"), "star", "moderation", false),
            });
            stack.Relayout();
            Counted?.Invoke(this, new Dictionary<string, long> { { "approvals", claims + edits + photos }, { "moderation", reports } });
        }

        private static ListItem Row(string title, long count, string icon, string page, bool isTodo)
        {
            return new ListItem
            {
                Title = title, Subtitle = isTodo ? (count == 0 ? "Không có gì đang chờ" : "Cần xử lý") : "Tổng số hiện có", Icon = icon, Data = page,
                Pill = count.ToString(), Tone = !isTodo ? PillTone.Neutral : count == 0 ? PillTone.Good : PillTone.Warn,
            };
        }
    }

    /// <summary>
    /// The admin console: a window of its own with a menu on the left (overview, waiting for approval, users, places, moderation,
    /// log) and the chosen page on the right. Nothing here is on the map screen, so an ordinary user never carries any of it.
    /// </summary>
    internal sealed class AdminConsole : Form
    {
        private sealed class Page
        {
            public string Key, Title, Icon;
            public Control View;
            public Func<Task> Load;
        }

        private readonly ApiClient api;
        private readonly List<Page> pages = new List<Page>();
        private readonly ItemList menu = new ItemList { Selectable = true, EmptyText = "" };
        private readonly Panel body = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        private readonly Toast toast = new Toast { TopOffset = 16 };
        private Page current;

        public AdminConsole(ApiClient api, UserInfo admin)
        {
            this.api = api;
            Text = "Bảng quản trị — Cố Đô GIS";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(Ui.S(1180), Ui.S(740));
            MinimumSize = new Size(Ui.S(900), Ui.S(600));
            BackColor = Color.White;
            Font = ThemeFonts.Regular(11);
            ShowInTaskbar = false;

            var overview = new OverviewSection(api);
            var approvals = new ApprovalsSection(api);
            var users = new UsersSection(api, admin.Id);
            var places = new PlacesSection(api);
            var moderation = new ModerationSection(api);
            var audit = new AuditSection(api);

            Add("overview", "Tổng quan", "explore", overview, overview.LoadAsync);
            Add("approvals", "Chờ duyệt", "verified_user", approvals, approvals.LoadAsync);
            Add("users", "Người dùng", "account_circle", users, users.LoadAsync);
            Add("places", "Địa điểm", "pin_drop", places, places.LoadAsync);
            Add("moderation", "Kiểm duyệt", "campaign", moderation, moderation.LoadAsync);
            Add("audit", "Nhật ký", "menu_book", audit, audit.LoadAsync);

            foreach (AdminSection section in new AdminSection[] { approvals, users, places, moderation, audit })
            {
                section.MessageRaised += (s, m) => toast.ShowMessage(m.Text, m.Kind);
                section.Changed += async (s, e) => await RefreshCountsAsync();
                section.PlacesChanged += (s, e) => PlacesChanged = true;
            }
            overview.MessageRaised += (s, m) => toast.ShowMessage(m.Text, m.Kind);
            overview.Navigate += (s, key) => Open(pages.First(p => p.Key == key));
            overview.Counted += (s, counts) => ShowCounts(counts);

            menu.ItemClicked += (s, item) => Open((Page)item.Data);
            var side = new Panel { Dock = DockStyle.Left, Width = Ui.S(230), BackColor = Color.White, Padding = new Padding(Ui.S(10), Ui.S(14), Ui.S(10), 0) };
            var line = new Panel { Dock = DockStyle.Left, Width = 1, BackColor = ThemeColors.Slate200 };
            menu.Dock = DockStyle.Top;
            side.Controls.Add(menu);
            Controls.Add(body);
            Controls.Add(line);
            Controls.Add(side);
            Controls.Add(toast);
            menu.SetItems(pages.Select(p => new ListItem { Title = p.Title, Icon = p.Icon, Data = p }));
            menu.Height = menu.WantedHeight;
            Load += async (s, e) => { Open(pages[0]); await RefreshCountsAsync(); };
        }

        /// <summary>True when a place was changed here, so the map's list of places has to be fetched again.</summary>
        public bool PlacesChanged { get; private set; }

        private void Add(string key, string title, string icon, Control view, Func<Task> load)
        {
            view.Visible = false;
            body.Controls.Add(view);
            pages.Add(new Page { Key = key, Title = title, Icon = icon, View = view, Load = load });
        }

        private async void Open(Page page)
        {
            if (current != null) current.View.Visible = false;
            current = page;
            page.View.Visible = true;
            page.View.BringToFront();
            menu.SelectedIndex = pages.IndexOf(page);
            try { await page.Load(); }
            catch (Exception ex) { toast.ShowMessage("Không tải được: " + ex.Message, ToastKind.Error); }
        }

        // The numbers on the menu come from the overview call, so every change asks the server again.
        private async Task RefreshCountsAsync()
        {
            ApiResponse r = await api.GetAsync("/v1/admin/stats");
            if (!r.IsSuccess || IsDisposed) return;
            ShowCounts(new Dictionary<string, long>
            {
                { "approvals", JsonHelper.Long(r.Object, "pendingClaims") + JsonHelper.Long(r.Object, "pendingEditRequests") + JsonHelper.Long(r.Object, "pendingPhotos") },
                { "moderation", JsonHelper.Long(r.Object, "openReports") },
            });
        }

        private void ShowCounts(IDictionary<string, long> counts)
        {
            menu.SetItems(pages.Select(p => new ListItem
            {
                Title = p.Title, Icon = p.Icon, Data = p,
                Pill = counts.ContainsKey(p.Key) && counts[p.Key] > 0 ? counts[p.Key].ToString() : null, Tone = PillTone.Warn,
            }));
            menu.SelectedIndex = current == null ? 0 : pages.IndexOf(current);
        }
    }
}
