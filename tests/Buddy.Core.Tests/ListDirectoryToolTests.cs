using System.Text.Json;
using Buddy.Core.Configuration;
using Buddy.Core.Tools;

namespace Buddy.Core.Tests;

public sealed class ListDirectoryToolTests : IDisposable {
    private readonly string _root;
    private readonly BuddyOptions _options;
    private readonly ListDirectoryTool _tool;

    public ListDirectoryToolTests() {
        _root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_root);
        _options = new BuddyOptions { WorkingDirectory = _root };
        _tool = new ListDirectoryTool(_options);
    }

    public void Dispose() {
        if (Directory.Exists(_root)) {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public async Task Lists_entries_in_working_directory_by_default() {
        var subDir = Path.Combine(_root, "Sub");
        Directory.CreateDirectory(subDir);

        var fileA = Path.Combine(_root, "b.txt");
        var fileB = Path.Combine(_root, "A.txt");
        await File.WriteAllTextAsync(fileA, "b");
        await File.WriteAllTextAsync(fileB, "a");

        using var doc = JsonDocument.Parse("{}");
        var result = await _tool.ExecuteAsync(doc.RootElement);

        var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.Equal(new[] { "A.txt", "b.txt", "Sub/" }, lines);
    }

    [Fact]
    public async Task Honors_explicit_path_relative_to_working_directory() {
        var dir = Path.Combine(_root, "child");
        Directory.CreateDirectory(dir);

        await File.WriteAllTextAsync(Path.Combine(dir, "c.txt"), "c");
        Directory.CreateDirectory(Path.Combine(dir, "Dir"));

        using var doc = JsonDocument.Parse("{\"path\":\"child\"}");
        var result = await _tool.ExecuteAsync(doc.RootElement);

        var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.Equal(new[] { "c.txt", "Dir/" }, lines);
    }

    [Fact]
    public async Task Returns_error_when_directory_not_found() {
        using var doc = JsonDocument.Parse("{\"path\":\"missing\"}");
        var result = await _tool.ExecuteAsync(doc.RootElement);

        Assert.StartsWith("error: directory not found:", result);
        Assert.Contains(Path.Combine(_root, "missing"), result);
    }

    [Fact]
    public void FormatStatusLine_defaults_to_dot() {
        using var doc = JsonDocument.Parse("{}");
        Assert.Equal("Listing directory: .", _tool.FormatStatusLine(doc.RootElement));
    }
}
