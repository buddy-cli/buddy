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
        string version,
        string systemPrompt,
        string? projectInstructions,
        CancellationToken cancellationToken) {
        var historyBuffer = new StringBuilder();
        var turnInFlight = false;
        CancellationTokenSource? turnCts = null;
        var currentClient = llmClient;
        var bannerText = string.Join("\n", new[] {
            "██████╗ ██╗   ██╗██████╗ ██████╗ ██╗   ██╗",
            "██╔══██╗██║   ██║██╔══██╗██╔══██╗╚██╗ ██╔╝",
            "██████╔╝██║   ██║██║  ██║██║  ██║ ╚████╔╝ ",
            "██╔══██╗██║   ██║██║  ██║██║  ██║  ╚██╔╝  ",
            "██████╔╝╚██████╔╝██████╔╝██████╔╝   ██║   ",
            "╚═════╝  ╚═════╝ ╚═════╝ ╚═════╝    ╚═╝   "
        });
        var bannerLines = bannerText.Split('\n').Length;
        var infoLines = 2;
        var headerHeight = bannerLines + infoLines;
        var inputHeight = 3;

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
            var header = new View {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = headerHeight
            };

            var headerTitle = new Label {
                Text = bannerText,
                X = 1,
                Y = 0
            };

            var headerInfo = new Label {
                Text = $"version {version}  •  model {options.Model}  •  base url {options.BaseUrl ?? "(default)"}\nworking dir {options.WorkingDirectory}",
                X = 1,
                Y = bannerLines
            };

            header.Add(headerTitle, headerInfo);

            var history = new TextView {
                X = 0,
                Y = Pos.Bottom(header),
                Width = Dim.Fill(),
                Height = Dim.Fill(headerHeight + inputHeight + 2),
                ReadOnly = true,
                WordWrap = true,
                CanFocus = false,
                TabStop = TabBehavior.NoStop
            };

            var inputPanel = new View {
                X = 0,
                Y = Pos.AnchorEnd(inputHeight + 2),
                Width = Dim.Fill(),
                Height = inputHeight + 2,
                CanFocus = true,
                TabStop = TabBehavior.TabStop
            };

            var input = new TextView {
                X = 1,
                Y = 0,
                Width = Dim.Fill(12),
                Height = inputHeight,
                WordWrap = true,
                CanFocus = true,
                TabStop = TabBehavior.TabStop
            };

            var sendButton = new Button {
                Text = "Send",
                X = Pos.AnchorEnd(10),
                Y = Pos.Bottom(input) - 1,
                CanFocus = true,
                TabStop = TabBehavior.TabStop
            };

            void AppendHistory(string text) {
                historyBuffer.Append(text);
                history.Text = historyBuffer.ToString();
                history.MoveEnd();
            }

            void AppendHistoryOnUi(string text) {
                Application.Invoke(() => AppendHistory(text));
            }

            void ApplyLayout() {
                input.Height = inputHeight;
                inputPanel.Height = inputHeight + 2;
                inputPanel.Y = Pos.AnchorEnd(inputHeight + 2);
                history.Height = Dim.Fill(headerHeight + inputHeight + 2);
                history.Y = Pos.Bottom(header);
                inputPanel.SetNeedsLayout();
                history.SetNeedsLayout();
                window.SetNeedsLayout();
                Application.LayoutAndDraw();
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

            input.KeyDown += (_, key) => {
                if (key.KeyCode == KeyCode.Tab) {
                    key.Handled = true;
                    input.Text += "    ";
                    return;
                }

                if (key.IsAlt && key.KeyCode == KeyCode.CursorUp) {
                    key.Handled = true;
                    inputHeight = Math.Min(10, inputHeight + 1);
                    ApplyLayout();
                    return;
                }

                if (key.IsAlt && key.KeyCode == KeyCode.CursorDown) {
                    key.Handled = true;
                    inputHeight = Math.Max(2, inputHeight - 1);
                    ApplyLayout();
                    return;
                }

                if (key.IsCtrl && key.KeyCode == KeyCode.Enter) {
                    key.Handled = true;
                    _ = SendAsync();
                }
            };

            inputPanel.Add(input, sendButton);
            window.Add(header, history, inputPanel);
            top.Add(window);

            input.SetFocus();
            AppendHistory("Type a message. Ctrl+Enter sends. Alt+Up/Down resizes input. (Esc to quit).\n\n");
            ApplyLayout();

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
