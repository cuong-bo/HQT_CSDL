using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Hue_travel.Services.Online
{
    /// <summary>
    /// The newest installer the server offers: GET /v1/client/manifest. The values end up as a file name and a command to run on this
    /// computer, so everything is checked before use.
    /// </summary>
    public sealed class ClientManifest
    {
        private static readonly Regex VersionPattern = new Regex(@"^\d{1,4}\.\d{1,4}\.\d{1,4}$", RegexOptions.Compiled);
        private static readonly Regex Sha256Pattern = new Regex("^[0-9a-fA-F]{64}$", RegexOptions.Compiled);
        private static readonly Regex PathPattern = new Regex(@"^/static/client/(\d{1,4}\.\d{1,4}\.\d{1,4})/HueTravel-Setup-(\d{1,4}\.\d{1,4}\.\d{1,4})\.exe$", RegexOptions.Compiled);

        private const long MaxBytes = 1024L * 1024 * 1024;

        public Version Version { get; private set; }

        /// <summary>The address of the installer on the server, starting with /static/client/.</summary>
        public string InstallerPath { get; private set; }
        public long InstallerSize { get; private set; }
        public string InstallerSha256 { get; private set; }
        public string Notes { get; private set; }

        /// <summary>The manifest in a parsed JSON reply, or null when it is missing something or has a value that cannot be trusted.</summary>
        public static ClientManifest Parse(object json)
        {
            var root = json as IDictionary<string, object>;
            IDictionary<string, object> installer = JsonHelper.Obj(root, "installer");
            if (root == null || installer == null) return null;

            string version = JsonHelper.Str(root, "version");
            string path = JsonHelper.Str(installer, "path");
            string hash = JsonHelper.Str(installer, "sha256");
            long size = JsonHelper.Long(installer, "size");
            if (version == null || !VersionPattern.IsMatch(version)) return null;
            if (path == null) return null;
            Match match = PathPattern.Match(path);
            if (!match.Success || match.Groups[1].Value != version || match.Groups[2].Value != version) return null;   // the file must be the version it claims
            if (hash == null || !Sha256Pattern.IsMatch(hash) || size <= 0 || size > MaxBytes) return null;

            return new ClientManifest
            {
                Version = new Version(version), InstallerPath = path, InstallerSize = size,
                InstallerSha256 = hash.ToLowerInvariant(), Notes = (JsonHelper.Str(root, "notes") ?? string.Empty).Trim(),
            };
        }
    }

    /// <summary>
    /// Updates the app itself: asks the server for the newest installer, downloads it, checks its checksum and starts it quietly.
    /// The installer replaces the files and opens the app again, so the app must close right after <see cref="Launch"/>.
    /// </summary>
    public sealed class AppUpdater : IDisposable
    {
        /// <summary>A download that receives nothing for this long is given up.</summary>
        public static readonly TimeSpan StallTimeout = TimeSpan.FromSeconds(30);

        /// <summary>Where the installer is saved; removed at the next start.</summary>
        public static readonly string DownloadFolder = Path.Combine(Path.GetTempPath(), "HueTravel-Update");

        private readonly ApiClient api;
        private readonly string baseUrl;
        private readonly HttpClient http;

        public AppUpdater(ApiClient api, string baseUrl, HttpMessageHandler handler = null)
        {
            this.api = api;
            this.baseUrl = baseUrl.TrimEnd('/');
            http = handler == null ? new HttpClient() : new HttpClient(handler);
            http.Timeout = Timeout.InfiniteTimeSpan;   // the installer is large; a stalled transfer is caught by StallTimeout instead
        }

        public void Dispose() => http.Dispose();

        /// <summary>The version of this program (major.minor.build).</summary>
        public static Version CurrentVersion()
        {
            Version v = Assembly.GetExecutingAssembly().GetName().Version;
            return new Version(v.Major, v.Minor, Math.Max(v.Build, 0));
        }

        public static bool IsNewer(ClientManifest manifest, Version current) => manifest != null && manifest.Version > current;

        /// <summary>The manifest, or null when the server has not published an installer. Throws <see cref="ApiException"/> for anything else.</summary>
        public async Task<ClientManifest> FetchManifestAsync(CancellationToken ct)
        {
            ApiResponse reply = await api.GetAsync("/v1/client/manifest", ct).ConfigureAwait(true);
            if (reply.IsNetworkError) throw new ApiException(reply.Error ?? ApiClient.Unreachable);
            if (reply.StatusCode == 404) return null;   // an older server, or nothing published yet: nobody is asked to update
            if (!reply.IsSuccess) throw new ApiException("Máy chủ trả lỗi " + reply.StatusCode + " khi hỏi bản cập nhật.");
            ClientManifest manifest = ClientManifest.Parse(reply.Json);
            if (manifest == null) throw new ApiException("Máy chủ trả thông tin bản cập nhật không hợp lệ.");
            return manifest;
        }

        /// <summary>Removes what an earlier update left in the temporary folder.</summary>
        public static void CleanUp()
        {
            try
            {
                if (Directory.Exists(DownloadFolder)) Directory.Delete(DownloadFolder, true);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        /// <summary>Downloads the installer and returns its path; a file that does not match the checksum is deleted and refused.</summary>
        public async Task<string> DownloadAsync(ClientManifest manifest, IProgress<long> progress, CancellationToken ct)
        {
            CleanUp();
            Directory.CreateDirectory(DownloadFolder);
            string path = Path.Combine(DownloadFolder, "HueTravel-Setup-" + manifest.Version.ToString(3) + ".exe");
            using (CancellationTokenSource stall = CancellationTokenSource.CreateLinkedTokenSource(ct))
            {
                try
                {
                    stall.CancelAfter(StallTimeout);
                    using (HttpResponseMessage reply = await http.GetAsync(baseUrl + manifest.InstallerPath, HttpCompletionOption.ResponseHeadersRead, stall.Token).ConfigureAwait(true))
                    {
                        if (!reply.IsSuccessStatusCode)
                            throw new ApiException("Không tải được bản cập nhật (máy chủ trả mã " + (int)reply.StatusCode + ").");
                        using (Stream input = await reply.Content.ReadAsStreamAsync().ConfigureAwait(true))
                        using (var output = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
                        using (SHA256 hash = SHA256.Create())
                        {
                            var buffer = new byte[81920];
                            long done = 0;
                            while (true)
                            {
                                stall.CancelAfter(StallTimeout);
                                int read = await input.ReadAsync(buffer, 0, buffer.Length, stall.Token).ConfigureAwait(true);
                                if (read == 0) break;
                                done += read;
                                if (done > manifest.InstallerSize) throw new ApiException("Bản cập nhật lớn hơn máy chủ đã khai. Hãy thử lại sau.");
                                hash.TransformBlock(buffer, 0, read, null, 0);
                                await output.WriteAsync(buffer, 0, read, stall.Token).ConfigureAwait(true);
                                if (progress != null) progress.Report(done);
                            }
                            hash.TransformFinalBlock(new byte[0], 0, 0);
                            if (done != manifest.InstallerSize || !string.Equals(Hex(hash.Hash), manifest.InstallerSha256, StringComparison.Ordinal))
                                throw new ApiException("Bản cập nhật tải về bị thiếu hoặc bị sửa trên đường truyền. Hãy thử lại.");
                        }
                    }
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    TryDelete(path);
                    throw new ApiException("Đường truyền bị treo: không nhận được dữ liệu trong " + (int)StallTimeout.TotalSeconds + " giây.");
                }
                catch (HttpRequestException)
                {
                    TryDelete(path);
                    throw new ApiException(ApiClient.Unreachable);
                }
                catch (IOException ex)
                {
                    TryDelete(path);
                    throw new ApiException("Lỗi khi tải hoặc ghi bản cập nhật: " + ex.Message);
                }
                catch
                {
                    TryDelete(path);   // a cancelled or refused download leaves nothing behind
                    throw;
                }
            }
            return path;
        }

        /// <summary>
        /// Starts the installer quietly (Windows asks for permission once, because the installer needs administrator rights). It closes
        /// what is still open, replaces the files and starts the app again. The caller must exit right away.
        /// </summary>
        public static void Launch(string installerPath)
        {
            var start = new ProcessStartInfo(installerPath, "/SILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS /RELAUNCH=1") { UseShellExecute = true };
            try
            {
                Process.Start(start);
            }
            catch (Win32Exception)   // the person answered "No" to the permission prompt, or the system refused to run it
            {
                throw new ApiException("Windows không cho chạy bộ cài. Hãy bấm \"Có\" khi Windows hỏi quyền, rồi thử lại.");
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private static string Hex(byte[] hash) => BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}
