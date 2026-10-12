using System.Text;
using System.Text.RegularExpressions;

namespace Hue_travel.Api.Auth;

/// <summary>Input rules for accounts. Messages are shown to the person typing, so they are in Vietnamese.</summary>
public static partial class AuthValidation
{
    public const int MinPasswordLength = 8;
    public const int MaxPasswordBytes = 72;   // bcrypt ignores everything after 72 bytes
    public const int MaxDisplayNameLength = 60;

    [GeneratedRegex("^[A-Za-z0-9_.]{3,40}$")]
    private static partial Regex UsernamePattern();

    /// <summary>Returns the first problem found, or null when the registration is acceptable.</summary>
    public static string? ValidateRegistration(string? username, string? password, string? displayName)
    {
        if (string.IsNullOrWhiteSpace(username) || !UsernamePattern().IsMatch(username))
            return "Tên đăng nhập gồm 3 đến 40 ký tự: chữ, số, dấu gạch dưới hoặc dấu chấm.";
        if (string.IsNullOrEmpty(password) || password.Length < MinPasswordLength)
            return $"Mật khẩu cần ít nhất {MinPasswordLength} ký tự.";
        if (Encoding.UTF8.GetByteCount(password) > MaxPasswordBytes)
            return "Mật khẩu quá dài (tối đa 72 byte).";
        if (displayName is { Length: > MaxDisplayNameLength })
            return $"Tên hiển thị tối đa {MaxDisplayNameLength} ký tự.";
        return null;
    }

    public static string DisplayNameOrUsername(string? displayName, string username) =>
        string.IsNullOrWhiteSpace(displayName) ? username : displayName.Trim();
}
