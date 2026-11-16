namespace Hue_travel.Api.Chat;

/// <summary>
/// Settings of the assistant (section "Chat"). The OpenCode API key comes from the environment (Chat__ApiKey) and is never in a file of the
/// repository. Without a key the assistant is simply off (503); the rest of the server is not affected.
/// </summary>
public sealed class ChatOptions
{
    public const string SectionName = "Chat";

    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://opencode.ai/inference/openai/v1";   // OpenCode's OpenAI-compatible endpoint; "/chat/completions" is added to it
    public string Model { get; set; } = "deepseek-v4.1-flash";   // DeepSeek V4.1 Flash on OpenCode (the older "V4 Flash" is deprecated); only this setting changes
    public double Temperature { get; set; } = 0.3;             // places, prices and times are facts: little creativity
    public int MaxOutputTokens { get; set; } = 700;
    public int TimeoutSeconds { get; set; } = 45;
    public int MaxToolRounds { get; set; } = 4;
    public int GuestPerDay { get; set; } = 20;                 // per client address
    public int UserPerDay { get; set; } = 100;                 // per account
    public long GlobalTokensPerDay { get; set; } = 2_000_000;  // a fuse for the whole server

    public bool Enabled => !string.IsNullOrWhiteSpace(ApiKey);
}
