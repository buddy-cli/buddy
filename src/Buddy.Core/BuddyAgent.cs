using System.Text;
using Buddy.Core.Tools;
using Buddy.LLM;

namespace Buddy.Core;

public sealed class BuddyAgent
{
    private readonly ToolRegistry _toolRegistry;

    private readonly List<Message> _history = new();

    public BuddyAgent(ToolRegistry toolRegistry)
    {
        _toolRegistry = toolRegistry;
    }

    public void ClearHistory() => _history.Clear();

    public async Task RunTurnAsync(
        ILLMClient llmClient,
        string systemPrompt,
        string? projectInstructions,
        string userInput,
        Func<string, Task> onTextDelta,
        Func<string, Task> onToolStatus,
        CancellationToken cancellationToken)
    {
        _history.Add(new Message(MessageRole.User, userInput));

        var tools = _toolRegistry.GetToolDefinitions();

        // Loop: LLM -> tool calls -> tool results -> LLM
        for (var round = 0; round < 8; round++)
        {
            var messages = BuildMessageList(systemPrompt, projectInstructions);

            var assistantText = new StringBuilder();
            var toolAccumulators = new Dictionary<int, ToolCallAccumulator>();

            await foreach (var chunk in llmClient.GetStreamingResponseAsync(messages, tools, cancellationToken))
            {
                if (chunk.TextDelta is { Length: > 0 } text)
                {
                    assistantText.Append(text);
                    await onTextDelta(text);
                }

                if (chunk.ToolCall is not null)
                {
                    var delta = chunk.ToolCall;
                    if (!toolAccumulators.TryGetValue(delta.Index, out var acc))
                    {
                        acc = new ToolCallAccumulator(delta.Index);
                        toolAccumulators[delta.Index] = acc;
                    }

                    acc.Apply(delta);
                }
            }

            var toolCalls = toolAccumulators.Values
                .OrderBy(a => a.Index)
                .Select(a => a.ToToolCall())
                .ToArray();

            if (toolCalls.Length == 0)
            {
                if (assistantText.Length > 0)
                {
                    _history.Add(new Message(MessageRole.Assistant, assistantText.ToString()));
                }

                return;
            }

            // Add assistant message that requested tools.
            _history.Add(new Message(
                MessageRole.Assistant,
                assistantText.Length == 0 ? null : assistantText.ToString(),
                ToolCallId: null,
                ToolCalls: toolCalls));

            // Execute tools sequentially and append tool messages.
            foreach (var tc in toolCalls)
            {
                await onToolStatus($"→ {tc.Name}({SummarizeArgs(tc.ArgumentsJson)})");

                var result = await _toolRegistry.ExecuteAsync(tc.Name, tc.ArgumentsJson, cancellationToken);
                _history.Add(new Message(MessageRole.Tool, result, ToolCallId: tc.Id));
            }
        }

        await onToolStatus("warning: max tool loop rounds reached");
    }

    private List<Message> BuildMessageList(string systemPrompt, string? instructions)
    {
        var list = new List<Message>(_history.Count + 1);

        var sys = systemPrompt;
        if (!string.IsNullOrWhiteSpace(instructions))
        {
            sys += "\n\nProject instructions:\n" + instructions;
        }

        list.Add(new Message(MessageRole.System, sys));
        list.AddRange(_history);
        return list;
    }

    private static string SummarizeArgs(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return "";
        return json.Length <= 120 ? json : json[..117] + "...";
    }

    private sealed class ToolCallAccumulator
    {
        public int Index { get; }
        private string _id = string.Empty;
        private string _name = string.Empty;
        private readonly StringBuilder _args = new();

        public ToolCallAccumulator(int index) => Index = index;

        public void Apply(ToolCallDelta delta)
        {
            if (!string.IsNullOrWhiteSpace(delta.Id)) _id = delta.Id;
            if (!string.IsNullOrWhiteSpace(delta.Name)) _name = delta.Name;
            if (!string.IsNullOrEmpty(delta.ArgumentsJsonDelta)) _args.Append(delta.ArgumentsJsonDelta);
        }

        public ToolCall ToToolCall()
        {
            var id = string.IsNullOrWhiteSpace(_id) ? $"call_{Index}" : _id;
            return new ToolCall(id, _name, _args.ToString());
        }
    }
}
