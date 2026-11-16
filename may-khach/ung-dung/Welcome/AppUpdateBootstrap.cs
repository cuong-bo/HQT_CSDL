using System;
using System.Threading;
using System.Threading.Tasks;
using Hue_travel.Services.Online;

namespace Hue_travel.Welcome
{
    /// <summary>
    /// Runs first at start-up: asks the server whether a newer version of the app exists. If so the update is mandatory: the person
    /// updates (the installer then opens the app again) or the app closes. A server that does not answer, or has nothing published,
    /// is ignored: start-up never waits on it and an offline app still opens.
    /// </summary>
    internal static class AppUpdateBootstrap
    {
        private static readonly TimeSpan QuickCheck = TimeSpan.FromSeconds(4);

        /// <summary>False when the app must close now (the installer was started, or the person declined a mandatory update).</summary>
        public static bool Run(AppSession session)
        {
            AppUpdater.CleanUp();   // the installer an earlier update left behind
            if (!session.IsOnline) return true;

            using (var updater = new AppUpdater(session.Api, ServerSettings.BaseUrl))
            {
                ClientManifest offered = Ask(updater);
                if (!AppUpdater.IsNewer(offered, AppUpdater.CurrentVersion())) return true;

                using (var form = new AppUpdateForm(updater, offered))
                {
                    form.ShowDialog();
                    return false;
                }
            }
        }

        // null when the server does not answer in a few seconds or answers with something unusable.
        private static ClientManifest Ask(AppUpdater updater)
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
