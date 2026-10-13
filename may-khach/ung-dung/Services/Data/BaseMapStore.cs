using System;
using System.IO;

namespace Hue_travel.Services.Data
{
    /// <summary>
    /// Where the map data lives on this computer: the shapefiles of the base map and of the places, downloaded from the server (see
    /// <see cref="Hue_travel.Services.Online.BaseMapUpdater"/>). They are kept under the user's own application-data folder, not next to
    /// the program: the program folder is read-only for a normal user, and the data changes without a new installer.
    /// </summary>
    public static class BaseMapStore
    {
        public const string VersionFileName = "version.txt";

        /// <summary>A layer every complete copy has; used to tell "installed" from "a folder that happens to exist".</summary>
        public const string MarkerFile = "rgXa.shp";

        /// <summary>%LocalAppData%\HueTravel\Data</summary>
        public static string DefaultRoot => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HueTravel", "Data");

        /// <summary>The folder the map and the place list are read from: <c>shp</c> inside <see cref="DefaultRoot"/>.</summary>
        public static string Folder => FolderIn(DefaultRoot);

        public static string FolderIn(string root) => Path.Combine(root, "shp");

        /// <summary>The version of the data in <paramref name="root"/>, or "" when none is installed (or the copy is incomplete).</summary>
        public static string InstalledVersion(string root)
        {
            try
            {
                string folder = FolderIn(root);
                string versionFile = Path.Combine(folder, VersionFileName);
                if (!File.Exists(versionFile) || !File.Exists(Path.Combine(folder, MarkerFile))) return "";
                return File.ReadAllText(versionFile).Trim();
            }
            catch (IOException)
            {
                return "";
            }
            catch (UnauthorizedAccessException)
            {
                return "";
            }
        }

        public static string InstalledVersion() => InstalledVersion(DefaultRoot);

        public static bool IsInstalled => InstalledVersion().Length > 0;
    }
}
