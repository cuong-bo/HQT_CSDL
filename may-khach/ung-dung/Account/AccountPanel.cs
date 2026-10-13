using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Hue_travel.Models;
using Hue_travel.Services.Online;
using Hue_travel.Theme;

namespace Hue_travel.Account
{
    /// <summary>
    /// "Hồ sơ": the signed-in person's side panel. A profile card on top and a menu of what this person can do, which depends on
    /// who they are: everybody has favourites, saved trips, their reviews, their account and password; an owner (or someone who asked
    /// to become one) also has "Địa điểm của tôi"; an admin also has the admin console. A menu row opens its screen (an
    /// <see cref="AccountTab"/>) with a back button. Built in code.
    /// </summary>
    public sealed class AccountPanel : FloatingPanelForm
    {
        private sealed class ProfileCard : Control
        {
            public string DisplayName = "";
            public string RoleName = "";
            public string Username = "";

            public ProfileCard()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                BackColor = ThemeColors.Slate50;
            }

            private static string Initials(string name)
            {
                string[] words = (name ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length == 0) return "?";
                if (words.Length == 1) return words[0].Substring(0, 1).ToUpperInvariant();
                return (words[words.Length - 2].Substring(0, 1) + words[words.Length - 1].Substring(0, 1)).ToUpperInvariant();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(BackColor);
                using (var pen = new Pen(ThemeColors.Slate200)) g.DrawLine(pen, 0, Height - 1, Width, Height - 1);

                int avatar = Ui.S(56);
                var circle = new Rectangle(Ui.S(18), (Height - avatar) / 2, avatar, avatar);
                using (var brush = new LinearGradientBrush(circle, ThemeColors.Sky500, ThemeColors.Sky700, LinearGradientMode.ForwardDiagonal)) g.FillEllipse(brush, circle);
                TextRenderer.DrawText(g, Initials(DisplayName), ThemeFonts.Bold(19), circle, Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);

                int left = circle.Right + Ui.S(16);
                int width = Width - left - Ui.S(16);
                const TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;
                TextRenderer.DrawText(g, DisplayName, ThemeFonts.Bold(17), new Rectangle(left, Ui.S(18), width, Ui.S(26)), ThemeColors.Slate800, flags);

                Font chipFont = ThemeFonts.SemiBold(11);
                Size chipText = TextRenderer.MeasureText(RoleName, chipFont, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                var chip = new Rectangle(left, Ui.S(50), chipText.Width + Ui.S(18), Ui.S(22));
                using (GraphicsPath path = Ui.RoundedRect(chip, chip.Height / 2f))
                using (var brush = new SolidBrush(ThemeColors.Sky100)) g.FillPath(brush, path);
                TextRenderer.DrawText(g, RoleName, chipFont, chip, ThemeColors.Primary,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                TextRenderer.DrawText(g, "@" + Username, ThemeFonts.Regular(12), new Rectangle(chip.Right + Ui.S(10), chip.Y, Math.Max(10, Width - chip.Right - Ui.S(26)), chip.Height),
                    ThemeColors.Slate500, flags | TextFormatFlags.VerticalCenter);
            }
        }

        private readonly AuthService auth;
        private readonly ApiClient api;
        private readonly PanelHeader header = new PanelHeader { IconName = "account_circle", Text = "Hồ sơ", Prominent = true };
        private readonly ProfileCard profileCard = new ProfileCard { Dock = DockStyle.Top, Height = 92 };
        private readonly Panel body = new Panel { Dock = DockStyle.Fill, BackColor = ThemeColors.SurfaceContainerLowest };
        private readonly ProfileMenu menu = new ProfileMenu { Dock = DockStyle.Fill };
        private readonly Panel detail = new Panel { Dock = DockStyle.Fill, Visible = false };
        private readonly Panel subHeader = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = ThemeColors.SurfaceContainerLowest };
        private readonly IconButton btnBack = AccountKit.Secondary("Quay lại", "arrow_back");
        private readonly Label lblSection = new Label { AutoSize = false, Font = ThemeFonts.Bold(15), ForeColor = ThemeColors.Slate800, TextAlign = ContentAlignment.MiddleLeft };
        private readonly Panel host = new Panel { Dock = DockStyle.Fill };
        private readonly List<Func<Task>> menuActions = new List<Func<Task>>();
        private readonly List<AccountTab> tabs = new List<AccountTab>();
        private readonly FavoritesTab favorites;
        private readonly ItinerariesTab itineraries;
        private readonly MyReviewsTab myReviews;
        private readonly ProfileTab profile;
        private readonly PasswordTab password;
        private readonly OwnerPlacesTab ownerPlaces;
        private readonly PlaceManageTab placeManage;
        private Action backAction;

        public event EventHandler<Poi> ShowOnMapRequested;
        public event EventHandler FavoritesChanged;

        /// <summary>A saved trip was pressed: the main window opens its places (the first is the start) in the route form.</summary>
        public event EventHandler<IReadOnlyList<Poi>> ItineraryRequested;

        /// <summary>A review of the person was pressed: the main window opens that place's reviews.</summary>
        public event EventHandler<Poi> ReviewRequested;

        /// <summary>"Bảng quản trị" was chosen by an admin: the main window opens the console.</summary>
        public event EventHandler AdminRequested;

        /// <summary>The user chose "Đăng xuất": the main window goes back to the sign-in screen.</summary>
        public event EventHandler LoggedOut;

        /// <summary>The password was changed: every session ended, so the user signs in again.</summary>
        public event EventHandler PasswordChanged;

        /// <summary>True while the panel itself is ending the session (sign out, password change), so the window does not report an expired session.</summary>
        public bool IsLeaving { get; private set; }

        public AccountPanel(ApiClient api, AuthService auth)
        {
            this.api = api;
            this.auth = auth;
            Size = new Size(440, 640);
            favorites = new FavoritesTab(api);
            itineraries = new ItinerariesTab(api);
            myReviews = new MyReviewsTab(api);
            profile = new ProfileTab(api, auth);
            password = new PasswordTab(api);
            ownerPlaces = new OwnerPlacesTab(api);
            placeManage = new PlaceManageTab(api);
            foreach (AccountTab tab in new AccountTab[] { favorites, itineraries, myReviews, profile, password, ownerPlaces, placeManage })
            {
                tab.Visible = false;
                host.Controls.Add(tab);
                tabs.Add(tab);
                tab.MessageRaised += (s, m) => Notify(m.Text, m.Kind);
            }

            header.CloseClicked += (s, e) => RaiseCloseRequested();
            menu.ItemClicked += async (s, index) => { if (index >= 0 && index < menuActions.Count) await menuActions[index](); };
            btnBack.Click += (s, e) => { if (backAction != null) { Action back = backAction; backAction = null; back(); } else ShowHome(); };
            subHeader.Controls.Add(lblSection);
            subHeader.Controls.Add(btnBack);
            subHeader.Resize += (s, e) => LayoutSubHeader();

            favorites.ShowOnMapRequested += (s, p) => ShowOnMapRequested?.Invoke(this, p);
            favorites.FavoritesChanged += (s, e) => FavoritesChanged?.Invoke(this, EventArgs.Empty);
            itineraries.UseRequested += (s, places) => ItineraryRequested?.Invoke(this, places);
            myReviews.OpenRequested += (s, p) => ReviewRequested?.Invoke(this, p);
            profile.Changed += (s, e) => ShowCard();
            ownerPlaces.OpenPlaceRequested += async (s, p) => await OpenPlaceAsync(p);
            password.PasswordChanged += (s, e) =>
            {
                IsLeaving = true;
                auth.ForgetSession();
                PasswordChanged?.Invoke(this, EventArgs.Empty);
            };

            detail.Controls.Add(host);
            detail.Controls.Add(subHeader);
            body.Controls.Add(detail);
            body.Controls.Add(menu);
            Controls.Add(body);
            Controls.Add(profileCard);
            Controls.Add(header);
        }

        private void LayoutSubHeader()
        {
            btnBack.SetBounds(Ui.S(12), Ui.S(8), Ui.S(112), Ui.S(36));
            lblSection.SetBounds(btnBack.Right + Ui.S(10), 0, Math.Max(10, subHeader.Width - btnBack.Right - Ui.S(22)), subHeader.Height);
        }

        internal void SetRouteSource(Func<IReadOnlyList<string>> source) => itineraries.CurrentRouteStopIds = source;

        private void ShowCard()
        {
            UserInfo user = auth.CurrentUser;
            if (user == null) return;
            profileCard.DisplayName = user.DisplayName;
            profileCard.RoleName = user.RoleName;
            profileCard.Username = user.Username;
            profileCard.Invalidate();
        }

        /// <summary>Fills the panel for the signed-in user (card and menu by role). With a temporary password only the password screen opens.</summary>
        public async Task OpenAsync()
        {
            UserInfo user = auth.CurrentUser;
            if (user == null) return;
            IsLeaving = false;
            backAction = null;
            ShowCard();
            await BuildMenuAsync(user);

            if (auth.MustChangePassword)
            {
                Notify("Bạn đang dùng mật khẩu tạm. Hãy đổi mật khẩu để tiếp tục", ToastKind.Info);
                await ShowDetailAsync("Đổi mật khẩu", password, false);
            }
            else
            {
                ShowHome();
            }
        }

        private async Task BuildMenuAsync(UserInfo user)
        {
            var items = new List<ProfileMenu.Item>();
            menuActions.Clear();
            Action<string, string, string, string, AccountTab> addScreen = (icon, title, subtitle, heading, tab) =>
            {
                items.Add(new ProfileMenu.Item(icon, title, subtitle));
                menuActions.Add(() => ShowDetailAsync(heading, tab, true));
            };

            if (!auth.MustChangePassword)
            {
                if (user.IsAdmin)
                {
                    string waiting = await AdminSummaryAsync();
                    items.Add(new ProfileMenu.Item("verified_user", "Bảng quản trị", waiting));
                    menuActions.Add(() => { AdminRequested?.Invoke(this, EventArgs.Empty); return Task.CompletedTask; });
                }
                else
                {
                    int claims = await ClaimCountAsync();
                    if (user.IsOwner || claims > 0)
                        addScreen("pin_drop", "Địa điểm của tôi", user.IsOwner ? "Cập nhật thông tin, ảnh, thông báo và đánh giá" : claims + " yêu cầu đang chờ quản trị viên duyệt", "Địa điểm của tôi", ownerPlaces);
                    else
                        addScreen("pin_drop", "Đăng ký chủ quản địa điểm", "Nhận quyền quản lý địa điểm của bạn", "Địa điểm của tôi", ownerPlaces);
                }
                addScreen("star", "Địa điểm yêu thích", "Những nơi bạn đã lưu", "Địa điểm yêu thích", favorites);
                addScreen("route", "Lộ trình đã lưu", "Các lịch trình tham quan của bạn", "Lộ trình đã lưu", itineraries);
                addScreen("lightbulb", "Đánh giá của tôi", "Những nhận xét bạn đã viết", "Đánh giá của tôi", myReviews);
                addScreen("account_circle", "Thông tin tài khoản", "Tên hiển thị của bạn", "Thông tin tài khoản", profile);
            }
            addScreen("lock", "Đổi mật khẩu", "Cập nhật mật khẩu đăng nhập", "Đổi mật khẩu", password);

            items.Add(new ProfileMenu.Item("login", "Đăng xuất", "Kết thúc phiên và quay lại màn hình đăng nhập", true));
            menuActions.Add(async () =>
            {
                IsLeaving = true;
                await auth.LogoutAsync();
                LoggedOut?.Invoke(this, EventArgs.Empty);
            });
            menu.SetItems(items);
        }

        private async Task<int> ClaimCountAsync()
        {
            ApiResponse r = await api.GetAsync("/v1/owner/claims");
            return r.IsSuccess ? (JsonHelper.AsList(r.Json) ?? new List<object>()).OfType<IDictionary<string, object>>().Count(d => JsonHelper.Str(d, "status") == "pending") : 0;
        }

        private async Task<string> AdminSummaryAsync()
        {
            ApiResponse r = await api.GetAsync("/v1/admin/stats");
            if (!r.IsSuccess) return "Duyệt yêu cầu, người dùng, dữ liệu và nhật ký";
            long waiting = JsonHelper.Long(r.Object, "pendingClaims") + JsonHelper.Long(r.Object, "pendingEditRequests") + JsonHelper.Long(r.Object, "openReports") + JsonHelper.Long(r.Object, "pendingPhotos");
            return waiting > 0 ? waiting + " việc đang chờ xử lý" : "Không có việc nào đang chờ";
        }

        private void ShowHome()
        {
            backAction = null;
            detail.Visible = false;
            menu.Visible = true;
            foreach (AccountTab tab in tabs) tab.Visible = false;
        }

        private async Task ShowDetailAsync(string heading, AccountTab tab, bool canGoBack)
        {
            foreach (AccountTab other in tabs) other.Visible = other == tab;
            lblSection.Text = heading;
            btnBack.Visible = canGoBack;
            LayoutSubHeader();
            menu.Visible = false;
            detail.Visible = true;
            await tab.LoadAsync();
        }

        // A place of an owner opens inside this panel; "Quay lại" returns to the list of places, not the menu.
        private async Task OpenPlaceAsync(Poi poi)
        {
            placeManage.SetPlace(poi);
            await ShowDetailAsync("Quản lý địa điểm", placeManage, true);
            backAction = () => { var ignored = ShowDetailAsync("Địa điểm của tôi", ownerPlaces, true); };
        }

        public Task RefreshFavoritesAsync() => favorites.LoadAsync();
    }
}
