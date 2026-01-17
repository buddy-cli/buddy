using System.Text;
using Buddy.LLM;

namespace Buddy.Core.Tooling;

/// <summary>
/// Accumulates tool calls from a streaming response, handling both structured
/// (delta.tool_calls) and text-based (JSON in content) tool calls.
/// 
/// Strategy: Always try structured first. If no structured tool calls are found,
/// attempt to extract from text content.
/// </summary>
internal sealed class StreamingToolCallCollector {
    private readonly Dictionary<int, ToolCallAccumulator> _structuredAccumulators = new();
    private readonly TextToolCallExtractor _textExtractor;
    private readonly StringBuilder _textBuffer = new();
    private readonly StringBuilder _displayTextBuffer = new();

    public StreamingToolCallCollector(IEnumerable<string> knownToolNames) {
        _textExtractor = new TextToolCallExtractor(knownToolNames);
    }

    /// <summary>
    /// Text that should be displayed to the user (excludes extracted tool call JSON).
    /// </summary>
    public string DisplayText => _displayTextBuffer.ToString();

    /// <summary>
    /// Full assistant text content (for history, may include tool call JSON if not extracted).
    /// </summary>
    public string FullText => _textBuffer.ToString();

    /// <summary>
    /// Processes a structured tool call delta.
    /// </summary>
    public void ProcessToolCallDelta(ToolCallDelta delta) {
        if (!_structuredAccumulators.TryGetValue(delta.Index, out var acc)) {
            acc = new ToolCallAccumulator(delta.Index);
            _structuredAccumulators[delta.Index] = acc;
        }

        acc.Apply(delta);
    }

    /// <summary>
    /// Processes text content. Returns text that should be immediately displayed.
    /// Some text may be buffered if it looks like a potential tool call JSON.
    /// </summary>
    public string? ProcessTextDelta(string text) {
        _textBuffer.Append(text);

        // If we already have structured tool calls, just pass text through
        if (_structuredAccumulators.Count > 0) {
            _displayTextBuffer.Append(text);
            return text;
        }

        // Try to extract tool calls from text
        var result = _textExtractor.Process(text);

        if (result.TextToDisplay is { Length: > 0 }) {
            _displayTextBuffer.Append(result.TextToDisplay);
        }

        // Return text to display (may be null if buffering)
        return result.TextToDisplay;
    }

    /// <summary>
    /// Finalizes collection and returns all extracted tool calls.
    /// Prioritizes structured tool calls over text-extracted ones.
    /// </summary>
    public (ToolCall[] ToolCalls, string? RemainingText) Finalize() {
        // Flush any remaining buffered text
        var remaining = _textExtractor.Flush();

        // Prefer structured tool calls if we have any
        if (_structuredAccumulators.Count > 0) {
            var structuredCalls = _structuredAccumulators.Values
                .OrderBy(a => a.Index)
                .Select(a => a.ToToolCall())
                .ToArray();

            // Append any remaining text to display buffer
            if (remaining is { Length: > 0 }) {
                _displayTextBuffer.Append(remaining);
            }

            return (structuredCalls, remaining);
        }

        // Fall back to text-extracted tool calls
        var textCalls = _textExtractor.GetExtractedToolCalls();

        if (remaining is { Length: > 0 }) {
            _displayTextBuffer.Append(remaining);
        }

        return (textCalls, remaining);
    }

    /// <summary>
    /// Resets the collector for a new response.
    /// </summary>
    public void Reset() {
        _structuredAccumulators.Clear();
        _textExtractor.Reset();
        _textBuffer.Clear();
        _displayTextBuffer.Clear();
    }
}
