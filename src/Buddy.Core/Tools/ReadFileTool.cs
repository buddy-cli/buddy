using System.Text.Json;
using Buddy.Core.Configuration;
using Buddy.Core.Tooling;

namespace Buddy.Core.Tools;

public sealed class ReadFileTool(BuddyOptions options) : ITool {
    private static readonly JsonElement Schema = JsonDocument.Parse(
        """
        {
          "type": "object",
          "properties": {
            "path": { "type": "string", "description": "Path to the file (relative to working directory unless absolute)." }
          },
          "required": ["path"],
          "additionalProperties": false
        }
        """).RootElement.Clone();

    private readonly string _workingDirectory = options.WorkingDirectory;

    public string Name => "read_file";
    public string Description => "Read the full contents of a file. Use after locating the file with 'glob' or 'list_directory'. Returns the complete file text.";
    public JsonElement ParameterSchema => Schema;

    public async Task<string> ExecuteAsync(JsonElement args, CancellationToken cancellationToken = default) {
        if (!args.TryGetProperty("path", out var pathEl) || pathEl.ValueKind != JsonValueKind.String) {
            return "error: missing required string argument 'path'";
        }

        var path = PathResolver.Resolve(_workingDirectory, pathEl.GetString()!);

        if (!File.Exists(path)) {
            return $"error: file not found: {path}";
        }

        return await File.ReadAllTextAsync(path, cancellationToken);
    }

    public string FormatStatusLine(JsonElement args) {
        var path = args.TryGetProperty("path", out var p) && p.ValueKind == JsonValueKind.String
            ? p.GetString() ?? "(unknown)"
            : "(unknown)";
        return $"Reading file: {path}";
    }
}
