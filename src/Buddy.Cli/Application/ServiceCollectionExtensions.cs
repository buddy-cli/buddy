using Buddy.Cli.Logging;
using Buddy.Cli.Ui;
using Buddy.Core.Configuration;
using Buddy.LLM;
using Microsoft.Extensions.DependencyInjection;

namespace Buddy.Cli.Extensions;

public static class ServiceCollectionExtensions {
    public static IServiceCollection AddBuddyCli(this IServiceCollection services) {
        services.AddSingleton<ISessionLogger, MarkdownSessionLoggerFactory>();

        services.AddSingleton<ChatLayoutMetrics>(_ => new ChatLayoutMetrics(
            SessionHeaderHeight: 2,
            StageHeight: 1,
            InfoLayerHeight: 1,
            FooterHeight: 2));

        services.AddSingleton<IReadOnlyList<SlashCommandOption>>(_ => new List<SlashCommandOption> {
            new() { Command = "help", Description = "Show available commands" },
            new() { Command = "clear", Description = "Clear conversation history" },
            new() { Command = "model", Description = "Select model for next turns" },
            new() { Command = "provider", Description = "Edit provider configuration" },
            new() { Command = "exit", Description = "Exit the application" },
            new() { Command = "quit", Description = "Exit the application" }
        });

        services.AddSingleton<ChatLayoutManager>();

        services.AddTransient<ChatSessionState>(sp => new ChatSessionState(
            sp.GetRequiredService<ILlmMClient>(),
            inputHeight: 3,
            currentStage: "Idle"));

        services.AddTransient<ChatController>();
        services.AddTransient<ChatApplication>();

        return services;
    }
}
