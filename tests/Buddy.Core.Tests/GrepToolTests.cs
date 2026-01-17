using System.Text.Json;
using Buddy.Core.Configuration;
using Buddy.Core.Tools;

namespace Buddy.Core.Tests;

public sealed class GrepToolTests {
    [Fact]
    public async Task Honors_glob_include_filters() {
        var root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(root);

        try {
            var srcDir = Path.Combine(root, "src");
            var otherDir = Path.Combine(root, "other");
            Directory.CreateDirectory(srcDir);
            Directory.CreateDirectory(otherDir);

            var matchingFile = Path.Combine(srcDir, "Sample.cs");
            var nonMatchingFile = Path.Combine(otherDir, "Sample.cs");
            await File.WriteAllTextAsync(matchingFile, "class RefreshFooter {}");
            await File.WriteAllTextAsync(nonMatchingFile, "class RefreshFooter {}");

            var options = new BuddyOptions { WorkingDirectory = root };
            var tool = new GrepTool(options);

            using var doc = JsonDocument.Parse("{\"pattern\":\"RefreshFooter\",\"path\":\".\",\"include\":\"src/**/*.cs\"}");
            var result = await tool.ExecuteAsync(doc.RootElement);

            var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            Assert.Single(lines);
            Assert.Contains("src/Sample.cs", lines[0]);
        }
        finally {
            if (Directory.Exists(root)) {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
