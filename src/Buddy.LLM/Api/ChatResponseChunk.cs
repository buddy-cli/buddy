namespace Buddy.LLM.Api;

public sealed record ChatResponseChunk(string? TextDelta, ToolCallDelta? ToolCall) {
    public static ChatResponseChunk FromText(string text) => new(text, null);
    public static ChatResponseChunk FromToolCall(ToolCallDelta toolCall) => new(null, toolCall);
}
