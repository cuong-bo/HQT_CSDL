using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Hue_travel.Models;
using Hue_travel.Services.Online;
using Hue_travel.Theme;

namespace Hue_travel.Account
{
    /// <summary>
    /// The signed-in person's panel: favourites, saved itineraries and password for everybody, "my places" for owners and
    /// the console for admins. Built in code; each screen is an <see cref="AccountTab"/>.
    /// </summary>
    public sealed class AccountPanel : FloatingPanelForm
    {
        private readonly AuthService auth;
        private readonly PanelHeader header = new PanelHeader { IconName = "account_circle", Text = "Tài khoản của tôi", Prominent = true };
        private readonly Panel userBar = new Panel { Dock = DockStyle.Top, Height = 62, BackColor = ThemeColors.Slate50 };
        private readonly Label lblName = new Label { AutoSize = true, Font = ThemeFonts.Bold(14), ForeColor = ThemeColors.Slate800, Location = new Point(16, 10) };
        private readonly Label lblRole = new Label { AutoSize = true, Font = ThemeFonts.Medium(11), ForeColor = ThemeColors.Primary, Location = new Point(16, 34) };
        private readonly IconButton btnLogout = AccountUi.Button("Đăng xuất", false, 100, "login");
        private readonly FlowLayoutPanel tabStrip = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(12, 8, 0, 0), WrapContents = false };
        private readonly Panel host = new Panel { Dock = DockStyle.Fill };
        private readonly List<KeyValuePair<IconButton, AccountTab>> tabs = new List<KeyValuePair<IconButton, AccountTab>>();
        private readonly FavoritesTab favorites;
        private readonly ItinerariesTab itineraries;
        private readonly PasswordTab password;
        private readonly OwnerTab owner;
        private readonly AdminTab admin;

        public event EventHandler<Poi> ShowOnMapRequested;
        public event EventHandler FavoritesChanged;
        public event EventHandler LoggedOut;

        public AccountPanel(ApiClient api, AuthService auth)
        {
            this.auth = auth;
            Size = new Size(580, 560);
            favorites = new FavoritesTab(api);
            itineraries = new ItinerariesTab(api);
            password = new PasswordTab(api);
            owner = new OwnerTab(api);
            admin = new AdminTab(api);

            header.CloseClicked += (s, e) => RaiseCloseRequested();
            btnLogout.Location = new Point(Size.Width - 116, 17);
            btnLogout.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnLogout.Click += async (s, e) =>
            {
                await auth.LogoutAsync();
                LoggedOut?.Invoke(this, EventArgs.Empty);
            };
            userBar.Controls.AddRange(new Control[] { lblName, lblRole, btnLogout });

            AddTab("Yêu thích", favorites);
            AddTab("Lộ trình", itineraries);
            AddTab("Chủ quản", owner);
            AddTab("Quản trị", admin);
            AddTab("Mật khẩu", password);

            favorites.ShowOnMapRequested += (s, p) => ShowOnMapRequested?.Invoke(this, p);
            favorites.FavoritesChanged += (s, e) => FavoritesChanged?.Invoke(this, EventArgs.Empty);
            password.PasswordChanged += (s, e) =>
            {
                auth.ForgetSession();
                LoggedOut?.Invoke(this, EventArgs.Empty);
            };
            foreach (KeyValuePair<IconButton, AccountTab> pair in tabs) pair.Value.MessageRaised += (s, m) => Notify(m.Text, m.Kind);

            Controls.Add(host);
            Controls.Add(tabStrip);
            Controls.Add(userBar);
            Controls.Add(header);
        }

        internal void SetRouteSource(Func<IReadOnlyList<string>> source) => itineraries.CurrentRouteStopIds = source;

        // All the screens exist from the start (so the panel scales them with itself); only one is visible at a time.
        private void AddTab(string title, AccountTab tab)
        {
            IconButton button = AccountUi.Button(title, false, 100);
            button.Margin = new Padding(0, 0, 6, 0);
            button.Click += async (s, e) => await ShowTabAsync(tab);
            tabStrip.Controls.Add(button);
            tab.Visible = false;
            host.Controls.Add(tab);
            tabs.Add(new KeyValuePair<IconButton, AccountTab>(button, tab));
        }

        /// <summary>Opens the panel for the signed-in user: shows the tabs their role allows and the first one (or the password screen).</summary>
        public async Task OpenAsync()
        {
            UserInfo user = auth.CurrentUser;
            if (user == null) return;
            lblName.Text = user.DisplayName;
            lblRole.Text = user.RoleName + "  •  " + user.Username;
            foreach (KeyValuePair<IconButton, AccountTab> pair in tabs)
            {
                bool allowed = pair.Value != admin || user.IsAdmin;
                if (auth.MustChangePassword) allowed = pair.Value == password;
                pair.Key.Visible = allowed;
            }
            Visible = true;
            BringToFront();
            if (auth.MustChangePassword)
            {
                Notify("Bạn đang dùng mật khẩu tạm. Hãy đổi mật khẩu để tiếp tục", ToastKind.Info);
                await ShowTabAsync(password);
            }
            else
            {
                await ShowTabAsync(favorites);
            }
        }

        public Task RefreshFavoritesAsync() => favorites.LoadAsync();

        private async Task ShowTabAsync(AccountTab tab)
        {
            foreach (KeyValuePair<IconButton, AccountTab> pair in tabs)
            {
                bool active = pair.Value == tab;
                pair.Value.Visible = active;
                pair.Key.FillColor = active ? ThemeColors.Sky100 : System.Drawing.Color.White;
                pair.Key.ForeColor = active ? ThemeColors.Primary : ThemeColors.Slate700;
                pair.Key.Invalidate();
            }
            await tab.LoadAsync();
        }
    }
}
