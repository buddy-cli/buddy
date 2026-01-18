using System.Reactive.Concurrency;
using Buddy.Cli.Logging;
using Buddy.Cli.Ui;
using Buddy.Core.Agents;
using Buddy.Core.Application;
using Buddy.Core.Configuration;
using Buddy.LLM;
using ReactiveUI;
using Terminal.Gui;

namespace Buddy.Cli;

internal sealed class ChatApplication {
    private readonly BuddyAgent _agent;
    private readonly ILlmMClient _llmMClient;
    private readonly ILLMClientFactory _llmClientFactory;
    private readonly BuddyOptions _options;
    private readonly string _version;
    private readonly string _systemPrompt;
    private readonly string? _projectInstructions;
    private readonly ChatLayoutMetrics _metrics;
    private readonly ISessionLogger _sessionLogger;
    private readonly IReadOnlyList<SlashCommandOption> _slashCommands;
    private readonly CancellationToken _cancellationToken;

    public ChatApplication(
        string version,
        string systemPrompt,
        string? projectInstructions,
        CancellationToken cancellationToken,
        BuddyAgent agent,
        ILlmMClient llmMClient,
        ILLMClientFactory llmClientFactory,
        BuddyOptions options,
        ChatLayoutMetrics metrics,
        ISessionLogger sessionLogger,
        IReadOnlyList<SlashCommandOption> slashCommands) {
        _agent = agent;
        _llmMClient = llmMClient;
        _llmClientFactory = llmClientFactory;
        _options = options;
        _version = version;
        _systemPrompt = systemPrompt;
        _projectInstructions = projectInstructions;
        _metrics = metrics;
        _sessionLogger = sessionLogger;
        _slashCommands = slashCommands;
        _cancellationToken = cancellationToken;
    }

    // public Task<int> RunAsync() {
    //     // Create logging client wrapper
    //     var logger = _sessionLogger.Create(_version, _options.Model);
    //     var loggingClient = new LoggingLlmMClient(_llmMClient, logger, () => _options.Model);
    //
    //     Application.Init();
    //
    //     // Configure ReactiveUI to use Terminal.Gui's main thread
    //     RxApp.MainThreadScheduler = TerminalScheduler.Instance;
    //     RxApp.TaskpoolScheduler = TaskPoolScheduler.Default;
    //
    //     // Create the ViewModel
    //     using var viewModel = new ChatViewModel(
    //         _agent,
    //         _llmClientFactory,
    //         loggingClient,
    //         _options,
    //         _systemPrompt,
    //         _projectInstructions);
    //
    //     // Create the View and bind to ViewModel
    //     var chatView = new ChatView(_metrics, _version) {
    //         ViewModel = viewModel
    //     };
    //
    //     // Setup slash command UI integration
    //     var slashCommandUi = new SlashCommandUi(
    //         chatView.InputView,
    //         chatView.SuggestionOverlayView,
    //         chatView.SuggestionItems,
    //         _slashCommands.ToList(),
    //         viewModel,
    //         () => { /* Layout is handled reactively */ });
    //     slashCommandUi.Initialize();
    //
    //     // Handle dialog commands
    //     viewModel.ShowModelDialogCommand.Subscribe(_ => {
    //         if (ModelSelectionDialog.TrySelectModel(_options.Providers, out var providerIndex, out var modelIndex)) {
    //             viewModel.UpdateModel(providerIndex, modelIndex);
    //         }
    //     });
    //
    //     viewModel.ShowProviderDialogCommand.Subscribe(_ => {
    //         if (ProviderConfigDialog.TryEditProviders(_options.Providers, out var updatedProviders)) {
    //             viewModel.UpdateProviders(updatedProviders);
    //             try {
    //                 var configPath = BuddyOptionsLoader.ResolveConfigPath();
    //                 BuddyOptionsLoader.Save(configPath, new BuddyConfigFile { Providers = updatedProviders });
    //             }
    //             catch {
    //                 // Logged via ViewModel
    //             }
    //         }
    //     });
    //
    //     using var cancellationRegistration = _cancellationToken.Register(() => Application.RequestStop(chatView));
    //
    //     try {
    //         Application.Run(chatView);
    //     }
    //     finally {
    //         chatView.Dispose();
    //         Application.Shutdown();
    //     }
    //
    //     return Task.FromResult(0);
    // }
}
