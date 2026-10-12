namespace Hue_travel.Api.Data;

public static class InputRules
{
    /// <summary>True for an empty value or an absolute http(s) address. Blocks javascript:, file: and UNC paths that a desktop app might open.</summary>
    public static bool IsHttpUrlOrEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ||
        (value.Length <= 300 && Uri.TryCreate(value.Trim(), UriKind.Absolute, out Uri? uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps));
}
