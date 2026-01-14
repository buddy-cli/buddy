using System.Text.Json;

namespace Buddy.Core.Tools;

public sealed class WriteFileTool : ITool {
    private static readonly JsonElement Schema = JsonDocument.Parse(
        """
        {
          "type": "object",
          "properties": {
            "path": { "type": "string" },
            "content": { "type": "string" }
          },
          "required": ["path", "content"],
          "additionalProperties": false
        }
        """).RootElement.Clone();

    private readonly string _workingDirectory;

    public WriteFileTool(BuddyOptions options) {
        _workingDirectory = options.WorkingDirectory;
    }

    public string Name => "write_file";
    public string Description => "Write a file (creating parent directories if needed).";
    public JsonElement ParameterSchema => Schema;

    public async Task<string> ExecuteAsync(JsonElement args, CancellationToken cancellationToken = default) {
        if (!args.TryGetProperty("path", out var pathEl) || pathEl.ValueKind != JsonValueKind.String) {
            return "error: missing required string argument 'path'";
        }

        if (!args.TryGetProperty("content", out var contentEl) || contentEl.ValueKind != JsonValueKind.String) {
            return "error: missing required string argument 'content'";
        }

        var path = PathResolver.Resolve(_workingDirectory, pathEl.GetString()!);
        var content = contentEl.GetString() ?? string.Empty;

        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir)) {
            Directory.CreateDirectory(dir);
        }

        await File.WriteAllTextAsync(path, content, cancellationToken);
        return $"ok: wrote {content.Length} chars to {path}";
    }
}
