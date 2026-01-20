using Buddy.Cli.AgentRuntime;
using Buddy.Cli.Logging;
using Buddy.Cli.Services;
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
        
        services.AddSingleton<IDialogViewModelFactory, DialogViewModelFactory>();
        services.AddSingleton<ISessionLogger, MarkdownSessionLoggerFactory>();
        
        services.AddSingleton<ILlmClientProvider>(sp => {
            var options = sp.GetRequiredService<BuddyOptions>();
            var sessionLogger = sp.GetRequiredService<ISessionLogger>();
            var environmentLoader = sp.GetRequiredService<EnvironmentLoader>();
            return new LlmClientProvider(options, sessionLogger, environmentLoader.Environment.Version);
        });
        
        services.AddSingleton<IAgentService, AgentService>();

        return services;
    }
}
