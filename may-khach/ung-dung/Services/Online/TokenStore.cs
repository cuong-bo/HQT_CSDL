using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Hue_travel.Services.Online
{
    /// <summary>
    /// Keeps the session tokens between runs, encrypted with Windows DPAPI for the current Windows user, in
    /// %LocalAppData%\HueTravel\session.dat. Another user (or another PC) cannot read the file.
    /// </summary>
    public sealed class TokenStore
    {
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("HueTravel.session.v1");
        private readonly string path;

        public TokenStore(string path = null)
        {
            this.path = path ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HueTravel", "session.dat");
        }

        public void Save(string access, string refresh)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string json = JsonHelper.Serialize(new Dictionary<string, object> { { "access", access }, { "refresh", refresh } });
                File.WriteAllBytes(path, ProtectedData.Protect(Encoding.UTF8.GetBytes(json), Entropy, DataProtectionScope.CurrentUser));
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is CryptographicException)
            {
                Debug.WriteLine("TokenStore.Save: " + ex.Message);   // not fatal: the user simply signs in again next time
            }
        }

        public bool TryLoad(out string access, out string refresh)
        {
            access = refresh = null;
            try
            {
                if (!File.Exists(path)) return false;
                byte[] plain = ProtectedData.Unprotect(File.ReadAllBytes(path), Entropy, DataProtectionScope.CurrentUser);
                var d = JsonHelper.Parse(Encoding.UTF8.GetString(plain)) as IDictionary<string, object>;
                access = JsonHelper.Str(d, "access");
                refresh = JsonHelper.Str(d, "refresh");
                return !string.IsNullOrEmpty(refresh);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is CryptographicException)
            {
                Debug.WriteLine("TokenStore.TryLoad: " + ex.Message);
                return false;
            }
        }

        public void Delete()
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                Debug.WriteLine("TokenStore.Delete: " + ex.Message);
            }
        }
    }
}
