using System.Text.Json.Serialization;

namespace Buddy.LLM.Api;

internal sealed class ApiToolDefinition {
    [JsonPropertyName("type")]
    public string Type { get; init; } = "function";

    [JsonPropertyName("function")]
    public ApiToolFunction Function { get; init; } = new();
}
