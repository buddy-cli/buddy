using System.Text.Json;
using Buddy.Core.Configuration;
using Buddy.Core.Tooling;

namespace Buddy.Core.Tools;

public sealed class EditFileTool : ITool {
    private static readonly JsonElement Schema = JsonDocument.Parse(
        """
        {
          "type": "object",
          "properties": {
            "path": { "type": "string" },
            "search": { "type": "string", "description": "Exact text to find." },
            "replace": { "type": "string", "description": "Replacement text." }
          },
          "required": ["path", "search", "replace"],
          "additionalProperties": false
        }
        """).RootElement.Clone();

    private readonly string _workingDirectory;

    public EditFileTool(BuddyOptions options) {
        _workingDirectory = options.WorkingDirectory;
    }

    public string Name => "edit_file";
    public string Description => "Edit a file by replacing exact text. The 'search' must match exactly (including whitespace). Include surrounding context lines in 'search' to ensure uniqueness. All occurrences are replaced. Read the file first to get the exact text. ESCAPE NOTE: JSON escaping applies - to get a literal backslash in the file, send '\\\\' (two backslashes) in the JSON value. For C# format strings like 'hh\\\\:mm' (double backslash in file), send 4 backslashes in JSON.";
    public JsonElement ParameterSchema => Schema;

    public async Task<string> ExecuteAsync(JsonElement args, CancellationToken cancellationToken = default) {
        if (!args.TryGetProperty("path", out var pathEl) || pathEl.ValueKind != JsonValueKind.String) {
            return "error: missing required string argument 'path'";
        }
        if (!args.TryGetProperty("search", out var searchEl) || searchEl.ValueKind != JsonValueKind.String) {
            return "error: missing required string argument 'search'";
        }
        if (!args.TryGetProperty("replace", out var replaceEl) || replaceEl.ValueKind != JsonValueKind.String) {
            return "error: missing required string argument 'replace'";
        }

        var path = PathResolver.Resolve(_workingDirectory, pathEl.GetString()!);
        var search = searchEl.GetString() ?? string.Empty;
        var replace = replaceEl.GetString() ?? string.Empty;

        if (!File.Exists(path)) {
            return $"error: file not found: {path}";
        }

        var text = await File.ReadAllTextAsync(path, cancellationToken);

        var count = CountOccurrences(text, search);
        if (count == 0) {
            return "error: search text not found (exact match required)";
        }

        var updated = text.Replace(search, replace, StringComparison.Ordinal);
        var changed = !string.Equals(text, updated, StringComparison.Ordinal);

        if (changed) {
            await File.WriteAllTextAsync(path, updated, cancellationToken);
        }

        return $"ok: replacements={count}; changed={changed.ToString().ToLowerInvariant()}";
    }

    private static int CountOccurrences(string haystack, string needle) {
        if (string.IsNullOrEmpty(needle)) {
            return 0;
        }

        var count = 0;
        var idx = 0;
        while (true) {
            idx = haystack.IndexOf(needle, idx, StringComparison.Ordinal);
            if (idx < 0) break;
            count++;
            idx += needle.Length;
        }

        return count;
    }

    public string FormatStatusLine(JsonElement args) {
        var path = args.TryGetProperty("path", out var p) && p.ValueKind == JsonValueKind.String
            ? p.GetString() ?? "(unknown)"
            : "(unknown)";
        return $"Editing file: {path}";
    }
}
