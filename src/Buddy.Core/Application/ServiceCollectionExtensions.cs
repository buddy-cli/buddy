using Buddy.Core.Agents;
using Buddy.Core.Configuration;
using Buddy.Core.Tooling;
using Buddy.Core.Tools;
using Buddy.Core.Worktree;
using Buddy.LLM;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Buddy.Core.Application;

public static class ServiceCollectionExtensions {
    public static IServiceCollection AddBuddyCore(this IServiceCollection services, BuddyOptions options) {
        services.AddSingleton(options);
        services.AddSingleton<IOptions<BuddyOptions>>(_ => Options.Create(options));

        services.AddSingleton<ILlmMClient>(_ => new OpenAiLlmMClient(options.ApiKey, options.Model, options.BaseUrl));
        services.AddSingleton<ILLMClientFactory, OpenAiLlmClientFactory>();

        services.AddSingleton<ISystemPromptBuilder, SystemPromptBuilder>();
        services.AddSingleton<IWorktreeSnapshotProvider, WorktreeSnapshotProvider>();

        services.AddSingleton<ITool, ReadFileTool>();
        services.AddSingleton<ITool, WriteFileTool>();
        services.AddSingleton<ITool, EditFileTool>();
        services.AddSingleton<ITool, ListDirectoryTool>();
        services.AddSingleton<ITool, GlobTool>();
        services.AddSingleton<ITool, GrepTool>();
        services.AddSingleton<ITool, RunTerminalTool>();
        services.AddSingleton<ToolRegistry>();

        services.AddSingleton<BuddyAgent>();

        return services;
    }
}
