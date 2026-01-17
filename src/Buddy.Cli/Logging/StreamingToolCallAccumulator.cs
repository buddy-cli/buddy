using System.Text;
using Buddy.LLM;

namespace Buddy.Cli.Logging;

internal sealed class StreamingToolCallAccumulator {
    private readonly Dictionary<int, ToolCallAccumulator> _accumulators = new();

    public void Apply(ToolCallDelta delta) {
        if (!_accumulators.TryGetValue(delta.Index, out var acc)) {
            acc = new ToolCallAccumulator(delta.Index);
            _accumulators[delta.Index] = acc;
        }

        acc.Apply(delta);
    }

    public IReadOnlyList<ToolCall> ToToolCalls() {
        return _accumulators.Values
            .OrderBy(acc => acc.Index)
            .Select(acc => acc.ToToolCall())
            .ToArray();
    }

    private sealed class ToolCallAccumulator {
        public int Index { get; }
        private string _id = string.Empty;
        private string _name = string.Empty;
        private readonly StringBuilder _args = new();

        public ToolCallAccumulator(int index) => Index = index;

        public void Apply(ToolCallDelta delta) {
            if (!string.IsNullOrWhiteSpace(delta.Id)) _id = delta.Id;
            if (!string.IsNullOrWhiteSpace(delta.Name)) _name = delta.Name;
            if (!string.IsNullOrEmpty(delta.ArgumentsJsonDelta)) _args.Append(delta.ArgumentsJsonDelta);
        }

        public ToolCall ToToolCall() {
            var id = string.IsNullOrWhiteSpace(_id) ? $"call_{Index}" : _id;
            return new ToolCall(id, _name, _args.ToString());
        }
    }
}
