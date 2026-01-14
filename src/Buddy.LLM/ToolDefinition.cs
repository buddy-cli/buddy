using System.Text.Json;

namespace Buddy.LLM;

public sealed record ToolDefinition(string Name, string Description, JsonElement ParameterSchema);
