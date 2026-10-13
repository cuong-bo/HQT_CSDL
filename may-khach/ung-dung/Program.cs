using System;
using System.Windows.Forms;
using Hue_travel.Services.Online;
using Hue_travel.Shell;
using Hue_travel.Welcome;

namespace Hue_travel
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine(e.Exception);
                MessageBox.Show("Đã xảy ra lỗi không mong muốn: " + e.Exception.Message + "\n\nỨng dụng vẫn chạy. Nếu lỗi lặp lại, hãy khởi động lại ứng dụng.",
                    "Hue Travel", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            };
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Sign in first, then the main window. Signing out (or an ended session) comes back to the sign-in screen.
            AppSession session = AppSession.Create();

            // The map data is downloaded from the server (the first time, and when a newer version is published) before anything uses it.
            if (!BaseMapBootstrap.Run(session)) return;

            while (true)
            {
                using (var login = new LoginForm(session))
                {
                    if (login.ShowDialog() != DialogResult.OK) return;
                }
                session.Notice = null;
                session.ReturnToSignIn = false;
                using (var main = new MainForm(session)) Application.Run(main);
                if (!session.ReturnToSignIn) return;
            }
        }
    }
}
