using System.Reactive;
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
    private readonly BuddyOptions _options;
    private CancellationTokenSource? _cts;

    /// <summary>
    /// Interaction to request application exit. View handles the actual shutdown.
    /// </summary>
    public Interaction<Unit, Unit> RequestExit { get; } = new();

    /// <summary>
    /// Interaction to show the model selection dialog. View handles dialog display.
    /// </summary>
    public Interaction<ModelSelectionDialogViewModel, ModelSelectionResult?> ShowModelDialog { get; } = new();

    /// <summary>
    /// Interaction to show the provider configuration dialog. View handles dialog display.
    /// </summary>
    public Interaction<ProviderConfigDialogViewModel, ProviderConfigResult?> ShowProviderDialog { get; } = new();

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
        _options = options;
        
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
            AgentLog.SetIdle();
            _cts.Dispose();
            _cts = null;
        }
    }

    public void CancelCurrentOperation() {
        _cts?.Cancel();
    }

    /// <summary>
    /// Attempts to execute the current slash command.
    /// Returns true if a command was executed, false otherwise.
    /// </summary>
    public bool TryExecuteSlashCommand() {
        var input = InputText.Trim();
        if (!input.StartsWith("/")) {
            return false;
        }

        var command = input.TrimStart('/').Split(' ')[0].ToLowerInvariant();
        
        switch (command) {
            case "exit":
                RequestExit.Handle(Unit.Default).Subscribe();
                return true;
            case "model":
                ExecuteModelCommandAsync();
                return true;
            case "provider":
                ExecuteProviderCommandAsync();
                return true;
            default:
                return false;
        }
    }

    private async void ExecuteModelCommandAsync() {
        InputText = string.Empty;
        
        var dialogViewModel = new ModelSelectionDialogViewModel(_options.Providers);
        if (!dialogViewModel.HasItems) {
            return;
        }

        var result = await ShowModelDialog.Handle(dialogViewModel);
        
        if (result is not null) {
            _agentService.ChangeModel(result.Provider, result.Model);
            
            var modelDisplayName = string.IsNullOrWhiteSpace(result.Model.Name) 
                ? result.Model.System 
                : result.Model.Name;
            var providerName = string.IsNullOrWhiteSpace(result.Provider.Name) 
                ? "(unnamed)" 
                : result.Provider.Name;
            ModelInfo = $"{modelDisplayName} ({providerName})";
        }
    }

    private async void ExecuteProviderCommandAsync() {
        InputText = string.Empty;
        
        var dialogViewModel = new ProviderConfigDialogViewModel(_options.Providers);

        var result = await ShowProviderDialog.Handle(dialogViewModel);
        
        if (result is not null) {
            _options.Providers.Clear();
            _options.Providers.AddRange(result.Providers);
            
            // Refresh the model info display
            var provider = _options.Providers
                .FirstOrDefault(p => p.Models.Any(m => m.System == _options.Model));
            var providerName = provider?.Name ?? "Unknown";
            var modelDisplayName = provider?.Models.FirstOrDefault(m => m.System == _options.Model)?.Name ?? _options.Model;
            ModelInfo = $"{modelDisplayName} ({providerName})";
        }
    }
}