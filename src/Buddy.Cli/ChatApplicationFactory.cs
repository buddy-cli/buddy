using Buddy.Cli.Logging;
using Buddy.Cli.Ui;
using Buddy.Core.Agents;
using Buddy.Core.Application;
using Buddy.Core.Configuration;
using Buddy.LLM;
using Microsoft.Extensions.DependencyInjection;

namespace Buddy.Cli;

internal sealed class ChatApplicationFactory {
    private readonly IServiceProvider _services;

    public ChatApplicationFactory(IServiceProvider services) {
        _services = services;
    }

    public ChatApplication Create(
        string version,
        string systemPrompt,
        string? projectInstructions,
        CancellationToken cancellationToken) {
        return ActivatorUtilities.CreateInstance<ChatApplication>(
            _services,
            version,
            systemPrompt,
            projectInstructions ?? string.Empty,
            cancellationToken);
    }
}
