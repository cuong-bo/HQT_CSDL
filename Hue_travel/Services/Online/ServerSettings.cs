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
            Environment.GetEnvironmentVariable(EnvironmentVariable), ReadServerFile(), ConfigurationManager.AppSettings["ApiBaseUrl"]));

        public static string BaseUrl => Current.Value;

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

        private static string ReadServerFile()
        {
            try
            {
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "CoDoGIS", "server.txt");
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
