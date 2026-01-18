using Buddy.LLM.Api;

namespace Buddy.LLM;

public interface ILlmMClient_ {
    IAsyncEnumerable<ChatResponseChunk> GetStreamingResponseAsync(
        IReadOnlyList<Message> messages,
        IReadOnlyList<ToolDefinition> tools,
        CancellationToken cancellationToken = default);
}
