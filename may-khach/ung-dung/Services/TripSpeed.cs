using System;
using System.Globalization;
using System.IO;

namespace Hue_travel.Services
{
    /// <summary>
    /// The speeds of the toy car on the map: x1 drives a trip in its real time, x2, x4 and x8 are faster. The last choice is kept in
    /// <c>%LocalAppData%\HueTravel\trip-speed.txt</c> (one number), so a 20-minute trip is not a 20-minute wait every time.
    /// </summary>
    public static class TripSpeed
    {
        public static readonly double[] Choices = { 1, 2, 4, 8 };

        /// <summary>The speed of a first run: x8 (a 20-minute trip drives in 2.5 minutes).</summary>
        public const double Default = 8;

        public static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HueTravel", "trip-speed.txt");

        /// <summary>The next speed in the cycle x1, x2, x4, x8, x1; anything unknown goes to <see cref="Default"/>.</summary>
        public static double Next(double current)
        {
            int index = Array.IndexOf(Choices, current);
            return index < 0 ? Default : Choices[(index + 1) % Choices.Length];
        }

        public static string Label(double speed) => "x" + speed.ToString("0.##", CultureInfo.InvariantCulture);

        /// <summary>The remembered speed, or <see cref="Default"/> when there is none or it is not one of the choices.</summary>
        public static double Load(string path = null)
        {
            try
            {
                string text = File.ReadAllText(path ?? FilePath).Trim();
                double value;
                if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && Array.IndexOf(Choices, value) >= 0) return value;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                System.Diagnostics.Debug.WriteLine("TripSpeed.Load: " + ex.Message);
            }
            return Default;
        }

        /// <summary>Remembers the speed; failing to write is not an error (the choice is only a convenience).</summary>
        public static void Save(double speed, string path = null)
        {
            try
            {
                string target = path ?? FilePath;
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.WriteAllText(target, speed.ToString("0.##", CultureInfo.InvariantCulture));
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                System.Diagnostics.Debug.WriteLine("TripSpeed.Save: " + ex.Message);
            }
        }
    }
}
