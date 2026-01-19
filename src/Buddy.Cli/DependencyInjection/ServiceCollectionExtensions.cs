using Buddy.Cli.AgentRuntime;
using Buddy.Cli.Logging;
using Buddy.Cli.Services;
using Buddy.Cli.Ui;
using Buddy.Cli.ViewModels;
using Buddy.Cli.Views;
using Buddy.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Buddy.Cli.DependencyInjection;

public static class ServiceCollectionExtensions {
    public static IServiceCollection AddBuddyCli(this IServiceCollection services) {
        services.AddSingleton<EnvironmentLoader>();
        services.AddTransient<MainView>();
        services.AddTransient<MainViewModel>();
        
        services.AddSingleton<ISessionLogger, MarkdownSessionLoggerFactory>();
        
        services.AddSingleton<ILlmClientProvider>(sp => {
            var options = sp.GetRequiredService<BuddyOptions>();
            var sessionLogger = sp.GetRequiredService<ISessionLogger>();
            var environmentLoader = sp.GetRequiredService<EnvironmentLoader>();
            return new LlmClientProvider(options, sessionLogger, environmentLoader.Environment.Version);
        });
        
        services.AddSingleton<IAgentService, AgentService>();

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

        services.AddTransient<ChatApplication>();

        return services;
    }
}
