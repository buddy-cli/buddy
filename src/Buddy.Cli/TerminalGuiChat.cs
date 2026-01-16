using System.Text;
using Buddy.Core.Agents;
using Buddy.LLM;
using Terminal.Gui;

namespace Buddy.Cli;

internal static class TerminalGuiChat {
    public static async Task<int> RunAsync(
        BuddyAgent agent,
        ILLMClient llmClient,
        Func<string, ILLMClient> llmClientFactory,
        Buddy.Core.Configuration.BuddyOptions options,
        string systemPrompt,
        string? projectInstructions,
        CancellationToken cancellationToken) {
        var historyBuffer = new StringBuilder();
        var turnInFlight = false;
        CancellationTokenSource? turnCts = null;
        var currentClient = llmClient;

        Application.Init();
        var top = new Toplevel();
        var window = new Window {
            Title = "buddy (Esc to quit)",
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        try {

            var history = new TextView {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(2),
                ReadOnly = true,
                WordWrap = true
            };

            var input = new TextField {
                Text = "",
                X = 0,
                Y = Pos.Bottom(history),
                Width = Dim.Fill(12),
                Height = 1
            };

            var sendButton = new Button {
                Text = "Send",
                X = Pos.Right(input) + 1,
                Y = Pos.Bottom(history)
            };

            void AppendHistory(string text) {
                historyBuffer.Append(text);
                history.Text = historyBuffer.ToString();
                history.MoveEnd();
            }

            void AppendHistoryOnUi(string text) {
                Application.Invoke(() => AppendHistory(text));
            }

            async Task SendAsync() {
                if (turnInFlight) {
                    return;
                }

                var text = input.Text?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(text)) {
                    return;
                }

                if (TryHandleCommand(text)) {
                    input.Text = string.Empty;
                    return;
                }

                turnInFlight = true;
                input.Text = string.Empty;
                input.Enabled = false;
                sendButton.Enabled = false;

                AppendHistoryOnUi($"\nYou: {text}\nBuddy: ");

                turnCts = new CancellationTokenSource();

                try {
                    await agent.RunTurnAsync(
                        currentClient,
                        systemPrompt,
                        projectInstructions,
                        text,
                        onTextDelta: delta => {
                            AppendHistoryOnUi(delta);
                            return Task.CompletedTask;
                        },
                        onToolStatus: status => {
                            AppendHistoryOnUi($"\n{status}\n");
                            AppendHistoryOnUi("Buddy: ");
                            return Task.CompletedTask;
                        },
                        cancellationToken: turnCts.Token);
                }
                catch (OperationCanceledException) {
                    AppendHistoryOnUi("\n(canceled)\n");
                }
                catch (Exception ex) {
                    AppendHistoryOnUi($"\nerror: {ex.Message}\n");
                }
                finally {
                    turnCts?.Dispose();
                    turnCts = null;
                    turnInFlight = false;
                    Application.Invoke(() => {
                        input.Enabled = true;
                        sendButton.Enabled = true;
                        input.SetFocus();
                    });
                }
            }

            bool TryHandleCommand(string rawInput) {
                if (!rawInput.StartsWith("/", StringComparison.Ordinal)) {
                    return false;
                }

                var parts = rawInput.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var cmd = parts[0];
                var arg = parts.Length > 1 ? parts[1] : null;

                switch (cmd) {
                    case "/help":
                        AppendHistoryOnUi("\nCommands:\n  /help            Show this help\n  /clear           Clear conversation history\n  /model <name>    Switch model for next turns\n  /exit or /quit   Exit\n");
                        return true;
                    case "/clear":
                        agent.ClearHistory();
                        historyBuffer.Clear();
                        history.Text = string.Empty;
                        AppendHistoryOnUi("\n(history cleared)\n");
                        return true;
                    case "/model":
                        if (string.IsNullOrWhiteSpace(arg)) {
                            AppendHistoryOnUi($"\ncurrent model {options.Model}\n");
                            return true;
                        }

                        options.Model = arg.Trim();
                        if (currentClient is IDisposable disposable) {
                            disposable.Dispose();
                        }
                        currentClient = llmClientFactory(options.Model);
                        AppendHistoryOnUi($"\nmodel set to {options.Model}\n");
                        return true;
                    case "/exit":
                    case "/quit":
                        if (turnCts is not null && !turnCts.IsCancellationRequested) {
                            turnCts.Cancel();
                        }
                        Application.RequestStop();
                        return true;
                    default:
                        AppendHistoryOnUi("\nunknown command — try /help\n");
                        return true;
                }
            }

            sendButton.Accepting += (_, _) => _ = SendAsync();
            sendButton.IsDefault = true;

            window.Add(history, input, sendButton);
            top.Add(window);

            input.SetFocus();
            AppendHistory("Type a message and press Enter (Esc to quit).\n\n");

            Application.Run(top);
        }
        finally {
            window.Dispose();
            turnCts?.Dispose();
            Application.Shutdown();
        }

        return 0;
    }
}
