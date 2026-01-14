using Microsoft.Extensions.Hosting;

namespace Buddy.Core;

/// <summary>
/// Entry point for running the buddy agent. For now, this is a placeholder
/// that will host the interactive loop in later phases.
/// </summary>
public sealed class BuddyApplication {
    public Task<int> RunAsync(CancellationToken cancellationToken = default) {
        // Stage 0 scaffold: no orchestration yet.
        return Task.FromResult(0);
    }
}
