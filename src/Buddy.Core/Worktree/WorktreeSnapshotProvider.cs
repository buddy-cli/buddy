namespace Buddy.Core.Worktree;

public sealed class WorktreeSnapshotProvider : IWorktreeSnapshotProvider {
    public string Build(string root) => WorktreeSnapshot.Build(root);
}
