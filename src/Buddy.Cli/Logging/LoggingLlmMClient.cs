using System.Text;
using Buddy.LLM;
using Buddy.LLM.Api;

namespace Buddy.Cli.Logging;

internal sealed class LoggingLlmMClient : ILlmMClient {
    private readonly ILlmMClient _inner;
    private readonly MarkdownSessionLogger _logger;
    private readonly Func<string> _modelNameProvider;

    public LoggingLlmMClient(ILlmMClient inner, MarkdownSessionLogger logger, Func<string> modelNameProvider) {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _modelNameProvider = modelNameProvider ?? throw new ArgumentNullException(nameof(modelNameProvider));
    }

    public async IAsyncEnumerable<ChatResponseChunk> GetStreamingResponseAsync(
        IReadOnlyList<Message> messages,
        IReadOnlyList<ToolDefinition> tools,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default) {
        var responseBuilder = new StringBuilder();
        var toolCallAccumulator = new StreamingToolCallAccumulator();
        var call = _logger.BeginCall(_modelNameProvider(), messages, tools);
        await using var enumerator = _inner.GetStreamingResponseAsync(messages, tools, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);

        while (true) {
            ChatResponseChunk chunk;
            try {
                if (!await enumerator.MoveNextAsync()) {
                    break;
                }

                chunk = enumerator.Current;
            }
            catch (Exception ex) {
                _logger.FailCall(call, responseBuilder.ToString(), toolCallAccumulator.ToToolCalls(), ex);
                throw;
            }

            if (chunk.TextDelta is { Length: > 0 } text) {
                responseBuilder.Append(text);
            }

            if (chunk.ToolCall is not null) {
                toolCallAccumulator.Apply(chunk.ToolCall);
            }

            yield return chunk;
        }

        _logger.CompleteCall(call, responseBuilder.ToString(), toolCallAccumulator.ToToolCalls());
    }
}
