using System.Text.RegularExpressions;

namespace Hue_travel.Auth
{
    /// <summary>Client-side checks for the account forms. Pure functions, no I/O.</summary>
    public static class AuthValidation
    {
        public const int MinPasswordLength = 6;

        private static readonly Regex Email = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
        private static readonly Regex VietnamPhone = new Regex(@"^(\+84|0)\d{9}$", RegexOptions.Compiled);

        public static bool IsEmail(string value) => !string.IsNullOrWhiteSpace(value) && Email.IsMatch(value.Trim());

        public static bool IsPhone(string value) => !string.IsNullOrWhiteSpace(value) && VietnamPhone.IsMatch(value.Trim().Replace(" ", ""));

        public static bool IsEmailOrPhone(string value) => IsEmail(value) || IsPhone(value);

        public static bool IsStrongEnoughPassword(string value) => value != null && value.Length >= MinPasswordLength;

        public static bool IsFullName(string value) => value != null && value.Trim().Length >= 2;
    }
}
