using Buddy.Core.Agents;
using Buddy.Core.Application;
using Buddy.Core.Configuration;
using Buddy.Core.Instructions;
using Buddy.LLM;

namespace Buddy.Cli.Services;

public sealed class AgentService : IAgentService {
    private readonly BuddyAgent _agent;
    private readonly ILlmMClient _llmClient;
    private readonly string _systemPrompt;
    private readonly string? _projectInstructions;

    public AgentService(
        BuddyAgent agent,
        ILlmMClient llmClient,
        ISystemPromptBuilder systemPromptBuilder,
        BuddyOptions options) {
        _agent = agent;
        _llmClient = llmClient;
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
            _llmClient,
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
}
