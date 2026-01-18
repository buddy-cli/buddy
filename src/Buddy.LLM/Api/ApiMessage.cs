using System.Text.Json.Serialization;

namespace Buddy.LLM.Api;

internal sealed class ApiMessage {
    [JsonPropertyName("role")]
    public string Role { get; init; } = string.Empty;

    [JsonPropertyName("content")]
    public string? Content { get; init; }

    [JsonPropertyName("tool_call_id")]
    public string? ToolCallId { get; init; }

    [JsonPropertyName("tool_calls")]
    public ApiToolCall[]? ToolCalls { get; init; }
}
