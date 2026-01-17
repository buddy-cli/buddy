using System.Text;

namespace Buddy.Core.Worktree;

public static class WorktreeSnapshot {
    private const int MaxDepth = 4;
    private const int MaxDirsPerNode = 5;
    private const int MaxFilesPerNode = 5;

    public static string Build(string root) {
        if (string.IsNullOrWhiteSpace(root)) {
            root = Directory.GetCurrentDirectory();
        }

        var rootInfo = new DirectoryInfo(root);
        if (!rootInfo.Exists) {
            return "(working directory missing)";
        }

        var sb = new StringBuilder();
        sb.AppendLine(".");
        AppendDirectory(sb, rootInfo, depth: 1);

        return sb.ToString().TrimEnd();
    }

    private static void AppendDirectory(StringBuilder sb, DirectoryInfo directory, int depth) {
        if (depth > MaxDepth) {
            return;
        }

        var children = directory.EnumerateFileSystemInfos()
            .Where(entry => !string.Equals(entry.Name, ".git", StringComparison.OrdinalIgnoreCase))
            .OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var directories = children.OfType<DirectoryInfo>().ToList();
        var files = children.Except(directories).ToList();

        var dirCount = 0;
        foreach (var dir in directories) {
            if (dirCount >= MaxDirsPerNode) {
                sb.AppendLine($"{Indent(depth)}...");
                break;
            }

            sb.AppendLine($"{Indent(depth)}{dir.Name}/");
            AppendDirectory(sb, dir, depth + 1);
            dirCount++;
        }

        var fileCount = 0;
        foreach (var file in files) {
            if (fileCount >= MaxFilesPerNode) {
                sb.AppendLine($"{Indent(depth)}...");
                break;
            }

            sb.AppendLine($"{Indent(depth)}{file.Name}");
            fileCount++;
        }
    }

    private static string Indent(int depth) => new string(' ', depth * 2);
}
