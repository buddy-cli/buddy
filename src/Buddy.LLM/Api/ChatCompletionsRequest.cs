using System.Text.Json.Serialization;

namespace Buddy.LLM.Api;

// Minimal DTOs for request serialization.
internal sealed class ChatCompletionsRequest {
    [JsonPropertyName("model")]
    public string Model { get; init; } = string.Empty;

    [JsonPropertyName("stream")]
    public bool Stream { get; init; }

    [JsonPropertyName("messages")]
    public ApiMessage[] Messages { get; init; } = [];

    [JsonPropertyName("tools")]
    public ApiToolDefinition[]? Tools { get; init; }
}
