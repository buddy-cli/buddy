using System.Reactive;
using System.Reactive.Subjects;
using System.Text;
using Buddy.Core.Agents;
using Buddy.Core.Configuration;
using Buddy.LLM;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace Buddy.Cli.Ui;

/// <summary>
/// ViewModel for the chat interface, implementing reactive MVVM pattern.
/// Manages chat state, handles commands, and exposes observable properties for UI binding.
/// </summary>
internal sealed partial class ChatViewModel : ReactiveObject, IDisposable {
    private readonly BuddyAgent _agent;
    private readonly Subject<string> _historyAppended = new();
    private readonly Subject<Unit> _historyClearRequested = new();
    private readonly StringBuilder _historyBuffer = new();
    private CancellationTokenSource? _turnCts;
    private bool _disposed;

    public ChatViewModel(
        BuddyAgent agent,
        ILlmMClient initialClient,
        BuddyOptions options,
        string systemPrompt,
        string? projectInstructions) {
        _agent = agent;
        CurrentMClient = initialClient;
        Options = options;

        CancelCommand = ReactiveCommand.Create(ExecuteCancel, this.WhenAnyValue(x => x.TurnInFlight));
        ClearCommand = ReactiveCommand.Create(ExecuteClear);
        ExitCommand = ReactiveCommand.Create(ExecuteExit);
        ShowModelDialogCommand = ReactiveCommand.Create<Unit>(_ => { });
        ShowProviderDialogCommand = ReactiveCommand.Create<Unit>(_ => { });
    }

    #region Reactive Properties

    /// <summary>Current input text from the user.</summary>
    [Reactive]
    private string _inputText = string.Empty;

    /// <summary>Whether a turn is currently in progress.</summary>
    [Reactive]
    private bool _turnInFlight;

    /// <summary>Whether the session has started (showing chat view vs start view).</summary>
    [Reactive]
    private bool _sessionStarted;

    /// <summary>Current stage of processing (Idle, Planning, Tooling, Responding, etc.).</summary>
    [Reactive]
    private string _currentStage = "Idle";

    /// <summary>Height of the input area in rows.</summary>
    [Reactive]
    private int _inputHeight = 3;

    /// <summary>Number of rows for the suggestion overlay.</summary>
    [Reactive]
    private int _suggestionOverlayRows;

    /// <summary>The current LLM client.</summary>
    [Reactive]
    private ILlmMClient _currentMClient;

    #endregion

    #region Observable As Properties

    [ObservableAsProperty]
    private bool _showSpinner;

    #endregion

    #region Commands

    public ReactiveCommand<Unit, Unit> SendCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }
    public ReactiveCommand<Unit, Unit> ClearCommand { get; }
    public ReactiveCommand<Unit, Unit> ExitCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowModelDialogCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowProviderDialogCommand { get; }

    #endregion

    #region Non-Reactive Properties

    public BuddyOptions Options { get; }
    public string SystemPrompt { get; }
    public string? ProjectInstructions { get; }
    
    #endregion

    #region Command Implementations

    private async Task ExecuteSendAsync(CancellationToken cancellationToken) {
        var text = InputText?.Trim();
        if (string.IsNullOrEmpty(text)) {
            return;
        }

        // Handle slash commands
        if (TryHandleCommand(text)) {
            InputText = string.Empty;
            return;
        }

        TurnInFlight = true;
        InputText = string.Empty;

        if (!SessionStarted) {
            SessionStarted = true;
        }

        CurrentStage = "Planning";
        AppendHistory($"\nYou: {text}\nBuddy: ");

        _turnCts = new CancellationTokenSource();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            _turnCts.Token,
            cancellationToken);

        try {
            await _agent.RunTurnAsync(
                CurrentMClient,
                SystemPrompt,
                ProjectInstructions,
                text,
                onTextDelta: delta => {
                    CurrentStage = "Responding";
                    AppendHistory(delta);
                    return Task.CompletedTask;
                },
                onToolStatus: status => {
                    CurrentStage = "Tooling";
                    AppendHistory($"\n{status}\n");
                    AppendHistory("Buddy: ");
                    return Task.CompletedTask;
                },
                cancellationToken: linkedCts.Token);

            CurrentStage = "Done";
        }
        catch (OperationCanceledException) {
            CurrentStage = "Canceled";
            AppendHistory("\n(canceled)\n");
        }
        catch (Exception ex) {
            CurrentStage = "Error";
            AppendHistory($"\nerror: {ex.Message}\n");
        }
        finally {
            _turnCts?.Dispose();
            _turnCts = null;
            TurnInFlight = false;
        }
    }

    private void ExecuteCancel() {
        _turnCts?.Cancel();
    }

    private void ExecuteClear() {
        _agent.ClearHistory();
        _historyBuffer.Clear();
        _historyClearRequested.OnNext(Unit.Default);
        AppendHistory("\n(history cleared)\n");
    }

    private void ExecuteExit() {
        _turnCts?.Cancel();
        //Application.RequestStop();
    }

    private bool TryHandleCommand(string rawInput) {
        if (!rawInput.StartsWith('/')) {
            return false;
        }

        var parts = rawInput.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var cmd = parts[0];

        switch (cmd) {
            case "/help":
                AppendHistory("\nCommands:\n  /help             Show this help\n  /clear            Clear conversation history\n  /model            Select model for next turns\n  /provider         Edit provider configuration\n  /exit or /quit    Exit\n");
                return true;
            case "/clear":
                ExecuteClear();
                return true;
            case "/model":
                ShowModelDialogCommand.Execute().Subscribe();
                return true;
            case "/provider":
                ShowProviderDialogCommand.Execute().Subscribe();
                return true;
            case "/exit":
                ExecuteExit();
                return true;
            default:
                AppendHistory("\nunknown command — try /help\n");
                return true;
        }
    }

    #endregion
    

    private void AppendHistory(string text) {
        _historyBuffer.Append(text);
        _historyAppended.OnNext(text);
    }
    

    public void Dispose() {
        if (_disposed) {
            return;
        }

        _disposed = true;
        _turnCts?.Cancel();
        _turnCts?.Dispose();
        _historyAppended.Dispose();
        _historyClearRequested.Dispose();

        if (CurrentMClient is IDisposable disposable) {
            disposable.Dispose();
        }
    }
}
