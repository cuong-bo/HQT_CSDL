using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using Hue_travel.Models;
using Hue_travel.Services.Online;
using Hue_travel.Theme;

namespace Hue_travel.Account
{
    /// <summary>
    /// "Hồ sơ": the signed-in person's side panel. A profile card on top and a menu of what they can do (favourites, saved
    /// itineraries, "my places", the admin console, password, sign out); a menu row opens its screen (an
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
        private readonly PanelHeader header = new PanelHeader { IconName = "account_circle", Text = "Hồ sơ", Prominent = true };
        private readonly ProfileCard profileCard = new ProfileCard { Dock = DockStyle.Top, Height = 92 };
        private readonly Panel body = new Panel { Dock = DockStyle.Fill, BackColor = ThemeColors.SurfaceContainerLowest };
        private readonly ProfileMenu menu = new ProfileMenu { Dock = DockStyle.Fill };
        private readonly Panel detail = new Panel { Dock = DockStyle.Fill, Visible = false };
        private readonly Panel subHeader = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = ThemeColors.SurfaceContainerLowest };
        private readonly IconButton btnBack = AccountUi.Button("Hồ sơ", false, 96, "arrow_back");
        private readonly Label lblSection = new Label { AutoSize = false, Font = ThemeFonts.Bold(15), ForeColor = ThemeColors.Slate800, TextAlign = ContentAlignment.MiddleLeft, Location = new Point(118, 0), Size = new Size(380, 48) };
        private readonly Panel host = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 0, 12, 12) };
        private readonly List<Func<Task>> menuActions = new List<Func<Task>>();
        private readonly List<AccountTab> tabs = new List<AccountTab>();
        private readonly FavoritesTab favorites;
        private readonly ItinerariesTab itineraries;
        private readonly PasswordTab password;
        private readonly OwnerTab owner;
        private readonly AdminTab admin;

        public event EventHandler<Poi> ShowOnMapRequested;
        public event EventHandler FavoritesChanged;

        /// <summary>The user chose "Đăng xuất": the main window goes back to the sign-in screen.</summary>
        public event EventHandler LoggedOut;

        /// <summary>The password was changed: every session ended, so the user signs in again.</summary>
        public event EventHandler PasswordChanged;

        /// <summary>True while the panel itself is ending the session (sign out, password change), so the window does not report an expired session.</summary>
        public bool IsLeaving { get; private set; }

        public AccountPanel(ApiClient api, AuthService auth)
        {
            this.auth = auth;
            Size = new Size(520, 640);
            favorites = new FavoritesTab(api);
            itineraries = new ItinerariesTab(api);
            password = new PasswordTab(api);
            owner = new OwnerTab(api);
            admin = new AdminTab(api);
            foreach (AccountTab tab in new AccountTab[] { favorites, itineraries, owner, admin, password })
            {
                tab.Visible = false;
                host.Controls.Add(tab);
                tabs.Add(tab);
                tab.MessageRaised += (s, m) => Notify(m.Text, m.Kind);
            }

            header.CloseClicked += (s, e) => RaiseCloseRequested();
            menu.ItemClicked += async (s, index) => { if (index >= 0 && index < menuActions.Count) await menuActions[index](); };
            btnBack.Click += (s, e) => ShowHome();
            subHeader.Controls.Add(lblSection);
            btnBack.Location = new Point(12, 10);
            subHeader.Controls.Add(btnBack);

            favorites.ShowOnMapRequested += (s, p) => ShowOnMapRequested?.Invoke(this, p);
            favorites.FavoritesChanged += (s, e) => FavoritesChanged?.Invoke(this, EventArgs.Empty);
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

        internal void SetRouteSource(Func<IReadOnlyList<string>> source) => itineraries.CurrentRouteStopIds = source;

        /// <summary>Fills the panel for the signed-in user (card and menu by role). With a temporary password only the password screen opens.</summary>
        public async Task OpenAsync()
        {
            UserInfo user = auth.CurrentUser;
            if (user == null) return;
            IsLeaving = false;
            profileCard.DisplayName = user.DisplayName;
            profileCard.RoleName = user.RoleName;
            profileCard.Username = user.Username;
            profileCard.Invalidate();
            BuildMenu(user);

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

        private void BuildMenu(UserInfo user)
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
                addScreen("star", "Địa điểm yêu thích", "Những nơi bạn đã lưu", "Địa điểm yêu thích", favorites);
                addScreen("route", "Lộ trình đã lưu", "Các lịch trình tham quan của bạn", "Lộ trình đã lưu", itineraries);
                addScreen("pin_drop", "Địa điểm của tôi", "Nhận và quản lý địa điểm bạn phụ trách", "Địa điểm của tôi", owner);
                if (user.IsAdmin) addScreen("verified_user", "Quản trị hệ thống", "Duyệt yêu cầu, người dùng và nhật ký", "Quản trị hệ thống", admin);
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

        private void ShowHome()
        {
            detail.Visible = false;
            menu.Visible = true;
            foreach (AccountTab tab in tabs) tab.Visible = false;
        }

        private async Task ShowDetailAsync(string heading, AccountTab tab, bool canGoBack)
        {
            foreach (AccountTab other in tabs) other.Visible = other == tab;
            lblSection.Text = heading;
            btnBack.Visible = canGoBack;
            lblSection.Left = canGoBack ? Ui.S(118) : Ui.S(16);
            menu.Visible = false;
            detail.Visible = true;
            await tab.LoadAsync();
        }

        public Task RefreshFavoritesAsync() => favorites.LoadAsync();
    }
}
