using System;
using System.Drawing;
using System.Drawing.Drawing2D;
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
    /// The window that downloads the map data from the server, in the style of the other small windows: a status line, a progress bar and
    /// the amount done. The first time the app runs there is no map at all, so there is no way to go on without it (the left button
    /// then quits); for an update the left button simply postpones it. When something goes wrong the reason is shown and the right button
    /// tries again.
    /// </summary>
    internal sealed class BaseMapDownloadForm : ModalCard
    {
        private const int DesignWidth = 480;

        private readonly BaseMapUpdater updater;
        private readonly bool firstRun;
        private readonly Label status;
        private readonly Label detail;
        private readonly ProgressStrip bar = new ProgressStrip();
        private readonly IProgress<BaseMapProgress> progress;
        private BaseMapManifest manifest;
        private CancellationTokenSource cancel;
        private bool busy;   // true from the moment a transfer starts until it has finished, failed or been cancelled

        /// <param name="manifest">What to download; null to ask the server first (the first run, where nothing was asked before).</param>
        /// <param name="firstRun">True when there is no map data on this computer yet.</param>
        public BaseMapDownloadForm(BaseMapUpdater updater, BaseMapManifest manifest, bool firstRun)
            : base(firstRun ? "Tải dữ liệu bản đồ" : "Cập nhật dữ liệu bản đồ", DesignWidth, "Thử lại", false, true)
        {
            this.updater = updater;
            this.manifest = manifest;
            this.firstRun = firstRun;
            progress = new Progress<BaseMapProgress>(OnProgress);
            ShowInTaskbar = true;
            StartPosition = FormStartPosition.CenterScreen;
            Cancel.Text = firstRun ? "Thoát" : "Để sau";
            Ok.Visible = false;

            status = new Label { AutoSize = false, Font = ThemeFonts.SemiBold(13), ForeColor = ThemeColors.Slate800, TextAlign = ContentAlignment.MiddleLeft };
            detail = new Label { AutoSize = false, Font = ThemeFonts.Regular(12), ForeColor = ThemeColors.Slate500, TextAlign = ContentAlignment.TopLeft };
            int width = Ui.S(DesignWidth - Side * 2);
            status.SetBounds(0, 0, width, Ui.S(24));
            bar.SetBounds(0, Ui.S(34), width, Ui.S(10));
            detail.SetBounds(0, Ui.S(54), width, Ui.S(40));
            Content.Controls.Add(status);
            Content.Controls.Add(bar);
            Content.Controls.Add(detail);
            Arrange(98);
            ShowIntro();
        }

        /// <summary>True when the data was downloaded and installed.</summary>
        public bool Installed { get; private set; }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Activate();
            Start();
        }

        // The right button only appears after a failure, and then means "try again": it must not close the window.
        protected override bool CanAccept()
        {
            Start();
            return false;
        }

        // Closing while the transfer runs asks it to stop and waits for it: the swap of the data folders must never be cut in half.
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
            status.Text = firstRun ? "Lần đầu mở ứng dụng cần tải bản đồ từ máy chủ" : "Có dữ liệu bản đồ mới";
            detail.ForeColor = ThemeColors.Slate500;
            detail.Text = firstRun ? "Chỉ tải một lần, các lần sau chỉ tải phần cập nhật. Vui lòng giữ kết nối mạng." : "";
        }

        private async void Start()
        {
            if (busy) return;
            busy = true;
            Ok.Visible = false;
            Cancel.Enabled = true;
            bar.Value = 0;
            ShowIntro();
            status.Text = "Đang kết nối máy chủ…";
            if (cancel != null) cancel.Dispose();
            cancel = new CancellationTokenSource();
            await RunAsync(cancel.Token).ConfigureAwait(true);
        }

        private async Task RunAsync(CancellationToken ct)
        {
            try
            {
                if (manifest == null) manifest = await updater.FetchManifestAsync(ct).ConfigureAwait(true);
                detail.Text = Megabytes(manifest.PackageSize) + " MB";
                await updater.InstallAsync(manifest, progress, ct).ConfigureAwait(true);
                Installed = true;
                Finish(DialogResult.OK);
            }
            catch (OperationCanceledException)
            {
                Finish(DialogResult.Cancel);
            }
            catch (Exception ex)   // a refusal, a locked file, a damaged package, anything else: shown, never a crash at start-up
            {
                // The API client reports a cancelled request like an unreachable server: when the person asked to stop, it is a stop.
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
            if (firstRun) manifest = null;   // ask the server again: what it offers may have changed meanwhile
            status.Text = "Không tải được dữ liệu bản đồ";
            detail.ForeColor = ThemeColors.Error;
            detail.Text = message;
            bar.Value = 0;
            Ok.Visible = true;
            Cancel.Enabled = true;
        }

        private void OnProgress(BaseMapProgress p)
        {
            if (IsDisposed || !busy || p.Total <= 0) return;
            double fraction = Math.Min(1.0, (double)p.Done / p.Total);
            bar.Value = fraction;
            string percent = ((int)(fraction * 100)).ToString(CultureInfo.CurrentCulture) + "%";
            status.Text = p.Phase == BaseMapPhase.Downloading ? "Đang tải dữ liệu bản đồ…" : "Đang cài đặt…";
            detail.ForeColor = ThemeColors.Slate500;
            detail.Text = Megabytes(p.Done) + " / " + Megabytes(p.Total) + " MB (" + percent + ")";
        }

        private static string Megabytes(long bytes) => (bytes / 1048576.0).ToString("0.0", CultureInfo.CurrentCulture);

        /// <summary>A thin rounded bar, filled from the left.</summary>
        private sealed class ProgressStrip : Control
        {
            private double value;

            public ProgressStrip()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                BackColor = Color.White;
            }

            public double Value
            {
                get { return value; }
                set
                {
                    this.value = Math.Max(0, Math.Min(1, value));
                    Invalidate();
                }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                float radius = Height / 2f;
                var track = new RectangleF(0, 0, Width - 1, Height - 1);
                using (GraphicsPath path = Ui.RoundedRect(track, radius))
                using (var brush = new SolidBrush(ThemeColors.Slate200)) e.Graphics.FillPath(brush, path);
                if (value <= 0) return;
                var fill = new RectangleF(0, 0, Math.Max(Height - 1, (float)((Width - 1) * value)), Height - 1);
                using (GraphicsPath path = Ui.RoundedRect(fill, radius))
                using (var brush = new SolidBrush(ThemeColors.Primary)) e.Graphics.FillPath(brush, path);
            }
        }
    }
}
