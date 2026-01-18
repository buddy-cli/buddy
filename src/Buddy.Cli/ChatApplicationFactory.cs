using Buddy.Cli.AgentRuntime;
using Buddy.Core.Application;
using Buddy.Core.Configuration;
using Buddy.Core.Instructions;
using Buddy.Core.Worktree;
using Microsoft.Extensions.DependencyInjection;

namespace Buddy.Cli;

internal sealed class ChatApplicationFactory(IServiceProvider services) {
    public async Task<ChatApplication> Create(
        AgentEnvironment environment,
        BuddyOptions options,
        CancellationToken cancellationToken) {

        var worktreeSnapshotProvider = services.GetRequiredService<IWorktreeSnapshotProvider>();
        var worktreeSnapshot = worktreeSnapshotProvider.Build(environment.WorkingDirectory);

        var projectInstructions = await ProjectInstructionsLoader.Load(environment.WorkingDirectory);

        var systemPromptBuilder = services.GetRequiredService<ISystemPromptBuilder>();
        var systemPrompt = systemPromptBuilder.Build(options, environment.CurrentDate, environment.OsEnvironment)
                           + $"""
                              <ProjectWorktree>
                              Project worktree:
                              {worktreeSnapshot}
                              </ProjectWorktree>
                              """;


        return ActivatorUtilities.CreateInstance<ChatApplication>(
            services,
            environment.Version,
            systemPrompt,
            projectInstructions ?? string.Empty,
            cancellationToken);
    }
}
