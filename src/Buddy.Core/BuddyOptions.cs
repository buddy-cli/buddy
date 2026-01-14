namespace Buddy.Core;

/// <summary>
/// Configuration inputs for the buddy agent.
/// Values will eventually be bound from CLI args, environment variables, and .env.
/// </summary>
public sealed class BuddyOptions {
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini";
    public string? BaseUrl { get; set; }
    public string WorkingDirectory { get; set; } = Directory.GetCurrentDirectory();
}
