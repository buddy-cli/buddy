using System.Text.Json;
using Buddy.Core.Configuration;
using Buddy.Core.Tools;

namespace Buddy.Core.Tests;

public sealed class EditFileToolTests : IDisposable {
    private readonly string _root;
    private readonly BuddyOptions _options;
    private readonly EditFileTool _tool;

    public EditFileToolTests() {
        _root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_root);
        _options = new BuddyOptions { WorkingDirectory = _root };
        _tool = new EditFileTool(_options);
    }

    public void Dispose() {
        if (Directory.Exists(_root)) {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public async Task Replaces_exact_text() {
        var path = Path.Combine(_root, "test.txt");
        await File.WriteAllTextAsync(path, "Hello, World!");

        using var doc = JsonDocument.Parse("""
            {"path": "test.txt", "search": "World", "replace": "Universe"}
            """);

        var result = await _tool.ExecuteAsync(doc.RootElement);

        Assert.Equal("ok: replacements=1; changed=true", result);
        Assert.Equal("Hello, Universe!", await File.ReadAllTextAsync(path));
    }

    [Fact]
    public async Task Returns_error_when_search_not_found() {
        var path = Path.Combine(_root, "test.txt");
        await File.WriteAllTextAsync(path, "Hello, World!");

        using var doc = JsonDocument.Parse("""
            {"path": "test.txt", "search": "foo", "replace": "bar"}
            """);

        var result = await _tool.ExecuteAsync(doc.RootElement);

        Assert.Equal("error: search text not found (exact match required)", result);
    }

    [Fact]
    public async Task Replaces_all_occurrences() {
        var path = Path.Combine(_root, "test.txt");
        await File.WriteAllTextAsync(path, "foo bar foo baz foo");

        using var doc = JsonDocument.Parse("""
            {"path": "test.txt", "search": "foo", "replace": "qux"}
            """);

        var result = await _tool.ExecuteAsync(doc.RootElement);

        Assert.Equal("ok: replacements=3; changed=true", result);
        Assert.Equal("qux bar qux baz qux", await File.ReadAllTextAsync(path));
    }

    [Fact]
    public async Task Handles_multiline_search_and_replace() {
        var path = Path.Combine(_root, "test.txt");
        await File.WriteAllTextAsync(path, "line1\nline2\nline3");

        using var doc = JsonDocument.Parse("""
            {"path": "test.txt", "search": "line1\nline2", "replace": "replaced1\nreplaced2"}
            """);

        var result = await _tool.ExecuteAsync(doc.RootElement);

        Assert.Equal("ok: replacements=1; changed=true", result);
        Assert.Equal("replaced1\nreplaced2\nline3", await File.ReadAllTextAsync(path));
    }

    [Fact]
    public async Task Returns_error_when_file_not_found() {
        using var doc = JsonDocument.Parse("""
            {"path": "nonexistent.txt", "search": "foo", "replace": "bar"}
            """);

        var result = await _tool.ExecuteAsync(doc.RootElement);

        Assert.StartsWith("error: file not found:", result);
    }

    [Fact]
    public async Task Reports_no_change_when_search_equals_replace() {
        var path = Path.Combine(_root, "test.txt");
        await File.WriteAllTextAsync(path, "Hello, World!");

        using var doc = JsonDocument.Parse("""
            {"path": "test.txt", "search": "World", "replace": "World"}
            """);

        var result = await _tool.ExecuteAsync(doc.RootElement);

        Assert.Equal("ok: replacements=1; changed=false", result);
    }

    /// <summary>
    /// In raw strings, each backslash is literal. So raw string `\\` = 2 backslashes in JSON.
    /// JSON `\\` = single backslash after parse. JSON `\n` = newline.
    /// For LLM: to get backslash in file, send 2 backslashes in JSON text.
    /// </summary>
    [Fact]
    public async Task Double_backslash_in_raw_json_becomes_single_in_file() {
        var path = Path.Combine(_root, "test.cs");
        await File.WriteAllTextAsync(path, "var x = \"hello\";");

        // Raw string \\ = JSON \\ (2 backslashes) = single backslash after parse
        using var doc = JsonDocument.Parse("""
            {"path": "test.cs", "search": "hello", "replace": "path\\nwith\\tescapes"}
            """);

        var result = await _tool.ExecuteAsync(doc.RootElement);

        Assert.Equal("ok: replacements=1; changed=true", result);
        // File contains: path\nwith\tescapes (literal backslash chars, not control chars)
        var content = await File.ReadAllTextAsync(path);
        Assert.Equal("var x = \"path\\nwith\\tescapes\";", content);
    }

    /// <summary>
    /// For C# format strings like hh\\:mm (needs double backslash in file):
    /// Raw string needs 4 backslashes: \\\\ → JSON sees \\\\ → 2 backslashes after parse
    /// </summary>
    [Fact]
    public async Task Quad_backslash_in_raw_string_becomes_double_backslash_in_file() {
        var path = Path.Combine(_root, "test.cs");
        await File.WriteAllTextAsync(path, "format:hh:mm");

        // Raw string \\\\ = JSON \\\\ = double backslash after parse = \\ in file
        using var doc = JsonDocument.Parse("""
            {"path": "test.cs", "search": "format:hh:mm", "replace": "format:hh\\\\:mm\\\\:ss"}
            """);

        var result = await _tool.ExecuteAsync(doc.RootElement);

        Assert.Equal("ok: replacements=1; changed=true", result);
        var content = await File.ReadAllTextAsync(path);
        // File should contain: format:hh\\:mm\\:ss (with DOUBLE backslashes for C# format strings)
        Assert.Equal("format:hh\\\\:mm\\\\:ss", content);
    }

    /// <summary>
    /// Simulates the exact scenario from the bug report: C# interpolated format string
    /// </summary>
    [Fact]
    public async Task Handles_csharp_format_string_escaping() {
        var path = Path.Combine(_root, "Program.cs");
        // File starts with format string that has wrong escaping (single backslash)
        await File.WriteAllTextAsync(path, """Console.WriteLine($"Time: {sw.Elapsed:hh\:mm}");""");

        // To fix it, we search for single backslash and replace with double
        // Search JSON: hh\\:mm → after parse → hh\:mm (matches file's single backslash)
        // Replace JSON: hh\\\\:mm → after parse → hh\\:mm (double backslash in file)
        using var doc = JsonDocument.Parse("""
            {"path": "Program.cs", "search": "hh\\:mm", "replace": "hh\\\\:mm\\\\:ss"}
            """);

        var result = await _tool.ExecuteAsync(doc.RootElement);

        Assert.Equal("ok: replacements=1; changed=true", result);
        var content = await File.ReadAllTextAsync(path);
        // After fix, file should have double backslashes (correct C#)
        Assert.Equal("""Console.WriteLine($"Time: {sw.Elapsed:hh\\:mm\\:ss}");""", content);
    }

    [Fact]
    public async Task Handles_quotes_in_json() {
        var path = Path.Combine(_root, "test.txt");
        await File.WriteAllTextAsync(path, "He said \"hello\"");

        using var doc = JsonDocument.Parse("""
            {"path": "test.txt", "search": "said \"hello\"", "replace": "said \"goodbye\""}
            """);

        var result = await _tool.ExecuteAsync(doc.RootElement);

        Assert.Equal("ok: replacements=1; changed=true", result);
        Assert.Equal("He said \"goodbye\"", await File.ReadAllTextAsync(path));
    }
}
