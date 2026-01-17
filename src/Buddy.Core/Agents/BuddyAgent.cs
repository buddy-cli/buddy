using System.Text;
using Buddy.Core.Tooling;
using Buddy.LLM;

namespace Buddy.Core.Agents;

public sealed class BuddyAgent(ToolRegistry toolRegistry) {
    private readonly List<Message> _history = new();

    public void ClearHistory() => _history.Clear();

    public async Task RunTurnAsync(
        ILLMClient llmClient,
        string systemPrompt,
        string? projectInstructions,
        string userInput,
        Func<string, Task> onTextDelta,
        Func<string, Task> onToolStatus,
        CancellationToken cancellationToken) {
        _history.Add(new Message(MessageRole.User, userInput));

        var tools = toolRegistry.GetToolDefinitions();
        var toolNames = tools.Select(t => t.Name).ToList();

        // Loop: LLM -> tool calls -> tool results -> LLM
        for (var round = 0; round < 50; round++) {
            var messages = BuildMessageList(systemPrompt, projectInstructions);
            var collector = new StreamingToolCallCollector(toolNames);

            await foreach (var chunk in llmClient.GetStreamingResponseAsync(messages, tools, cancellationToken)) {
                if (chunk.ToolCall is not null) {
                    collector.ProcessToolCallDelta(chunk.ToolCall);
                }

                if (chunk.TextDelta is { Length: > 0 } text) {
                    var displayText = collector.ProcessTextDelta(text);
                    if (displayText is { Length: > 0 }) {
                        await onTextDelta(displayText);
                    }
                }
            }

            var (toolCalls, remainingText) = collector.Finalize();

            if (remainingText is { Length: > 0 }) {
                await onTextDelta(remainingText);
            }

            if (toolCalls.Length == 0) {
                if (collector.DisplayText.Length > 0) {
                    _history.Add(new Message(MessageRole.Assistant, collector.DisplayText));
                }

                return;
            }

            // Add assistant message that requested tools.
            _history.Add(new Message(
                MessageRole.Assistant,
                collector.DisplayText.Length == 0 ? null : collector.DisplayText,
                ToolCallId: null,
                ToolCalls: toolCalls));

            // Execute tools sequentially and append tool messages.
            foreach (var tc in toolCalls) {
                var statusLine = toolRegistry.FormatStatusLine(tc.Name, tc.ArgumentsJson);
                await onToolStatus($"→ {statusLine}");

                var result = await toolRegistry.ExecuteAsync(tc.Name, tc.ArgumentsJson, cancellationToken);
                _history.Add(new Message(MessageRole.Tool, result, ToolCallId: tc.Id));
            }
        }

        await onToolStatus("warning: max tool loop rounds reached");
    }

    private List<Message> BuildMessageList(string systemPrompt, string? instructions) {
        var list = new List<Message>(_history.Count + 1);

        var sys = systemPrompt;
        if (!string.IsNullOrWhiteSpace(instructions)) {
            sys += "\n\nProject instructions:\n" + instructions;
        }

        list.Add(new Message(MessageRole.System, sys));
        list.AddRange(_history);
        return list;
    }
}
