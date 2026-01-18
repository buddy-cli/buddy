using Buddy.Core.Worktree;

namespace Buddy.Core.Tests;

public sealed class WorktreeSnapshotBuilderTests {
    [Fact]
    public void Includes_shallow_tree_with_limits() {
        var root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(root);

        try {
            var dirA = Path.Combine(root, "aaa");
            var dirB = Path.Combine(root, "bbb");
            Directory.CreateDirectory(dirA);
            Directory.CreateDirectory(dirB);

            for (var i = 0; i < 7; i++) {
                Directory.CreateDirectory(Path.Combine(root, $"dir{i}"));
                File.WriteAllText(Path.Combine(root, $"file{i}.txt"), "x");
            }

            Directory.CreateDirectory(Path.Combine(dirA, "nested"));
            File.WriteAllText(Path.Combine(dirA, "nested.txt"), "y");

            var snapshot = WorktreeSnapshotBuilder.Build(root);
            var rawLines = snapshot.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var lines = rawLines.Select(line => line.Replace("  ", string.Empty, StringComparison.Ordinal)).ToList();

            Assert.Equal(".", lines[0]);
            Assert.Contains("aaa/", lines);
            Assert.Contains("bbb/", lines);
            Assert.Contains("nested/", lines);
            Assert.Contains("nested.txt", lines);
            Assert.Contains("...", lines);
        }
        finally {
            if (Directory.Exists(root)) {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
