namespace Buddy.LLM;

public sealed record ToolCall(string Id, string Name, string ArgumentsJson);
