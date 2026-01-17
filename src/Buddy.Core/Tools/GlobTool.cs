using System.Text.Json;
using Buddy.Core.Configuration;
using Buddy.Core.Tooling;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace Buddy.Core.Tools;

public sealed class GlobTool : ITool {
    private static readonly JsonElement Schema = JsonDocument.Parse(
        """
        {
          "type": "object",
          "properties": {
            "pattern": { "type": "string", "description": "Glob pattern to match." },
            "path": { "type": "string", "description": "Search root directory. Defaults to working directory." }
          },
          "required": ["pattern"],
          "additionalProperties": false
        }
        """).RootElement.Clone();

    private readonly string _workingDirectory;

    public GlobTool(BuddyOptions options) {
        _workingDirectory = options.WorkingDirectory;
    }

    public string Name => "glob";
    public string Description => "Find files by name pattern. Use this FIRST when looking for a specific file or project (e.g., '**/folder-name/**' or '**/*.txt'). Supports ** for recursive matching. Returns paths sorted by modification time (newest first).";
    public JsonElement ParameterSchema => Schema;

    public Task<string> ExecuteAsync(JsonElement args, CancellationToken cancellationToken = default) {
        if (!args.TryGetProperty("pattern", out var patternEl) || patternEl.ValueKind != JsonValueKind.String) {
            return Task.FromResult("error: missing required string argument 'pattern'");
        }

        var pattern = patternEl.GetString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(pattern)) {
            return Task.FromResult("error: pattern is empty");
        }

        var rawPath = _workingDirectory;
        if (args.ValueKind == JsonValueKind.Object && args.TryGetProperty("path", out var pathEl) && pathEl.ValueKind == JsonValueKind.String) {
            var providedPath = pathEl.GetString();
            if (!string.IsNullOrWhiteSpace(providedPath)) {
                rawPath = providedPath;
            }
        }

        var root = PathResolver.Resolve(_workingDirectory, rawPath);
        if (!Directory.Exists(root)) {
            return Task.FromResult($"error: directory not found: {root}");
        }

        var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
        matcher.AddInclude(pattern);

        var result = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(root)));
        if (!result.HasMatches) {
            return Task.FromResult(string.Empty);
        }

        var matches = result.Files
            .Select(match => Path.Combine(root, match.Path))
            .Select(path => new { Path = path, Modified = File.GetLastWriteTimeUtc(path) })
            .OrderByDescending(entry => entry.Modified)
            .ThenBy(entry => entry.Path, StringComparer.OrdinalIgnoreCase)
            .Select(entry => FormatPath(entry.Path));

        return Task.FromResult(string.Join("\n", matches));
    }

    public string FormatStatusLine(JsonElement args) {
        var pattern = args.TryGetProperty("pattern", out var p) && p.ValueKind == JsonValueKind.String
            ? p.GetString() ?? "(unknown)"
            : "(unknown)";
        var path = args.TryGetProperty("path", out var pathEl) && pathEl.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(pathEl.GetString())
            ? pathEl.GetString()!
            : ".";
        return $"Globbing: {pattern} in {path}";
    }

    private string FormatPath(string path) {
        return Path.GetRelativePath(_workingDirectory, path);
    }
}
