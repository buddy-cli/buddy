using System.Text.Json.Serialization;

namespace Buddy.LLM.Api;

internal sealed class ApiToolCall {
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; init; } = "function";

    [JsonPropertyName("function")]
    public ApiToolCallFunction Function { get; init; } = new();
}
