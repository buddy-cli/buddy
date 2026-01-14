using System.IO;

namespace Buddy.Core;

/// <summary>
/// Configuration inputs for the buddy agent.
/// Values will eventually be bound from CLI args, environment variables, and .env.
/// </summary>
public sealed class BuddyOptions
{
    public string ApiKey { get; init; } = string.Empty;
    public string Model { get; init; } = "gpt-4o-mini";
    public string? BaseUrl { get; init; }
    public string WorkingDirectory { get; init; } = Directory.GetCurrentDirectory();
}
