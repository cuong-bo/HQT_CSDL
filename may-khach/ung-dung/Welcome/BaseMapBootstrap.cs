using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Hue_travel.Services.Data;
using Hue_travel.Services.Online;

namespace Hue_travel.Welcome
{
    /// <summary>
    /// Runs once at start-up, before the sign-in screen: makes sure the map data is on this computer and current.
    /// The first run has none and downloads it (nothing can be shown without it). Later runs ask the server briefly what the latest
    /// version is; the person decides whether to fetch a newer one now, and a server that does not answer is simply ignored.
    /// This is done here, before the map exists, because the map holds its files open and a swap of the folders needs them closed.
    /// </summary>
    internal static class BaseMapBootstrap
    {
        private static readonly TimeSpan QuickCheck = TimeSpan.FromSeconds(4);

        /// <summary>False when the app cannot go on and should close (the first run, and the person gave up on the download).</summary>
        public static bool Run(AppSession session)
        {
            BaseMapUpdater.Recover();   // undo what a crash in the middle of an earlier swap left behind
            string installed = BaseMapStore.InstalledVersion();

            if (!session.IsOnline)
            {
                if (installed.Length == 0)
                    MessageBox.Show("Chưa có dữ liệu bản đồ trên máy này và chưa có địa chỉ máy chủ để tải.\n\nHãy đặt địa chỉ máy chủ (khi cài đặt, hoặc biến môi trường HUE_API_URL) rồi mở lại. " +
                                    "Bây giờ ứng dụng vẫn mở được nhưng bản đồ sẽ trống.", "Hue Travel", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }

            using (var updater = new BaseMapUpdater(session.Api, ServerSettings.BaseUrl))
            {
                BaseMapManifest offered = null;
                if (installed.Length > 0)
                {
                    offered = Ask(updater);
                    if (offered == null || offered.Version == installed) return true;
                    string question = "Có dữ liệu bản đồ mới (khoảng " + (offered.PackageSize / 1048576.0).ToString("0.0") + " MB).\n\nTải về ngay? Chọn \"Không\" để dùng bản hiện có và hỏi lại lần sau.";
                    if (MessageBox.Show(question, "Hue Travel", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return true;
                }

                using (var form = new BaseMapDownloadForm(updater, offered, installed.Length == 0))
                {
                    form.ShowDialog();
                    if (form.Installed) return true;
                }
                // An update that was postponed or failed leaves the data in use as it was; with no data at all there is nothing to show.
                return installed.Length > 0;
            }
        }

        // null when the server does not answer in a few seconds or answers with something unusable: start-up must not wait on it.
        private static BaseMapManifest Ask(BaseMapUpdater updater)
        {
            try
            {
                using (var timeout = new CancellationTokenSource(QuickCheck))
                    return Task.Run(() => updater.FetchManifestAsync(timeout.Token)).GetAwaiter().GetResult();
            }
            catch (ApiException)
            {
                return null;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }
    }
}
