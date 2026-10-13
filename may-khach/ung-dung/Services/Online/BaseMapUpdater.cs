using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Hue_travel.Services.Data;

namespace Hue_travel.Services.Online
{
    public sealed class BaseMapFile
    {
        public string Name { get; set; }
        public long Size { get; set; }
        public string Sha256 { get; set; }
    }

    /// <summary>
    /// What the server offers: GET /v1/basemap/manifest. Everything in it is checked before use (names, sizes, checksums), because the
    /// values end up as file names and sizes on this computer.
    /// </summary>
    public sealed class BaseMapManifest
    {
        private static readonly Regex VersionPattern = new Regex("^[0-9a-f]{16}$", RegexOptions.Compiled);
        private static readonly Regex Sha256Pattern = new Regex("^[0-9a-fA-F]{64}$", RegexOptions.Compiled);
        private static readonly Regex NamePattern = new Regex(@"^[A-Za-z0-9_][A-Za-z0-9_.\-]{0,80}$", RegexOptions.Compiled);
        private static readonly Regex PathPattern = new Regex(@"^/static/basemap/[A-Za-z0-9_./\-]{1,160}$", RegexOptions.Compiled);

        private const long MaxBytes = 4L * 1024 * 1024 * 1024;
        private const int MaxFiles = 500;

        public string Version { get; private set; }

        /// <summary>The address of the package on the server, starting with /static/basemap/.</summary>
        public string PackagePath { get; private set; }
        public long PackageSize { get; private set; }
        public string PackageSha256 { get; private set; }

        /// <summary>The size of all the files once unpacked.</summary>
        public long TotalSize { get; private set; }
        public IReadOnlyList<BaseMapFile> Files { get; private set; }

        /// <summary>The manifest in a parsed JSON reply, or null when it is missing something or has a value that cannot be trusted.</summary>
        public static BaseMapManifest Parse(object json)
        {
            var root = json as IDictionary<string, object>;
            IDictionary<string, object> package = JsonHelper.Obj(root, "package");
            IList<object> list = JsonHelper.List(root, "files");
            if (root == null || package == null || list == null || list.Count == 0 || list.Count > MaxFiles) return null;

            string version = JsonHelper.Str(root, "version");
            string path = JsonHelper.Str(package, "path");
            string packageHash = JsonHelper.Str(package, "sha256");
            long packageSize = JsonHelper.Long(package, "size");
            long totalSize = JsonHelper.Long(root, "totalSize");
            if (version == null || !VersionPattern.IsMatch(version)) return null;
            if (path == null || !PathPattern.IsMatch(path) || path.Contains("..") || path.Contains("//")) return null;
            if (packageHash == null || !Sha256Pattern.IsMatch(packageHash)) return null;
            if (packageSize <= 0 || packageSize > MaxBytes || totalSize <= 0 || totalSize > MaxBytes) return null;

            var files = new List<BaseMapFile>();
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { BaseMapStore.VersionFileName };
            long sum = 0;
            foreach (object item in list)
            {
                var entry = item as IDictionary<string, object>;
                string name = JsonHelper.Str(entry, "name");
                string hash = JsonHelper.Str(entry, "sha256");
                long size = JsonHelper.Long(entry, "size");
                if (name == null || !NamePattern.IsMatch(name) || name.Contains("..")) return null;
                if (hash == null || !Sha256Pattern.IsMatch(hash) || size < 0 || size > MaxBytes) return null;
                if (!names.Add(name)) return null;   // twice in the list, or the name the app keeps for its own version file
                sum += size;
                files.Add(new BaseMapFile { Name = name, Size = size, Sha256 = hash.ToLowerInvariant() });
            }
            if (sum != totalSize) return null;
            if (!files.Any(f => string.Equals(f.Name, BaseMapStore.MarkerFile, StringComparison.OrdinalIgnoreCase))) return null;

            return new BaseMapManifest
            {
                Version = version, PackagePath = path, PackageSize = packageSize, PackageSha256 = packageHash.ToLowerInvariant(),
                TotalSize = totalSize, Files = files,
            };
        }
    }

    public enum BaseMapPhase { Downloading, Installing }

    public struct BaseMapProgress
    {
        public BaseMapProgress(BaseMapPhase phase, long done, long total)
        {
            Phase = phase;
            Done = done;
            Total = total;
        }

        public BaseMapPhase Phase { get; }
        public long Done { get; }
        public long Total { get; }
    }

    /// <summary>
    /// Brings the map data on this computer up to date with the server: asks for the manifest, downloads the package, checks every
    /// checksum, unpacks it beside the data in use and swaps the two folders. A copy in use is never touched until the new one is
    /// complete and checked, so a lost connection, a full disk or a closed window leave the old data as it was.
    /// </summary>
    public sealed class BaseMapUpdater : IDisposable
    {
        /// <summary>A download that receives nothing for this long is given up.</summary>
        public static readonly TimeSpan StallTimeout = TimeSpan.FromSeconds(30);

        private const string DownloadName = "basemap.download";
        private const string StagingSuffix = ".new";
        private const string OldSuffix = ".old";
        private const long FreeSpaceMargin = 50L * 1024 * 1024;

        private readonly ApiClient api;
        private readonly string baseUrl;
        private readonly string root;
        private readonly HttpClient http;

        /// <param name="root">Where the data lives (<see cref="BaseMapStore.DefaultRoot"/> unless a test says otherwise).</param>
        public BaseMapUpdater(ApiClient api, string baseUrl, string root = null, HttpMessageHandler handler = null)
        {
            this.api = api;
            this.baseUrl = baseUrl.TrimEnd('/');
            this.root = root ?? BaseMapStore.DefaultRoot;
            http = handler == null ? new HttpClient() : new HttpClient(handler);
            http.Timeout = Timeout.InfiniteTimeSpan;   // the package is large; a stalled transfer is caught by StallTimeout instead
        }

        public string InstalledVersion => BaseMapStore.InstalledVersion(root);

        public void Dispose() => http.Dispose();

        public async Task<BaseMapManifest> FetchManifestAsync(CancellationToken ct)
        {
            ApiResponse reply = await api.GetAsync("/v1/basemap/manifest", ct).ConfigureAwait(true);
            if (reply.IsNetworkError) throw new ApiException(reply.Error ?? ApiClient.Unreachable);
            if (reply.StatusCode == 404) throw new ApiException("Máy chủ chưa có dữ liệu bản đồ để tải. Hãy báo quản trị viên chạy \"deploy.sh pack\" trên máy chủ.");
            if (!reply.IsSuccess) throw new ApiException("Máy chủ trả lỗi " + reply.StatusCode + " khi hỏi dữ liệu bản đồ.");
            BaseMapManifest manifest = BaseMapManifest.Parse(reply.Json);
            if (manifest == null) throw new ApiException("Máy chủ trả bản kê dữ liệu bản đồ không hợp lệ.");
            return manifest;
        }

        /// <summary>
        /// Finishes or undoes what a crash left behind: when the swap died between its two moves the old data is put back, and the
        /// half-downloaded files are removed. Call it before reading the data.
        /// </summary>
        public static void Recover(string root)
        {
            try
            {
                string live = BaseMapStore.FolderIn(root);
                string old = live + OldSuffix;
                if (!Directory.Exists(live) && Directory.Exists(old)) Directory.Move(old, live);
                DeleteFolder(live + StagingSuffix);
                DeleteFolder(old);
                string download = Path.Combine(root, DownloadName);
                if (File.Exists(download)) File.Delete(download);
            }
            catch (IOException) { }                    // something still holds the files: the next start tries again
            catch (UnauthorizedAccessException) { }
        }

        public static void Recover() => Recover(BaseMapStore.DefaultRoot);

        /// <summary>
        /// Downloads and installs <paramref name="manifest"/>. Cancelling is honoured until the data is complete and checked; the
        /// final swap of the two folders is not interrupted.
        /// </summary>
        public async Task InstallAsync(BaseMapManifest manifest, IProgress<BaseMapProgress> progress, CancellationToken ct)
        {
            Recover(root);
            Directory.CreateDirectory(root);
            CheckFreeSpace(manifest);

            string download = Path.Combine(root, DownloadName);
            string staging = BaseMapStore.FolderIn(root) + StagingSuffix;
            try
            {
                await DownloadAsync(manifest, download, progress, ct).ConfigureAwait(true);
                ct.ThrowIfCancellationRequested();
                Unpack(manifest, download, staging, progress, ct);
            }
            catch
            {
                DeleteFolder(staging);
                throw;
            }
            finally
            {
                TryDeleteFile(download);
            }
            Swap(staging);
        }

        private void CheckFreeSpace(BaseMapManifest manifest)
        {
            long free;
            try
            {
                free = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(root))).AvailableFreeSpace;
            }
            catch (ArgumentException) { return; }       // no drive letter (a network path): cannot tell, so try
            catch (IOException) { return; }
            // The package, the unpacked copy and, for a moment, the old copy as well.
            long needed = manifest.PackageSize + manifest.TotalSize * 2 + FreeSpaceMargin;
            if (free < needed)
                throw new ApiException(string.Format("Ổ đĩa không đủ chỗ để tải dữ liệu bản đồ: cần khoảng {0} MB trống, hiện còn {1} MB.", needed / (1024 * 1024), free / (1024 * 1024)));
        }

        private async Task DownloadAsync(BaseMapManifest manifest, string path, IProgress<BaseMapProgress> progress, CancellationToken ct)
        {
            using (CancellationTokenSource stall = CancellationTokenSource.CreateLinkedTokenSource(ct))
            {
                try
                {
                    stall.CancelAfter(StallTimeout);
                    using (HttpResponseMessage reply = await http.GetAsync(baseUrl + manifest.PackagePath, HttpCompletionOption.ResponseHeadersRead, stall.Token).ConfigureAwait(true))
                    {
                        if (!reply.IsSuccessStatusCode)
                            throw new ApiException("Không tải được gói dữ liệu bản đồ (máy chủ trả mã " + (int)reply.StatusCode + ").");
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
                                if (done > manifest.PackageSize) throw new ApiException("Gói dữ liệu bản đồ lớn hơn máy chủ đã khai. Hãy thử lại sau.");
                                hash.TransformBlock(buffer, 0, read, null, 0);
                                await output.WriteAsync(buffer, 0, read, stall.Token).ConfigureAwait(true);
                                if (progress != null) progress.Report(new BaseMapProgress(BaseMapPhase.Downloading, done, manifest.PackageSize));
                            }
                            hash.TransformFinalBlock(new byte[0], 0, 0);
                            if (done != manifest.PackageSize || !string.Equals(Hex(hash.Hash), manifest.PackageSha256, StringComparison.Ordinal))
                                throw new ApiException("Gói dữ liệu bản đồ tải về bị thiếu hoặc bị sửa trên đường truyền. Hãy thử lại.");
                        }
                    }
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    throw new ApiException("Đường truyền bị treo: không nhận được dữ liệu trong " + (int)StallTimeout.TotalSeconds + " giây.");
                }
                catch (HttpRequestException)
                {
                    throw new ApiException(ApiClient.Unreachable);
                }
                catch (IOException ex)
                {
                    throw new ApiException("Lỗi khi tải hoặc ghi dữ liệu bản đồ: " + ex.Message);
                }
            }
        }

        // Every file is unpacked by name from the manifest only (never a path from inside the package), and checked against its
        // size and checksum, so nothing outside the staging folder can be written and nothing unlisted can come along.
        private static void Unpack(BaseMapManifest manifest, string package, string staging, IProgress<BaseMapProgress> progress, CancellationToken ct)
        {
            DeleteFolder(staging);
            Directory.CreateDirectory(staging);
            var expected = manifest.Files.ToDictionary(f => f.Name, StringComparer.OrdinalIgnoreCase);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            long done = 0;
            try
            {
                using (ZipArchive zip = ZipFile.OpenRead(package))
                {
                    foreach (ZipArchiveEntry entry in zip.Entries)
                    {
                        ct.ThrowIfCancellationRequested();
                        BaseMapFile file;
                        if (!expected.TryGetValue(entry.FullName, out file))
                            throw new ApiException("Gói dữ liệu bản đồ có tệp không có trong bản kê: " + Shorten(entry.FullName));
                        if (!seen.Add(file.Name)) throw new ApiException("Gói dữ liệu bản đồ có tệp trùng tên: " + file.Name);

                        string target = Path.Combine(staging, file.Name);
                        long written = 0;
                        using (Stream input = entry.Open())
                        using (var output = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                        using (SHA256 hash = SHA256.Create())
                        {
                            var buffer = new byte[81920];
                            int read;
                            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                written += read;
                                if (written > file.Size) throw new ApiException("Tệp " + file.Name + " trong gói lớn hơn bản kê khai.");
                                hash.TransformBlock(buffer, 0, read, null, 0);
                                output.Write(buffer, 0, read);
                            }
                            hash.TransformFinalBlock(new byte[0], 0, 0);
                            if (written != file.Size || !string.Equals(Hex(hash.Hash), file.Sha256, StringComparison.Ordinal))
                                throw new ApiException("Tệp " + file.Name + " trong gói bị hỏng.");
                        }
                        done += file.Size;
                        if (progress != null) progress.Report(new BaseMapProgress(BaseMapPhase.Installing, done, manifest.TotalSize));
                    }
                }
            }
            catch (InvalidDataException)
            {
                throw new ApiException("Gói dữ liệu bản đồ không phải tệp nén hợp lệ.");
            }
            if (seen.Count != expected.Count)
                throw new ApiException("Gói dữ liệu bản đồ thiếu tệp: " + string.Join(", ", expected.Keys.Where(k => !seen.Contains(k)).Take(3)));
            // Written last: a folder with this file is a complete one.
            File.WriteAllText(Path.Combine(staging, BaseMapStore.VersionFileName), manifest.Version);
        }

        private void Swap(string staging)
        {
            string live = BaseMapStore.FolderIn(root);
            string old = live + OldSuffix;
            DeleteFolder(old);
            try
            {
                if (Directory.Exists(live)) Directory.Move(live, old);
                Directory.Move(staging, live);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                try
                {
                    if (!Directory.Exists(live) && Directory.Exists(old)) Directory.Move(old, live);   // put the data in use back
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
                DeleteFolder(staging);
                throw new ApiException("Không thay được dữ liệu bản đồ cũ (có thể một chương trình khác đang mở): " + ex.Message);
            }
            DeleteFolder(old);
        }

        private static void DeleteFolder(string folder)
        {
            try
            {
                if (Directory.Exists(folder)) Directory.Delete(folder, true);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private static string Hex(byte[] hash) => BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

        private static string Shorten(string text) => text.Length <= 60 ? text : text.Substring(0, 60) + "...";
    }
}
