using System;
using System.Drawing;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Hue_travel.Account;
using Hue_travel.Services.Online;
using Hue_travel.Theme;

namespace Hue_travel.Welcome
{
    /// <summary>
    /// "A new version is required": the window that updates the app itself. The update is mandatory, so the only ways out are the update
    /// button and "Thoát". The button downloads the installer with a progress bar, checks it and starts it; the app then closes and the
    /// installer opens it again. When something goes wrong the reason is shown and the button tries again.
    /// </summary>
    internal sealed class AppUpdateForm : ModalCard
    {
        private const int DesignWidth = 480;

        private readonly AppUpdater updater;
        private readonly ClientManifest manifest;
        private readonly Label status;
        private readonly Label detail;
        private readonly BaseMapDownloadForm.ProgressStrip bar = new BaseMapDownloadForm.ProgressStrip();
        private readonly IProgress<long> progress;
        private CancellationTokenSource cancel;
        private bool busy;

        public AppUpdateForm(AppUpdater updater, ClientManifest manifest)
            : base("Cần cập nhật ứng dụng", DesignWidth, "Cập nhật ngay", false, true)
        {
            this.updater = updater;
            this.manifest = manifest;
            progress = new Progress<long>(OnProgress);
            ShowInTaskbar = true;
            StartPosition = FormStartPosition.CenterScreen;
            Cancel.Text = "Thoát";

            status = new Label { AutoSize = false, Font = ThemeFonts.SemiBold(13), ForeColor = ThemeColors.Slate800, TextAlign = ContentAlignment.MiddleLeft };
            detail = new Label { AutoSize = false, Font = ThemeFonts.Regular(12), ForeColor = ThemeColors.Slate500, TextAlign = ContentAlignment.TopLeft };
            int width = Ui.S(DesignWidth - Side * 2);
            status.SetBounds(0, 0, width, Ui.S(24));
            bar.SetBounds(0, Ui.S(34), width, Ui.S(10));
            bar.Visible = false;
            detail.SetBounds(0, Ui.S(54), width, Ui.S(80));
            Content.Controls.Add(status);
            Content.Controls.Add(bar);
            Content.Controls.Add(detail);
            Arrange(138);
            ShowIntro();
        }

        /// <summary>True when the installer was started: the app has to close now.</summary>
        public bool Launched { get; private set; }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Activate();
        }

        // The right button starts (or restarts) the update; the window closes by itself once the installer is running.
        protected override bool CanAccept()
        {
            Start();
            return false;
        }

        // Closing while the download runs asks it to stop and waits for it.
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (busy && cancel != null)
            {
                e.Cancel = true;
                cancel.Cancel();
                Cancel.Enabled = false;
                status.Text = "Đang huỷ…";
                return;
            }
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && cancel != null) cancel.Dispose();
            base.Dispose(disposing);
        }

        private void ShowIntro()
        {
            status.Text = "Có phiên bản mới " + manifest.Version.ToString(3) + " (bạn đang dùng " + AppUpdater.CurrentVersion().ToString(3) + ")";
            detail.ForeColor = ThemeColors.Slate500;
            string notes = manifest.Notes.Length > 0 ? manifest.Notes + "\n\n" : "";
            detail.Text = notes + "Cần cập nhật để tiếp tục dùng ứng dụng (" + Megabytes(manifest.InstallerSize) + " MB). Ứng dụng sẽ tự cài và mở lại; Windows có thể hỏi quyền một lần.";
        }

        private async void Start()
        {
            if (busy) return;
            busy = true;
            Ok.Enabled = false;
            Cancel.Enabled = true;
            bar.Visible = true;
            bar.Value = 0;
            ShowIntro();
            status.Text = "Đang tải bản cập nhật…";
            if (cancel != null) cancel.Dispose();
            cancel = new CancellationTokenSource();
            await RunAsync(cancel.Token).ConfigureAwait(true);
        }

        private async Task RunAsync(CancellationToken ct)
        {
            try
            {
                string installer = await updater.DownloadAsync(manifest, progress, ct).ConfigureAwait(true);
                status.Text = "Đang mở bộ cài…";
                AppUpdater.Launch(installer);
                Launched = true;
                Finish(DialogResult.OK);
            }
            catch (OperationCanceledException)
            {
                Finish(DialogResult.Cancel);
            }
            catch (Exception ex)   // a refusal, a bad checksum, a refused permission prompt, anything else: shown, never a crash
            {
                if (ct.IsCancellationRequested) Finish(DialogResult.Cancel);
                else Fail(ex.Message);
            }
        }

        private void Finish(DialogResult result)
        {
            busy = false;
            if (!IsDisposed) DialogResult = result;
        }

        private void Fail(string message)
        {
            busy = false;
            if (IsDisposed) return;
            status.Text = "Chưa cập nhật được";
            detail.ForeColor = ThemeColors.Error;
            detail.Text = message;
            bar.Value = 0;
            Ok.Text = "Thử lại";
            Ok.Enabled = true;
            Cancel.Enabled = true;
        }

        private void OnProgress(long done)
        {
            if (IsDisposed || !busy || manifest.InstallerSize <= 0) return;
            double fraction = Math.Min(1.0, (double)done / manifest.InstallerSize);
            bar.Value = fraction;
            detail.ForeColor = ThemeColors.Slate500;
            detail.Text = Megabytes(done) + " / " + Megabytes(manifest.InstallerSize) + " MB (" + ((int)(fraction * 100)).ToString(CultureInfo.CurrentCulture) + "%)";
        }

        private static string Megabytes(long bytes) => (bytes / 1048576.0).ToString("0.0", CultureInfo.CurrentCulture);
    }
}
