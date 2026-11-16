using System;

namespace Hue_travel.Services.Online
{
    /// <summary>What the app should do about its own version.</summary>
    public enum UpdateState
    {
        /// <summary>Nothing to say.</summary>
        UpToDate,

        /// <summary>A newer version exists; the person may carry on.</summary>
        Available,

        /// <summary>This version is older than the server accepts: the app must update.</summary>
        Required,
    }

    /// <summary>
    /// Compares the running version with the two the server announces in /v1/config (<c>minimumClientVersion</c>, <c>latestClientVersion</c>).
    /// The app never downloads or runs an installer by itself: it opens the announced address in the browser, and only an http or https one.
    /// </summary>
    public static class UpdateCheck
    {
        /// <summary>"1.1.0", "1.1" or "1.1.0.0" as a version; null when empty or not a version (so a bad setting never blocks the app).</summary>
        public static Version Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            Version version;
            if (!Version.TryParse(text.Trim().TrimStart('v', 'V'), out version)) return null;
            return version;
        }

        /// <summary>Required when older than the minimum, Available when older than the latest, otherwise UpToDate.</summary>
        public static UpdateState Evaluate(Version current, string minimum, string latest)
        {
            if (current == null) return UpdateState.UpToDate;
            Version min = Parse(minimum), newest = Parse(latest);
            if (min != null && Compare(current, min) < 0) return UpdateState.Required;
            if (newest != null && Compare(current, newest) < 0) return UpdateState.Available;
            return UpdateState.UpToDate;
        }

        // 1.1 and 1.1.0.0 are the same version (System.Version would call 1.1 older than 1.1.0.0).
        private static int Compare(Version a, Version b) =>
            new Version(a.Major, a.Minor, Math.Max(0, a.Build), Math.Max(0, a.Revision)).CompareTo(new Version(b.Major, b.Minor, Math.Max(0, b.Build), Math.Max(0, b.Revision)));

        /// <summary>True for an absolute http or https address: nothing that would open a file or start a program.</summary>
        public static bool IsSafeUrl(string url)
        {
            Uri uri;
            return Uri.TryCreate(url, UriKind.Absolute, out uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }
    }
}
