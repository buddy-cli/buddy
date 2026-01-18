namespace Buddy.Core.Worktree;

public sealed class WorktreeSnapshotProvider : IWorktreeSnapshotProvider {
    public string Build(string root) => WorktreeSnapshotBuilder.Build(root);
}
