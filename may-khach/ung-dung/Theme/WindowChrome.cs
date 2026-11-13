using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Hue_travel.Theme
{
    /// <summary>
    /// Rounds the corners of the borderless main window. Windows 11 rounds a borderless window by itself (about 8 px, smooth);
    /// older Windows gets a small rounded Region instead. A maximised window is square. The big 20-28 px radii of the design
    /// apply to what is INSIDE the window (cards, panels): a top-level window cannot be rounded that much smoothly.
    /// Call <see cref="Attach"/> once, in the constructor of the form.
    /// </summary>
    public static class WindowChrome
    {
        private const int DwmWindowCornerPreference = 33;
        private const int DwmDoNotRound = 1, DwmRound = 2;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        public static void Attach(Form form)
        {
            form.HandleCreated += (s, e) => Apply(form);
            form.SizeChanged += (s, e) => Apply(form);
        }

        private static void Apply(Form form)
        {
            if (!form.IsHandleCreated || form.Width <= 0 || form.Height <= 0) return;
            bool maximised = form.WindowState == FormWindowState.Maximized;
            bool system = false;
            try
            {
                int preference = maximised ? DwmDoNotRound : DwmRound;
                system = DwmSetWindowAttribute(form.Handle, DwmWindowCornerPreference, ref preference, sizeof(int)) == 0;
            }
            catch (DllNotFoundException) { }
            catch (EntryPointNotFoundException) { }

            if (system || maximised)
            {
                if (form.Region != null) form.Region = null;
                return;
            }
            using (GraphicsPath path = Ui.RoundedRect(new RectangleF(0, 0, form.Width, form.Height), Ui.S(8)))
            {
                form.Region = new Region(path);
            }
        }
    }
}
