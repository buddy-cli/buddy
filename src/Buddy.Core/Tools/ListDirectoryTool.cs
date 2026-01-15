using System.Text;
using System.Text.Json;
using Buddy.Core.Configuration;
using Buddy.Core.Tooling;

namespace Buddy.Core.Tools;

public sealed class ListDirectoryTool : ITool {
    private static readonly JsonElement Schema = JsonDocument.Parse(
        """
        {
          "type": "object",
          "properties": {
            "path": { "type": "string", "description": "Directory path. Defaults to working directory." }
          },
          "required": [],
          "additionalProperties": false
        }
        """).RootElement.Clone();

    private readonly string _workingDirectory;

    public ListDirectoryTool(BuddyOptions options) {
        _workingDirectory = options.WorkingDirectory;
    }

    public string Name => "list_directory";
    public string Description => "List entries in a directory.";
    public JsonElement ParameterSchema => Schema;

    public Task<string> ExecuteAsync(JsonElement args, CancellationToken cancellationToken = default) {
        var rawPath = _workingDirectory;
        if (args.ValueKind == JsonValueKind.Object && args.TryGetProperty("path", out var pathEl) && pathEl.ValueKind == JsonValueKind.String) {
            var providedPath = pathEl.GetString();
            if (!string.IsNullOrWhiteSpace(providedPath)) {
                rawPath = providedPath;
            }
        }

        var path = PathResolver.Resolve(_workingDirectory, rawPath);

        if (!Directory.Exists(path)) {
            return Task.FromResult($"error: directory not found: {path}");
        }

        var sb = new StringBuilder();
        foreach (var entry in Directory.EnumerateFileSystemEntries(path).OrderBy(p => p, StringComparer.OrdinalIgnoreCase)) {
            var name = Path.GetFileName(entry);
            if (Directory.Exists(entry)) {
                name += "/";
            }
            sb.AppendLine(name);
        }

        return Task.FromResult(sb.ToString().TrimEnd());
    }

    public string FormatStatusLine(JsonElement args) {
        var path = args.TryGetProperty("path", out var p) && p.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(p.GetString())
            ? p.GetString()!
            : ".";
        return $"Listing directory: {path}";
    }
}
