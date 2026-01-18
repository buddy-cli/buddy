using System.Text;
using Buddy.LLM;

namespace Buddy.Core.Tooling;

internal sealed class ToolCallAccumulator(int index) {
    public int Index { get; } = index;
    private string _id = string.Empty;
    private string _name = string.Empty;
    private readonly StringBuilder _args = new();

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
