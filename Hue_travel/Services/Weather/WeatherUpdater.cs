using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Hue_travel.Services.Weather
{
    /// <summary>
    /// Keeps the status-bar weather text fresh: refreshes on a timer, keeps the last good reading when a refresh
    /// fails (for a while), and reports a clear "no data" text instead of stale or invented values.
    /// Create and use it on the UI thread; <see cref="TextChanged"/> is raised there too.
    /// </summary>
    public sealed class WeatherUpdater : IDisposable
    {
        public const string LoadingText = "Đang cập nhật thời tiết...";
        public const string UnavailableText = "Chưa có dữ liệu thời tiết";

        private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(20);
        private static readonly TimeSpan RetryInterval = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan MaxStaleAge = TimeSpan.FromHours(3);

        private readonly IWeatherService service;
        private readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private WeatherSnapshot lastGood;
        private bool isRefreshing;

        public WeatherUpdater(IWeatherService weatherService)
        {
            service = weatherService ?? throw new ArgumentNullException(nameof(weatherService));
            Text = LoadingText;
            timer.Tick += async (s, e) => await RefreshAsync();
        }

        public event EventHandler<string> TextChanged;

        public string Text { get; private set; }

        public async void Start() => await RefreshAsync();

        private async Task RefreshAsync()
        {
            if (isRefreshing || cancellation.IsCancellationRequested) return;
            isRefreshing = true;
            timer.Stop();
            try
            {
                lastGood = await service.GetCurrentAsync(cancellation.Token);
                Publish(WeatherDescriber.Describe(lastGood.WeatherCode, lastGood.IsDay, lastGood.TemperatureC));
                timer.Interval = (int)RefreshInterval.TotalMilliseconds;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                // A background refresh must never take the app down: any failure means "no fresh reading".
                Debug.WriteLine("WeatherUpdater: " + ex.Message);
                bool keepLast = lastGood != null && DateTime.UtcNow - lastGood.FetchedAtUtc < MaxStaleAge;
                if (!keepLast) Publish(UnavailableText);
                timer.Interval = (int)RetryInterval.TotalMilliseconds;
            }
            finally
            {
                isRefreshing = false;
            }
            if (!cancellation.IsCancellationRequested) timer.Start();
        }

        private void Publish(string text)
        {
            if (text == Text) return;
            Text = text;
            TextChanged?.Invoke(this, text);
        }

        public void Dispose()
        {
            cancellation.Cancel();
            timer.Dispose();
            cancellation.Dispose();
        }
    }
}
