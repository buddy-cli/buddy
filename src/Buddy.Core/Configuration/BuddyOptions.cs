namespace Buddy.Core.Configuration;

/// <summary>
/// Configuration inputs for the buddy agent.
/// Values are bound from the buddy config file.
/// </summary>
public sealed class BuddyOptions {
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string? BaseUrl { get; set; }
    public string WorkingDirectory { get; set; } = Directory.GetCurrentDirectory();
    public List<LlmProviderConfig> Providers { get; set; } = new();
}

public sealed class LlmProviderConfig {
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public List<LlmModelConfig> Models { get; set; } = new();
}

public sealed class LlmModelConfig {
    public string Name { get; set; } = string.Empty;
    public string System { get; set; } = string.Empty;
}
