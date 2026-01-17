using System.Text;
using System.Text.Json;
using Buddy.Core.Configuration;
using Buddy.Core.Tooling;

namespace Buddy.Core.Tools;

public sealed class GrepTool : ITool {
    private static readonly JsonElement Schema = JsonDocument.Parse(
        """
        {
          "type": "object",
          "properties": {
            "pattern": { "type": "string", "description": "Regex pattern to search for." },
            "path": { "type": "string", "description": "Search root directory. Defaults to working directory." },
            "include": { "type": "string", "description": "Optional glob filter for files (e.g. *.cs)." }
          },
          "required": ["pattern"],
          "additionalProperties": false
        }
        """).RootElement.Clone();

    private readonly string _workingDirectory;

    public GrepTool(BuddyOptions options) {
        _workingDirectory = options.WorkingDirectory;
    }

    public string Name => "grep";
    public string Description => "Search inside file contents using regex. Use when looking for code patterns, function definitions, or specific text. Returns matching lines with file:line format. Use 'include' to filter by file type (e.g., '*.cs' or '**/*.ts').";
    public JsonElement ParameterSchema => Schema;

    public async Task<string> ExecuteAsync(JsonElement args, CancellationToken cancellationToken = default) {
        if (!args.TryGetProperty("pattern", out var patternEl) || patternEl.ValueKind != JsonValueKind.String) {
            return "error: missing required string argument 'pattern'";
        }

        var pattern = patternEl.GetString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(pattern)) {
            return "error: pattern is empty";
        }

        var rawPath = _workingDirectory;
        if (args.ValueKind == JsonValueKind.Object && args.TryGetProperty("path", out var pathEl) && pathEl.ValueKind == JsonValueKind.String) {
            var providedPath = pathEl.GetString();
            if (!string.IsNullOrWhiteSpace(providedPath)) {
                rawPath = providedPath;
            }
        }

        var include = args.TryGetProperty("include", out var includeEl) && includeEl.ValueKind == JsonValueKind.String
            ? includeEl.GetString()
            : null;

        var root = PathResolver.Resolve(_workingDirectory, rawPath);
        if (!Directory.Exists(root)) {
            return $"error: directory not found: {root}";
        }

        var results = new List<string>();
        var files = EnumerateFiles(root, include);
        foreach (var file in files) {
            if (cancellationToken.IsCancellationRequested) {
                break;
            }

            if (!File.Exists(file)) {
                continue;
            }

            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

            var lineNumber = 0;
            while (true) {
                if (cancellationToken.IsCancellationRequested) {
                    break;
                }

                var line = await reader.ReadLineAsync(cancellationToken);
                if (line is null) {
                    break;
                }

                lineNumber++;
                if (!System.Text.RegularExpressions.Regex.IsMatch(line, pattern)) {
                    continue;
                }

                var relative = Path.GetRelativePath(_workingDirectory, file);
                results.Add($"{relative}:{lineNumber}: {line}");
            }
        }

        return results.Count == 0 ? string.Empty : string.Join("\n", results);
    }

    public string FormatStatusLine(JsonElement args) {
        var pattern = args.TryGetProperty("pattern", out var p) && p.ValueKind == JsonValueKind.String
            ? p.GetString() ?? "(unknown)"
            : "(unknown)";
        var path = args.TryGetProperty("path", out var pathEl) && pathEl.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(pathEl.GetString())
            ? pathEl.GetString()!
            : ".";
        return $"Searching: {pattern} in {path}";
    }

    private static IEnumerable<string> EnumerateFiles(string root, string? includePattern) {
        if (string.IsNullOrWhiteSpace(includePattern)) {
            return Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories);
        }

        if (includePattern.Contains(Path.DirectorySeparatorChar) || includePattern.Contains(Path.AltDirectorySeparatorChar) || includePattern.Contains("**")) {
            return EnumerateFilesWithGlob(root, includePattern);
        }

        return Directory.EnumerateFiles(root, includePattern, SearchOption.AllDirectories);
    }

    private static IEnumerable<string> EnumerateFilesWithGlob(string root, string includePattern) {
        var matcher = new Microsoft.Extensions.FileSystemGlobbing.Matcher(StringComparison.OrdinalIgnoreCase);
        matcher.AddInclude(includePattern);

        var result = matcher.Execute(new Microsoft.Extensions.FileSystemGlobbing.Abstractions.DirectoryInfoWrapper(new DirectoryInfo(root)));
        if (!result.HasMatches) {
            return Array.Empty<string>();
        }

        return result.Files.Select(match => Path.Combine(root, match.Path));
    }
}
