namespace Buddy.LLM;

/// <summary>
/// A streamed tool-call delta.
///
/// Note: OpenAI-compatible streaming emits <c>function.arguments</c> as a JSON string fragment,
/// which may not be valid JSON until all chunks are concatenated.
/// </summary>
public sealed record ToolCallDelta(int Index, string Id, string Name, string ArgumentsJsonDelta);
