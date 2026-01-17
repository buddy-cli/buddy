namespace Buddy.Core.Worktree;

public interface IWorktreeSnapshotProvider {
    string Build(string root);
}
