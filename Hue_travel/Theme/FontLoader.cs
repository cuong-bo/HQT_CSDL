using System;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Hue_travel.Theme
{
    /// <summary>
    /// Loads the fonts embedded in the assembly (Plus Jakarta Sans + Material Symbols subset)
    /// so the app does not depend on installed fonts. Each font is registered twice: with a
    /// GDI+ PrivateFontCollection (for DrawString/Font(FontFamily)) and with GDI
    /// (AddFontMemResourceEx) so WinForms controls rendering through TextRenderer resolve
    /// the same family names.
    /// </summary>
    public static class FontLoader
    {
        private const string ResourcePrefix = "Hue_travel.Resources.";
        private static readonly string[] FontFiles =
        {
            "MaterialSymbolsSubset.ttf",
            "PlusJakartaSans-Regular.ttf",
            "PlusJakartaSans-Medium.ttf",
            "PlusJakartaSans-SemiBold.ttf",
            "PlusJakartaSans-Bold.ttf",
        };

        [DllImport("gdi32.dll")]
        private static extern IntPtr AddFontMemResourceEx(IntPtr pbFont, uint cbFont, IntPtr pdv, ref uint pcFonts);

        /// <summary>Messages for fonts that failed to load (empty when everything loaded).</summary>
        public static string LoadErrors { get; private set; } = "";

        private static readonly object Gate = new object();
        private static PrivateFontCollection collection;

        /// <summary>The Material Symbols family, or null if the font could not be loaded.</summary>
        public static FontFamily IconFamily => Find("Material Symbols Outlined");

        /// <summary>The named embedded family (e.g. "Plus Jakarta Sans"), or null if unavailable.</summary>
        public static FontFamily Find(string familyName)
        {
            EnsureLoaded();
            if (collection == null) return null;
            foreach (FontFamily family in collection.Families)
            {
                if (string.Equals(family.Name, familyName, StringComparison.OrdinalIgnoreCase)) return family;
            }
            return null;
        }

        private static void EnsureLoaded()
        {
            if (collection != null) return;
            lock (Gate)
            {
                if (collection != null) return;
                var loaded = new PrivateFontCollection();
                Assembly assembly = typeof(FontLoader).Assembly;
                foreach (string file in FontFiles)
                {
                    try { AddFont(loaded, assembly, file); }
                    catch (Exception ex)
                    {
                        // A missing font must not stop the app (or the WinForms designer); text
                        // falls back to Segoe UI and icons render blank.
                        System.Diagnostics.Debug.WriteLine("FontLoader: " + file + ": " + ex.Message);
                        LoadErrors += file + ": " + ex.Message + "\n";
                    }
                }
                collection = loaded;
            }
        }

        private static void AddFont(PrivateFontCollection target, Assembly assembly, string file)
        {
            using (Stream stream = assembly.GetManifestResourceStream(ResourcePrefix + file))
            {
                if (stream == null) throw new FileNotFoundException("Embedded font not found", file);

                var data = new byte[stream.Length];
                int read = 0;
                while (read < data.Length)
                {
                    int n = stream.Read(data, read, data.Length - read);
                    if (n <= 0) break;
                    read += n;
                }

                // The memory must stay valid for the lifetime of the process, so it is never freed.
                IntPtr memory = Marshal.AllocCoTaskMem(data.Length);
                Marshal.Copy(data, 0, memory, data.Length);
                target.AddMemoryFont(memory, data.Length);

                uint fontCount = 0;
                AddFontMemResourceEx(memory, (uint)data.Length, IntPtr.Zero, ref fontCount);
            }
        }
    }
}
