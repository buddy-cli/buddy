using Buddy.Core.Agents;
using Buddy.Core.Configuration;
using Buddy.Core.Application;
using Buddy.LLM;
using Terminal.Gui;

namespace Buddy.Cli.Ui;

internal sealed class ChatController {
    private readonly BuddyAgent _agent;
    private readonly ILLMClientFactory _llmClientFactory;
    private readonly ChatLayoutManager _layoutManager;
    private readonly ChatSessionState _state;
    private readonly ChatControllerContext _context;

    public ChatController(
        BuddyAgent agent,
        ILLMClientFactory llmClientFactory,
        ChatLayoutManager layoutManager,
        ChatSessionState state,
        ChatControllerContext context) {
        _agent = agent;
        _llmClientFactory = llmClientFactory;
        _layoutManager = layoutManager;
        _state = state;
        _context = context;
    }

    public void UpdateLogStyle() {
        _context.LayoutParts.History.ColorScheme = _state.HistoryBuffer.Length == 0 ? _context.IdleLogScheme : _context.ActiveLogScheme;
    }

    public async Task SendAsync() {
        if (_state.TurnInFlight) {
            return;
        }

        var text = _context.LayoutParts.Input.Text?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text)) {
            return;
        }

        if (TryHandleCommand(text)) {
            _context.LayoutParts.Input.Text = string.Empty;
            return;
        }

        _state.TurnInFlight = true;
        _context.LayoutParts.Input.Text = string.Empty;
        _context.LayoutParts.Input.Enabled = false;
        _context.LayoutParts.SendButton.Enabled = false;

        SwitchToSession();
        SetStage("Planning");

        AppendHistoryOnUi($"\nYou: {text}\nBuddy: ");

        _state.TurnCts = new CancellationTokenSource();

        try {
            await _agent.RunTurnAsync(
                _state.CurrentMClient,
                _context.SystemPrompt,
                _context.ProjectInstructions,
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
                _context.LayoutParts.Input.Enabled = true;
                _context.LayoutParts.SendButton.Enabled = true;
                _context.LayoutParts.Input.SetFocus();
            });

        }
    }

    private void AppendHistory(string text) {
        _state.HistoryBuffer.Append(text);
        _context.LayoutParts.History.MoveEnd();
        _context.LayoutParts.History.InsertText(text);
        UpdateLogStyle();
        _context.LayoutParts.History.MoveEnd();
        _context.LayoutParts.History.SetNeedsDraw();
        Application.LayoutAndDraw(forceDraw: false);
    }

    private void AppendHistoryOnUi(string text) {
        Application.Invoke(() => AppendHistory(text));
    }

    private void SetStage(string stage) {
        _state.CurrentStage = stage;
        var showSpinner = stage != "Idle" && stage != "Done" && stage != "Canceled" && stage != "Error";
        Application.Invoke(() => {
            _context.LayoutParts.StageLabel.Text = $"Stage: {_state.CurrentStage}";
            _context.LayoutParts.StageSpinner.Visible = showSpinner;
            _context.LayoutParts.StageSpinner.AutoSpin = showSpinner;
        });
    }

    private void SwitchToSession() {
        if (_state.SessionStarted) {
            return;
        }

        _state.SessionStarted = true;
        _context.LayoutParts.StartView.Visible = false;
        _context.LayoutParts.SessionView.Visible = true;
        _layoutManager.ApplyLayout(_state, _context.LayoutParts);
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
                AppendHistoryOnUi("\nCommands:\n  /help             Show this help\n  /clear            Clear conversation history\n  /model            Select model for next turns\n  /provider         Edit provider configuration\n  /exit or /quit    Exit\n");
                return true;
            case "/clear":
                _agent.ClearHistory();
                _state.HistoryBuffer.Clear();
                _context.LayoutParts.History.Text = string.Empty;
                UpdateLogStyle();
                AppendHistoryOnUi("\n(history cleared)\n");
                return true;
            case "/model":
                ShowModelDialog();
                return true;
            case "/provider":
                ShowProviderDialog();
                return true;
            case "/exit":
            case "/quit":
                if (_state.TurnCts is not null && !_state.TurnCts.IsCancellationRequested) {
                    _state.TurnCts.Cancel();
                }
                Application.RequestStop(_context.LayoutParts.Window);
                return true;
            default:
                AppendHistoryOnUi("\nunknown command — try /help\n");
                return true;
        }
    }

    private void ShowModelDialog() {
        if (!ModelSelectionDialog.TrySelectModel(_context.Options.Providers, out var providerIndex, out var modelIndex)) {
            return;
        }

        var provider = _context.Options.Providers[providerIndex];
        var model = provider.Models[modelIndex];

        _context.Options.Model = model.System;
        _context.Options.BaseUrl = provider.BaseUrl;
        _context.Options.ApiKey = provider.ApiKey;

        if (_state.CurrentMClient is IDisposable disposable) {
            disposable.Dispose();
        }

        _state.CurrentMClient = _llmClientFactory.Create(_context.Options.Model);

        TerminalGuiLayout.RefreshFooter(_context.Options, _context.Version, _context.LayoutParts);
        Application.LayoutAndDraw(forceDraw: false);

        AppendHistoryOnUi($"\nmodel set to {_context.Options.Model}\n");
    }

    private void ShowProviderDialog() {
        if (!ProviderConfigDialog.TryEditProviders(_context.Options.Providers, out var updatedProviders)) {
            return;
        }

        _context.Options.Providers = updatedProviders;
        BuddyOptionsLoader.ApplyPrimaryProviderDefaults(_context.Options);

        if (_state.CurrentMClient is IDisposable disposable) {
            disposable.Dispose();
        }

        _state.CurrentMClient = _llmClientFactory.Create(_context.Options.Model);

        var saved = true;

        try {
            var configPath = BuddyOptionsLoader.ResolveConfigPath();
            BuddyOptionsLoader.Save(configPath, new BuddyConfigFile { Providers = updatedProviders });
        }
        catch (Exception ex) {
            saved = false;
            AppendHistoryOnUi($"\nfailed to save provider config: {ex.Message}\n");
        }

        TerminalGuiLayout.RefreshFooter(_context.Options, _context.Version, _context.LayoutParts);
        Application.LayoutAndDraw(forceDraw: false);

        AppendHistoryOnUi($"\nmodel set to {_context.Options.Model}\n");
    }
}

