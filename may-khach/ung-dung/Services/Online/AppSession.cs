namespace Hue_travel.Services.Online
{
    /// <summary>
    /// What the sign-in screen and the main window share: the server connection and who is signed in. Without a server
    /// address both are null and the app runs offline on the data that ships with it.
    /// </summary>
    public sealed class AppSession
    {
        private AppSession(ApiClient api, AuthService auth)
        {
            Api = api;
            Auth = auth;
        }

        public ApiClient Api { get; }

        public AuthService Auth { get; }

        public bool IsOnline => Api != null;

        /// <summary>A line the sign-in screen shows once, e.g. "the session ended".</summary>
        public string Notice { get; set; }

        /// <summary>
        /// Set by the sign-in screen when the visitor chose "Khách": the server is there but nobody is signed in, so the app
        /// offers what needs no account (map, search, routes) and must not treat the missing account as an ended session.
        /// </summary>
        public bool IsGuest { get; set; }

        /// <summary>Set by the main window when the user signed out (or the session ended): the app goes back to the sign-in screen.</summary>
        public bool ReturnToSignIn { get; set; }

        public static AppSession Create()
        {
            if (!ServerSettings.IsOnline) return new AppSession(null, null);
            var api = new ApiClient(ServerSettings.BaseUrl, new TokenStore(), null, ResponseVerifier.TryCreate(ServerSettings.PublicKey));
            return new AppSession(api, new AuthService(api, new TokenStore()));
        }
    }
}
