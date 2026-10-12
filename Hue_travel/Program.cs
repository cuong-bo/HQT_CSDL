using System;
using System.Windows.Forms;
using Hue_travel.Shell;

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
                    "Cố Đô GIS", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            };
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
