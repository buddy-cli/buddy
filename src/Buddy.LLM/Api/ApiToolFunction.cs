using System.Text.Json;
using System.Text.Json.Serialization;

namespace Buddy.LLM;

internal sealed class ApiToolFunction {
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("parameters")]
    public JsonElement Parameters { get; init; }
}
