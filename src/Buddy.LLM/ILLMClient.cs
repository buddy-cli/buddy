namespace Buddy.LLM;

public interface ILLMClient {
    IAsyncEnumerable<ChatResponseChunk> GetStreamingResponseAsync(
        IReadOnlyList<Message> messages,
        IReadOnlyList<ToolDefinition> tools,
        CancellationToken cancellationToken = default);
}
