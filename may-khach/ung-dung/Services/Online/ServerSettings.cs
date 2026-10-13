using System;
using System.Configuration;
using System.IO;

namespace Hue_travel.Services.Online
{
    /// <summary>
    /// Where the server is. The address comes from, in this order: the HUE_API_URL environment variable, the file
    /// %ProgramData%\CoDoGIS\server.txt (written by the installer), and the ApiBaseUrl setting in the .config file.
    /// An empty address means "offline": the app uses the data that ships with it.
    /// </summary>
    public static class ServerSettings
    {
        public const string EnvironmentVariable = "HUE_API_URL";

        private static readonly Lazy<string> Current = new Lazy<string>(() => Resolve(
            Environment.GetEnvironmentVariable(EnvironmentVariable), ReadServerFile(), ReadSetting("ApiBaseUrl")));

        public static string BaseUrl => Current.Value;

        /// <summary>
        /// The server's public signing key (base64), from HUE_API_PUBLIC_KEY, %ProgramData%\CoDoGIS\public.key or the
        /// ApiPublicKey setting. Empty means replies are not checked.
        /// </summary>
        public static string PublicKey => PublicKeyValue.Value;

        private static readonly Lazy<string> PublicKeyValue = new Lazy<string>(() => FirstNonEmpty(
            Environment.GetEnvironmentVariable("HUE_API_PUBLIC_KEY"), ReadCommonFile("public.key"), ReadSetting("ApiPublicKey")));

        private static string FirstNonEmpty(params string[] values)
        {
            foreach (string value in values)
                if (!string.IsNullOrWhiteSpace(value)) return value.Trim();
            return "";
        }

        public static bool IsOnline => BaseUrl.Length > 0;

        /// <summary>The first candidate that is an absolute http or https address (without a trailing slash), or "".</summary>
        public static string Resolve(params string[] candidates)
        {
            foreach (string candidate in candidates)
            {
                string normalized = Normalize(candidate);
                if (normalized != null) return normalized;
            }
            return "";
        }

        public static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            value = value.Trim();
            Uri uri;
            if (!Uri.TryCreate(value, UriKind.Absolute, out uri)) return null;
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return null;
            return value.TrimEnd('/');
        }

        private static string ReadServerFile() => ReadCommonFile("server.txt");

        private static string ReadSetting(string key)
        {
            try
            {
                return ConfigurationManager.AppSettings[key];
            }
            catch (ConfigurationErrorsException)
            {
                return null;   // a damaged .config file means "not set", never a crash at start-up
            }
        }

        private static string ReadCommonFile(string name)
        {
            try
            {
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "CoDoGIS", name);
                return File.Exists(path) ? File.ReadAllText(path) : null;
            }
            catch (IOException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
        }
    }
}
