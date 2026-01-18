using System.Text.Json.Serialization;

namespace Buddy.LLM.Api;

internal sealed class ApiToolCallFunction {
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("arguments")]
    public string Arguments { get; init; } = string.Empty;
}
