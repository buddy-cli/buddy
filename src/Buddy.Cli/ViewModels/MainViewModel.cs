using System.Reactive.Linq;
using System.Windows.Input;
using Buddy.Cli.AgentRuntime;
using Buddy.Cli.Services;
using Buddy.Core.Configuration;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace Buddy.Cli.ViewModels;

public partial class MainViewModel : ReactiveObject {
    private readonly IAgentService _agentService;
    private CancellationTokenSource? _cts;

    [Reactive]
    private string _version = string.Empty;

    [Reactive]
    private string _workingDirectory = string.Empty;

    [Reactive]
    private string _inputText = string.Empty;

    [Reactive]
    private bool _isAgentWorkMode;

    [Reactive]
    private string _modelInfo = string.Empty;

    public SlashCommandsViewModel SlashCommands { get; }
    
    public AgentLogViewModel AgentLog { get; }

    public ICommand SubmitCommand { get; }

    public MainViewModel(EnvironmentLoader environmentLoader, IAgentService agentService, BuddyOptions options) {
        _agentService = agentService;
        
        Version = environmentLoader.Environment.Version;
        WorkingDirectory = environmentLoader.Environment.WorkingDirectory;
        
        // Find the provider and model display name for the current system model
        var provider = options.Providers
            .FirstOrDefault(p => p.Models.Any(m => m.System == options.Model));
        var providerName = provider?.Name ?? "Unknown";
        var modelDisplayName = provider?.Models.FirstOrDefault(m => m.System == options.Model)?.Name ?? options.Model;
        ModelInfo = $"{modelDisplayName} ({providerName})";

        SlashCommands = new SlashCommandsViewModel();
        AgentLog = new AgentLogViewModel();

        // Submit command - only enabled when there's text and not in slash mode
        var canSubmit = this.WhenAnyValue(x => x.InputText)
            .Select(text => !string.IsNullOrWhiteSpace(text) && !text.StartsWith("/"));
        
        SubmitCommand = ReactiveCommand.CreateFromTask(SubmitAsync, canSubmit);

        // Wire up slash command activation based on input text
        this.WhenAnyValue(x => x.InputText)
            .Select(text => text.StartsWith("/"))
            .DistinctUntilChanged()
            .Subscribe(isSlashMode => SlashCommands.IsActive = isSlashMode);

        // Update filter text when in slash mode
        this.WhenAnyValue(x => x.InputText)
            .Where(text => text.StartsWith("/"))
            .Select(text => text.Length > 1 ? text[1..].Split(' ')[0] : string.Empty)
            .DistinctUntilChanged()
            .Subscribe(filter => SlashCommands.FilterText = filter);
    }

    private async Task SubmitAsync() {
        var userInput = InputText.Trim();
        if (string.IsNullOrWhiteSpace(userInput) || userInput.StartsWith("/")) {
            return;
        }

        InputText = string.Empty;
        IsAgentWorkMode = true;
        AgentLog.IsProcessing = true;

        // Add user message to log
        AgentLog.AddUserMessage(userInput);

        _cts = new CancellationTokenSource();
        
        try {
            await _agentService.RunTurnAsync(
                userInput,
                text => {
                    AgentLog.AppendAssistantText(text);
                    return Task.CompletedTask;
                },
                status => {
                    AgentLog.AddToolStatus(status);
                    return Task.CompletedTask;
                },
                _cts.Token);
        } catch (OperationCanceledException) {
            // Cancelled by user
        } finally {
            AgentLog.IsProcessing = false;
            _cts.Dispose();
            _cts = null;
        }
    }

    public void CancelCurrentOperation() {
        _cts?.Cancel();
    }
}