using Buddy.Core.Agents;
using Buddy.Core.Configuration;
using Buddy.LLM;
using Terminal.Gui;

namespace Buddy.Cli.Ui;

internal sealed class ChatController {
    private readonly BuddyAgent _agent;
    private readonly Func<string, ILLMClient> _llmClientFactory;
    private readonly BuddyOptions _options;
    private readonly string _systemPrompt;
    private readonly string? _projectInstructions;
    private readonly ChatSessionState _state;
    private readonly ChatLayoutParts _parts;
    private readonly ChatLayoutManager _layoutManager;
    private readonly ColorScheme _idleLogScheme;
    private readonly ColorScheme _activeLogScheme;
    private readonly string _version;

    public ChatController(
        BuddyAgent agent,
        Func<string, ILLMClient> llmClientFactory,
        BuddyOptions options,
        string systemPrompt,
        string? projectInstructions,
        ChatSessionState state,
        ChatLayoutParts parts,
        ChatLayoutManager layoutManager,
        ColorScheme idleLogScheme,
        ColorScheme activeLogScheme,
        string version) {
        _agent = agent;
        _llmClientFactory = llmClientFactory;
        _options = options;
        _systemPrompt = systemPrompt;
        _projectInstructions = projectInstructions;
        _state = state;
        _parts = parts;
        _layoutManager = layoutManager;
        _idleLogScheme = idleLogScheme;
        _activeLogScheme = activeLogScheme;
        _version = version;
    }

    public void UpdateLogStyle() {
        _parts.History.ColorScheme = _state.HistoryBuffer.Length == 0 ? _idleLogScheme : _activeLogScheme;
    }

    public async Task SendAsync() {
        if (_state.TurnInFlight) {
            return;
        }

        var text = _parts.Input.Text?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text)) {
            return;
        }

        if (TryHandleCommand(text)) {
            _parts.Input.Text = string.Empty;
            return;
        }

        _state.TurnInFlight = true;
        _parts.Input.Text = string.Empty;
        _parts.Input.Enabled = false;
        _parts.SendButton.Enabled = false;

        SwitchToSession();
        SetStage("Planning");

        AppendHistoryOnUi($"\nYou: {text}\nBuddy: ");

        _state.TurnCts = new CancellationTokenSource();

        try {
            await _agent.RunTurnAsync(
                _state.CurrentClient,
                _systemPrompt,
                _projectInstructions,
                text,
                onTextDelta: delta => {
                    SetStage("Responding");
                    AppendHistoryOnUi(delta);
                    return Task.CompletedTask;
                },
                onToolStatus: status => {
                    SetStage("Tooling");
                    AppendHistoryOnUi($"\n{status}\n");
                    AppendHistoryOnUi("Buddy: ");
                    return Task.CompletedTask;
                },
                cancellationToken: _state.TurnCts.Token);
        }
        catch (OperationCanceledException) {
            SetStage("Canceled");
            AppendHistoryOnUi("\n(canceled)\n");
        }
        catch (Exception ex) {
            SetStage("Error");
            AppendHistoryOnUi($"\nerror: {ex.Message}\n");
        }
        finally {
            SetStage("Done");
            _state.TurnCts?.Dispose();
            _state.TurnCts = null;
            _state.TurnInFlight = false;
            Application.Invoke(() => {
                _parts.Input.Enabled = true;
                _parts.SendButton.Enabled = true;
                _parts.Input.SetFocus();
            });
        }
    }

    private void AppendHistory(string text) {
        _state.HistoryBuffer.Append(text);
        _parts.History.MoveEnd();
        _parts.History.InsertText(text);
        UpdateLogStyle();
        _parts.History.MoveEnd();
        _parts.History.SetNeedsDraw();
        Application.LayoutAndDraw(forceDraw: false);
    }

    private void AppendHistoryOnUi(string text) {
        Application.Invoke(() => AppendHistory(text));
    }

    private void SetStage(string stage) {
        _state.CurrentStage = stage;
        var showSpinner = stage != "Idle" && stage != "Done" && stage != "Canceled" && stage != "Error";
        Application.Invoke(() => {
            _parts.StageLabel.Text = $"Stage: {_state.CurrentStage}";
            _parts.StageSpinner.Visible = showSpinner;
            _parts.StageSpinner.AutoSpin = showSpinner;
        });
    }

    private void SwitchToSession() {
        if (_state.SessionStarted) {
            return;
        }

        _state.SessionStarted = true;
        _parts.StartView.Visible = false;
        _parts.SessionView.Visible = true;
        _layoutManager.ApplyLayout(_state, _parts);
    }

    private bool TryHandleCommand(string rawInput) {
        if (!rawInput.StartsWith("/", StringComparison.Ordinal)) {
            return false;
        }

        var parts = rawInput.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var cmd = parts[0];
        var arg = parts.Length > 1 ? parts[1] : null;

        switch (cmd) {
            case "/help":
                AppendHistoryOnUi("\nCommands:\n  /help             Show this help\n  /clear            Clear conversation history\n  /model <name>     Switch model for next turns\n  /provider         Edit provider configuration\n  /exit or /quit    Exit\n");
                return true;
            case "/clear":
                _agent.ClearHistory();
                _state.HistoryBuffer.Clear();
                _parts.History.Text = string.Empty;
                UpdateLogStyle();
                AppendHistoryOnUi("\n(history cleared)\n");
                return true;
            case "/model":
                if (string.IsNullOrWhiteSpace(arg)) {
                    AppendHistoryOnUi($"\ncurrent model {_options.Model}\n");
                    return true;
                }

                _options.Model = arg.Trim();
                if (_state.CurrentClient is IDisposable disposable) {
                    disposable.Dispose();
                }
                _state.CurrentClient = _llmClientFactory(_options.Model);
                AppendHistoryOnUi($"\nmodel set to {_options.Model}\n");
                return true;
            case "/provider":
                ShowProviderDialog();
                return true;
            case "/exit":
            case "/quit":
                if (_state.TurnCts is not null && !_state.TurnCts.IsCancellationRequested) {
                    _state.TurnCts.Cancel();
                }
                Application.RequestStop(_parts.Window);
                return true;
            default:
                AppendHistoryOnUi("\nunknown command — try /help\n");
                return true;
        }
    }

    private void ShowProviderDialog() {
        if (!ProviderConfigDialog.TryEditProviders(_options.Providers, out var updatedProviders)) {
            return;
        }

        _options.Providers = updatedProviders;
        BuddyOptionsLoader.ApplyPrimaryProviderDefaults(_options);

        if (_state.CurrentClient is IDisposable disposable) {
            disposable.Dispose();
        }

        _state.CurrentClient = _llmClientFactory(_options.Model);

        var saved = true;
        try {
            var configPath = BuddyOptionsLoader.ResolveConfigPath();
            BuddyOptionsLoader.Save(configPath, new BuddyConfigFile { Providers = updatedProviders });
        }
        catch (Exception ex) {
            saved = false;
            AppendHistoryOnUi($"\nfailed to save provider config: {ex.Message}\n");
        }

        TerminalGuiLayout.RefreshFooter(_options, _version, _parts);
        Application.LayoutAndDraw(forceDraw: false);
        if (saved) {
            AppendHistoryOnUi("\n(provider configuration saved)\n");
        }
    }
}
