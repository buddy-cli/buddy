using Buddy.Core.Worktree;

namespace Buddy.Cli;

internal static class WorktreeSnapshot {
    public static string Build(string root) => WorktreeSnapshotBuilder.Build(root);
}
