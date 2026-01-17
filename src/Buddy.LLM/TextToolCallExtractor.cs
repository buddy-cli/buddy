using System.Text;
using System.Text.Json;

namespace Buddy.LLM;

/// <summary>
/// Extracts tool calls from streaming text content.
/// Used as a fallback when models output tool calls as JSON in content instead of structured format.
/// </summary>
public sealed class TextToolCallExtractor {
    private readonly StringBuilder _buffer = new();
    private readonly HashSet<string> _knownToolNames;
    private readonly List<ToolCall> _extractedToolCalls = new();
    private int _braceDepth;
    private int _jsonStartIndex = -1;
    private bool _inString;
    private char _prevChar;
    private int _toolCallCounter;

    public TextToolCallExtractor(IEnumerable<string> knownToolNames) {
        _knownToolNames = new HashSet<string>(knownToolNames, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Result of processing a text chunk.
    /// </summary>
    public sealed record ExtractionResult(
        string? TextToDisplay,
        ToolCall? ExtractedToolCall,
        bool IsBuffering);

    /// <summary>
    /// Gets all tool calls extracted so far.
    /// </summary>
    public ToolCall[] GetExtractedToolCalls() => _extractedToolCalls.ToArray();

    /// <summary>
    /// Processes a text delta and attempts to extract tool calls.
    /// </summary>
    /// <returns>
    /// - If a complete tool call is found: returns the tool call, no text to display
    /// - If buffering potential JSON: returns null text, IsBuffering = true
    /// - If regular text: returns the text to display
    /// </returns>
    public ExtractionResult Process(string textDelta) {
        foreach (var ch in textDelta) {
            _buffer.Append(ch);

            // Track string state (to ignore braces inside strings)
            if (ch == '"' && _prevChar != '\\') {
                _inString = !_inString;
            }

            if (!_inString) {
                if (ch == '{') {
                    if (_braceDepth == 0) {
                        _jsonStartIndex = _buffer.Length - 1;
                    }

                    _braceDepth++;
                }
                else if (ch == '}') {
                    _braceDepth--;
                    if (_braceDepth == 0 && _jsonStartIndex >= 0) {
                        // We have a complete JSON object
                        var jsonCandidate = _buffer.ToString(_jsonStartIndex, _buffer.Length - _jsonStartIndex);
                        var toolCall = TryParseToolCall(jsonCandidate);

                        if (toolCall is not null) {
                            // Found a valid tool call - accumulate it
                            _extractedToolCalls.Add(toolCall);

                            var textBefore = _jsonStartIndex > 0
                                ? _buffer.ToString(0, _jsonStartIndex).Trim()
                                : null;
                            _buffer.Clear();
                            _jsonStartIndex = -1;

                            return new ExtractionResult(
                                string.IsNullOrWhiteSpace(textBefore) ? null : textBefore,
                                toolCall,
                                false);
                        }

                        // Not a valid tool call, reset JSON tracking but keep buffer
                        _jsonStartIndex = -1;
                    }
                }
            }

            _prevChar = ch;
        }

        // Still buffering if we're inside a potential JSON object
        if (_braceDepth > 0 && _jsonStartIndex >= 0) {
            return new ExtractionResult(null, null, true);
        }

        // No JSON detected, flush the buffer as regular text
        if (_buffer.Length > 0 && _braceDepth == 0) {
            var text = _buffer.ToString();
            _buffer.Clear();
            _jsonStartIndex = -1;
            return new ExtractionResult(text, null, false);
        }

        return new ExtractionResult(null, null, _braceDepth > 0);
    }

    /// <summary>
    /// Flushes any remaining buffered content.
    /// Call this when the stream ends to get any remaining text.
    /// </summary>
    public string? Flush() {
        if (_buffer.Length == 0) return null;
        var text = _buffer.ToString();
        _buffer.Clear();
        _braceDepth = 0;
        _jsonStartIndex = -1;
        _inString = false;
        return text;
    }

    /// <summary>
    /// Resets the extractor state for a new response.
    /// </summary>
    public void Reset() {
        _buffer.Clear();
        _extractedToolCalls.Clear();
        _braceDepth = 0;
        _jsonStartIndex = -1;
        _inString = false;
        _prevChar = '\0';
        _toolCallCounter = 0;
    }

    private ToolCall? TryParseToolCall(string json) {
        try {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Try common patterns:
            // Pattern 1: {"name": "tool_name", "arguments": {...}}
            // Pattern 2: {"name": "tool_name", "parameters": {...}}
            // Pattern 3: {"tool": "tool_name", "args": {...}}

            string? name = null;
            string? argsJson = null;

            // Try to extract name
            if (root.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String) {
                name = nameEl.GetString();
            }
            else if (root.TryGetProperty("tool", out var toolEl) && toolEl.ValueKind == JsonValueKind.String) {
                name = toolEl.GetString();
            }
            else if (root.TryGetProperty("function", out var funcEl) && funcEl.ValueKind == JsonValueKind.String) {
                name = funcEl.GetString();
            }

            if (string.IsNullOrWhiteSpace(name)) return null;

            // Check if this is a known tool
            if (!_knownToolNames.Contains(name)) return null;

            // Try to extract arguments
            if (root.TryGetProperty("arguments", out var argsEl)) {
                argsJson = argsEl.ValueKind == JsonValueKind.String
                    ? argsEl.GetString() // Already stringified
                    : argsEl.GetRawText(); // Object, serialize it
            }
            else if (root.TryGetProperty("parameters", out var paramsEl)) {
                argsJson = paramsEl.ValueKind == JsonValueKind.String
                    ? paramsEl.GetString()
                    : paramsEl.GetRawText();
            }
            else if (root.TryGetProperty("args", out var argsEl2)) {
                argsJson = argsEl2.ValueKind == JsonValueKind.String
                    ? argsEl2.GetString()
                    : argsEl2.GetRawText();
            }

            argsJson ??= "{}";

            // Generate a unique ID for this tool call
            var id = $"text_call_{_toolCallCounter++}";

            return new ToolCall(id, name, argsJson);
        }
        catch {
            return null;
        }
    }
}
