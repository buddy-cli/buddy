using System.Text.Json;
using Buddy.Core.Configuration;
using Buddy.Core.Tools;

namespace Buddy.Core.Tests;

public sealed class GlobToolTests {
    [Fact]
    public async Task Matches_files_with_glob_pattern() {
        var root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(root);

        try {
            var srcDir = Path.Combine(root, "src");
            var docsDir = Path.Combine(root, "docs");
            Directory.CreateDirectory(srcDir);
            Directory.CreateDirectory(docsDir);

            var matchPath = Path.Combine(srcDir, "Sample.cs");
            var otherPath = Path.Combine(docsDir, "Readme.md");
            await File.WriteAllTextAsync(matchPath, "class Sample {}");
            await File.WriteAllTextAsync(otherPath, "# docs");

            var options = new BuddyOptions { WorkingDirectory = root };
            var tool = new GlobTool(options);

            using var doc = JsonDocument.Parse("{\"pattern\":\"src/**/*.cs\"}");
            var result = await tool.ExecuteAsync(doc.RootElement);

            var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            Assert.Single(lines);
            Assert.Equal("src/Sample.cs", lines[0].Replace('\\', '/'));
        }
        finally {
            if (Directory.Exists(root)) {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Honors_search_root_path() {
        var root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(root);

        try {
            var srcDir = Path.Combine(root, "src");
            Directory.CreateDirectory(srcDir);

            var matchPath = Path.Combine(srcDir, "Match.cs");
            await File.WriteAllTextAsync(matchPath, "class Match {}");

            var options = new BuddyOptions { WorkingDirectory = root };
            var tool = new GlobTool(options);

            using var doc = JsonDocument.Parse("{\"pattern\":\"*.cs\",\"path\":\"src\"}");
            var result = await tool.ExecuteAsync(doc.RootElement);

            var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            Assert.Single(lines);
            Assert.Equal("src/Match.cs", lines[0].Replace('\\', '/'));
        }
        finally {
            if (Directory.Exists(root)) {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
