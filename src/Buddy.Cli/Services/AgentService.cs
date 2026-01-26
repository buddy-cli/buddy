using Buddy.Core.Agents;
using Buddy.Core.Application;
using Buddy.Core.Configuration;
using Buddy.Core.Instructions;

namespace Buddy.Cli.Services;

public sealed class AgentService : IAgentService {
    private readonly BuddyAgent _agent;
    private readonly ILlmClientProvider _clientProvider;
    private readonly string _systemPrompt;
    private readonly string? _projectInstructions;

    public AgentService(
        BuddyAgent agent,
        ILlmClientProvider clientProvider,
        ISystemPromptBuilder systemPromptBuilder,
        BuddyOptions options) {
        _agent = agent;
        _clientProvider = clientProvider;
        _systemPrompt = systemPromptBuilder.Build(options, DateTimeOffset.Now, Environment.OSVersion.ToString());

        // Load project instructions synchronously for now (could be improved)
        _projectInstructions = ProjectInstructionsLoader.Load(options.WorkingDirectory).GetAwaiter().GetResult();
    }

    public async Task RunTurnAsync(
        string userInput,
        Func<string, Task> onTextDelta,
        Func<string, Task> onToolStatus,
        CancellationToken cancellationToken) {
        await _agent.RunTurnAsync(
            _clientProvider.Current,
            _systemPrompt,
            _projectInstructions,
            userInput,
            onTextDelta,
            onToolStatus,
            cancellationToken);
    }

    public void ClearHistory() {
        _agent.ClearHistory();
    }

    public void ChangeModel(LlmProviderConfig provider, LlmModelConfig model) {
        _clientProvider.SetModel(provider, model);
    }
}
