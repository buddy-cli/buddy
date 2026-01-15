using System.Text.Json;

namespace Buddy.Core.Tools;

/// <summary>
/// Contract for tools exposed to the LLM. Stage 1/2 keeps these simple.
/// </summary>
public interface ITool {
    string Name { get; }
    string Description { get; }
    JsonElement ParameterSchema { get; }
    Task<string> ExecuteAsync(JsonElement args, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a user-friendly status line for display (e.g., "Reading file: README.md").
    /// </summary>
    string FormatStatusLine(JsonElement args);
}
