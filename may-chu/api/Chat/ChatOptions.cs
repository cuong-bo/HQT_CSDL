namespace Hue_travel.Api.Chat;

/// <summary>
/// Settings for the assistant. Unlike the JWT key, a missing DeepSeek key does not stop the server: it only switches the
/// assistant off, so every other part of the app keeps working.
/// </summary>
public sealed class ChatOptions
{
    public const string SectionName = "Chat";

    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.deepseek.com";
    public string Model { get; set; } = "deepseek-flash";
    public string Thinking { get; set; } = "disabled";   // no long reasoning pause for a travel question

    public double Temperature { get; set; } = 0.3;
    public int MaxOutputTokens { get; set; } = 700;
    public int TimeoutSeconds { get; set; } = 45;
    public int MaxToolRounds { get; set; } = 4;

    public int GuestPerDay { get; set; } = 20;
    public int UserPerDay { get; set; } = 100;
    public long GlobalTokensPerDay { get; set; } = 2_000_000;

    public bool Enabled => !string.IsNullOrWhiteSpace(ApiKey);
}
