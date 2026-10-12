using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hue_travel.Services.Online
{
    public sealed class UserInfo
    {
        public long Id { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string Role { get; set; }

        public bool IsAdmin => Role == "admin";

        public bool IsOwner => Role == "owner";

        public string RoleName => IsAdmin ? "Quản trị viên" : IsOwner ? "Chủ quản địa điểm" : "Người dùng";
    }

    /// <summary>Who is signed in. Wraps the register / login / logout calls and keeps the session between runs when asked to.</summary>
    public sealed class AuthService
    {
        private readonly ApiClient api;
        private readonly TokenStore store;

        public AuthService(ApiClient api, TokenStore store)
        {
            this.api = api;
            this.store = store;
            api.SessionExpired += (s, e) =>
            {
                CurrentUser = null;
                Changed?.Invoke(this, EventArgs.Empty);
            };
        }

        public UserInfo CurrentUser { get; private set; }

        /// <summary>True right after signing in with a temporary password: the only thing allowed is changing it.</summary>
        public bool MustChangePassword { get; private set; }

        public event EventHandler Changed;

        /// <summary>Returns null on success, otherwise the message to show.</summary>
        public Task<string> LoginAsync(string username, string password, bool remember) =>
            AuthenticateAsync("/v1/auth/login", new Dictionary<string, object> { { "username", username }, { "password", password } }, remember);

        public Task<string> RegisterAsync(string username, string password, string displayName, bool remember) =>
            AuthenticateAsync("/v1/auth/register", new Dictionary<string, object> { { "username", username }, { "password", password }, { "displayName", displayName } }, remember);

        private async Task<string> AuthenticateAsync(string path, object body, bool remember)
        {
            ApiResponse r = await api.PostAsync(path, body).ConfigureAwait(true);
            if (!r.IsSuccess) return r.Error;

            string access = JsonHelper.Str(r.Object, "accessToken");
            string refresh = JsonHelper.Str(r.Object, "refreshToken");
            UserInfo user = ToUser(JsonHelper.Obj(r.Object, "user"));
            if (access == null || refresh == null || user == null) return "Máy chủ trả về dữ liệu không hợp lệ.";

            api.SetTokens(access, refresh, remember);
            CurrentUser = user;
            MustChangePassword = JsonHelper.Bool(r.Object, "mustChangePassword");
            Changed?.Invoke(this, EventArgs.Empty);
            return null;
        }

        /// <summary>Signs in again from the saved tokens, if there are any. False when there is no (valid) saved session.</summary>
        public async Task<bool> RestoreAsync()
        {
            string access, refresh;
            if (!store.TryLoad(out access, out refresh)) return false;
            api.SetTokens(access, refresh, true);
            ApiResponse r = await api.GetAsync("/v1/me").ConfigureAwait(true);
            if (!r.IsSuccess)
            {
                if (!r.IsNetworkError) api.ClearTokens();
                return false;
            }
            CurrentUser = ToUser(r.Object);
            Changed?.Invoke(this, EventArgs.Empty);
            return CurrentUser != null;
        }

        public async Task LogoutAsync()
        {
            if (!string.IsNullOrEmpty(api.RefreshToken))
                await api.PostAsync("/v1/auth/logout", new Dictionary<string, object> { { "refreshToken", api.RefreshToken } }).ConfigureAwait(true);
            api.ClearTokens();
            CurrentUser = null;
            MustChangePassword = false;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>After a successful password change every session is ended on the server.</summary>
        public void ForgetSession()
        {
            api.ClearTokens();
            CurrentUser = null;
            MustChangePassword = false;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        private static UserInfo ToUser(IDictionary<string, object> d)
        {
            if (d == null || JsonHelper.Str(d, "username") == null) return null;
            return new UserInfo
            {
                Id = JsonHelper.Long(d, "id"),
                Username = JsonHelper.Str(d, "username"),
                DisplayName = JsonHelper.Str(d, "displayName") ?? JsonHelper.Str(d, "username"),
                Role = JsonHelper.Str(d, "role") ?? "user",
            };
        }
    }
}
