using System.Collections.ObjectModel;
using Buddy.Cli.Logging;
using Buddy.Cli.Ui;
using Buddy.Core.Agents;
using Buddy.Core.Application;
using Buddy.Core.Configuration;
using Buddy.LLM;
using Terminal.Gui;

namespace Buddy.Cli;

internal sealed class ChatApplication {
    private readonly BuddyAgent _agent;
    private readonly ILLMClient _llmClient;
    private readonly ILLMClientFactory _llmClientFactory;
    private readonly BuddyOptions _options;
    private readonly string _version;
    private readonly string _systemPrompt;
    private readonly string? _projectInstructions;
    private readonly ChatLayoutMetrics _metrics;
    private readonly ISessionLogger _sessionLogger;
    private readonly IReadOnlyList<SlashCommandOption> _slashCommands;
    private readonly ChatLayoutManager _layoutManager;
    private readonly ChatSessionState _state;
    private readonly CancellationToken _cancellationToken;

    public ChatApplication(
        string version,
        string systemPrompt,
        string? projectInstructions,
        CancellationToken cancellationToken,
        BuddyAgent agent,
        ILLMClient llmClient,
        ILLMClientFactory llmClientFactory,
        BuddyOptions options,
        ChatLayoutMetrics metrics,
        ISessionLogger sessionLogger,
        IReadOnlyList<SlashCommandOption> slashCommands,
        ChatLayoutManager layoutManager,
        ChatSessionState state) {
        _agent = agent;
        _llmClient = llmClient;
        _llmClientFactory = llmClientFactory;
        _options = options;
        _version = version;
        _systemPrompt = systemPrompt;
        _projectInstructions = projectInstructions;
        _metrics = metrics;
        _sessionLogger = sessionLogger;
        _slashCommands = slashCommands;
        _layoutManager = layoutManager;
        _state = state;
        _cancellationToken = cancellationToken;
    }

    public Task<int> RunAsync() {
        var logger = _sessionLogger.Create(_version, _options.Model);
        var loggingClient = new LoggingLlmClient(_llmClient, logger, () => _options.Model);
        _state.CurrentClient = loggingClient;

        Application.Init();
        var suggestionItems = new ObservableCollection<string>();
        var bannerText = TerminalGuiLayout.BannerText;
        var bannerLines = TerminalGuiLayout.BannerLines;
        var layoutParts = TerminalGuiLayout.Build(
            _options,
            _version,
            bannerText,
            bannerLines,
            _metrics,
            _state.CurrentStage,
            _state.InputHeight,
            suggestionItems);

        using var cancellationRegistration = _cancellationToken.Register(() => Application.RequestStop(layoutParts.Window));
        try {
            var driver = Application.Driver ?? throw new InvalidOperationException("Terminal driver not initialized.");
            var idleLogScheme = new ColorScheme {
                Normal = driver.MakeColor(Color.White, Color.Blue),
                Focus = driver.MakeColor(Color.White, Color.Blue)
            };

            var activeLogScheme = new ColorScheme {
                Normal = driver.MakeColor(Color.Black, Color.White),
                Focus = driver.MakeColor(Color.Black, Color.White)
            };

            var controllerContext = new ChatControllerContext(
                _options,
                _version,
                _systemPrompt,
                _projectInstructions,
                idleLogScheme,
                activeLogScheme,
                layoutParts);

            var controller = new ChatController(
                _agent,
                _llmClientFactory,
                _layoutManager,
                _state,
                controllerContext);

            var slashCommandUi = new SlashCommandUi(
                layoutParts.Input,
                layoutParts.SuggestionOverlay,
                suggestionItems,
                _slashCommands.ToList(),
                _state,
                () => _layoutManager.ApplyLayout(_state, layoutParts));
            slashCommandUi.Initialize();

            layoutParts.SendButton.Accepting += (_, _) => _ = controller.SendAsync();
            layoutParts.SendButton.IsDefault = true;

            layoutParts.Input.KeyDown += (_, key) => {
                if (key.KeyCode == KeyCode.Tab) {
                    if (layoutParts.Input.Autocomplete.Visible) {
                        return;
                    }

                    key.Handled = true;
                    layoutParts.Input.Text += "    ";
                    return;
                }

                if (key.IsAlt && key.KeyCode == KeyCode.CursorUp) {
                    key.Handled = true;
                    _state.InputHeight = Math.Min(10, _state.InputHeight + 1);
                    _layoutManager.ApplyLayout(_state, layoutParts);
                    return;
                }

                if (key.IsAlt && key.KeyCode == KeyCode.CursorDown) {
                    key.Handled = true;
                    _state.InputHeight = Math.Max(2, _state.InputHeight - 1);
                    _layoutManager.ApplyLayout(_state, layoutParts);
                    return;
                }

                if (key.IsCtrl && key.KeyCode == KeyCode.Enter) {
                    key.Handled = true;
                    _ = controller.SendAsync();
                }
            };

            layoutParts.Input.SetFocus();
            controller.UpdateLogStyle();
            _layoutManager.ApplyLayout(_state, layoutParts);

            Application.Run(layoutParts.Window);
        }
        finally {
            layoutParts.Window.Dispose();
            _state.TurnCts?.Dispose();
            Application.Shutdown();
        }

        return Task.FromResult(0);
    }
}
