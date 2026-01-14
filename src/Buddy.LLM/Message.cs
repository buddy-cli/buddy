namespace Buddy.LLM;

public sealed record Message(
    MessageRole Role,
    string? Content,
    string? ToolCallId = null,
    IReadOnlyList<ToolCall>? ToolCalls = null);
