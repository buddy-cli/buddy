using System.Text.Json;
using Buddy.Core.Configuration;
using Buddy.Core.Tooling;

namespace Buddy.Core.Tools;

public sealed class ReadFileTool : ITool {
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

    private readonly string _workingDirectory;

    public ReadFileTool(BuddyOptions options) {
        _workingDirectory = options.WorkingDirectory;
    }

    public string Name => "read_file";
    public string Description => "Read a file and return its contents.";
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
