using System.Collections.ObjectModel;
using System.Text;
using Buddy.Cli.Ui;
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
        var sessionHeaderHeight = 2;
        var stageHeight = 1;
        var infoHintHeight = 1;
        var infoLayerHeight = infoHintHeight;
        var inputHeight = 3;
        var footerHeight = 1;
        var sessionStarted = false;
        var currentStage = "Idle";

        // Define available slash commands
        var slashCommands = new List<SlashCommandOption>
        {
            new() { Command = "help", Description = "Show available commands" },
            new() { Command = "clear", Description = "Clear conversation history" },
            new() { Command = "model", Description = "Switch or view current model", ParameterHint = "<name>" },
            new() { Command = "exit", Description = "Exit the application" },
            new() { Command = "quit", Description = "Exit the application" }
        };

        Application.Init();
        var window = new Window {
            Title = "buddy - coding agent (Esc to quit)",
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        using var cancellationRegistration = cancellationToken.Register(() => Application.RequestStop(window));
        try {
            var startView = new View {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };

            var headerTitle = new Label {
                Text = bannerText,
                X = 1,
                Y = 0
            };

            var headerInfo = new Label {
                Text = $"model {options.Model}  •  base url {options.BaseUrl ?? "(default)"}",
                X = 1,
                Y = bannerLines
            };

            startView.Add(headerTitle, headerInfo);

            var sessionView = new View {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                Visible = false
            };

            var sessionHeader = new View {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = sessionHeaderHeight
            };

            var sessionTitle = new Label {
                Text = $"buddy  •  v{version}  •  model {options.Model}",
                X = 1,
                Y = 0
            };

            var sessionInfo = new Label {
                Text = $"working dir {options.WorkingDirectory}",
                X = 1,
                Y = 1
            };

            sessionHeader.Add(sessionTitle, sessionInfo);

            var stageLabel = new Label {
                Text = $"Stage: {currentStage}",
                X = 1,
                Y = Pos.Bottom(sessionHeader)
            };

            var history = new TextView {
                X = 0,
                Y = Pos.Bottom(stageLabel),
                Width = Dim.Fill(),
                Height = Dim.Fill(sessionHeaderHeight + stageHeight + infoLayerHeight + inputHeight + footerHeight + 3),
                ReadOnly = true,
                WordWrap = true,
                CanFocus = false,
                TabStop = TabBehavior.NoStop
            };

            var driver = Application.Driver ?? throw new InvalidOperationException("Terminal driver not initialized.");

            var idleLogScheme = new ColorScheme {
                Normal = driver.MakeColor(Color.White, Color.Blue),
                Focus = driver.MakeColor(Color.White, Color.Blue)
            };

            var activeLogScheme = new ColorScheme {
                Normal = driver.MakeColor(Color.Black, Color.White),
                Focus = driver.MakeColor(Color.Black, Color.White)
            };

            void UpdateLogStyle()
            {
                history.ColorScheme = historyBuffer.Length == 0 ? idleLogScheme : activeLogScheme;
            }

            var inputPanel = new View {
                X = 0,
                Y = Pos.AnchorEnd(inputHeight + footerHeight + 1),
                Width = Dim.Fill(),
                Height = inputHeight + 1,
                CanFocus = true,
                TabStop = TabBehavior.TabStop
            };

            var infoLayer = new View {
                X = 0,
                Y = Pos.AnchorEnd(inputHeight + footerHeight + infoLayerHeight + 1),
                Width = Dim.Fill(),
                Height = infoLayerHeight,
                CanFocus = false,
                TabStop = TabBehavior.NoStop
            };

            var footer = new View {
                X = 0,
                Y = Pos.AnchorEnd(footerHeight),
                Width = Dim.Fill(),
                Height = footerHeight,
                CanFocus = false,
                TabStop = TabBehavior.NoStop
            };

            var footerLeft = new Label {
                Text = options.WorkingDirectory,
                X = 1,
                Y = 0
            };

            var footerRight = new Label {
                Text = version,
                X = Pos.AnchorEnd(version.Length + 1),
                Y = 0
            };

            footer.Add(footerLeft, footerRight);

            var inputHint = new Label {
                Text = "Type a message or /command (e.g., /help) ",
                X = 1,
                Y = 0
            };

            var suggestionItems = new ObservableCollection<string>();
            var suggestionOverlayRows = 0;

            var suggestionOverlay = new ListView {
                X = 1,
                Y = 0,
                Width = Dim.Fill(12),
                Height = 0,
                CanFocus = false,
                TabStop = TabBehavior.NoStop,
                AllowsMarking = false,
                Visible = false
            };
            suggestionOverlay.SetSource(suggestionItems);

            infoLayer.Add(inputHint);

            var input = new TextView {
                X = 1,
                Y = 1,
                Width = Dim.Fill(12),
                Height = inputHeight,
                WordWrap = true,
                CanFocus = true,
                TabStop = TabBehavior.TabStop
            };

            // Configure autocomplete for slash commands
            input.Autocomplete.SuggestionGenerator = new SlashCommandSuggestionGenerator(slashCommands);
            input.Autocomplete.SelectionKey = KeyCode.Tab; // Use Tab to select suggestions (Enter sends the message)
            input.Autocomplete.PopupInsideContainer = false; // Render popup outside the input box (so it can expand upward)
            input.Autocomplete.MaxHeight = 8;
            
            // Update suggestions when input changes
            input.ContentsChanged += (_, _) =>
            {
                var text = input.Text?.ToString() ?? string.Empty;

                if (string.IsNullOrEmpty(text) || !text.StartsWith("/"))
                {
                    suggestionOverlay.Visible = false;
                    suggestionItems.Clear();
                    suggestionOverlayRows = 0;
                    input.Autocomplete.ClearSuggestions();
                    input.Autocomplete.Visible = false;
                    ApplyLayout();
                    return;
                }

                var commandPrefix = text.Length > 1 ? text.Substring(1).Split(' ')[0] : string.Empty;
                var matches = slashCommands
                    .Where(cmd => cmd.Command.StartsWith(commandPrefix, StringComparison.OrdinalIgnoreCase))
                    .Select(cmd => $"/{cmd.Command.PadRight(12)} {cmd.Description}")
                    .ToList();

                if (matches.Count == 0)
                {
                    matches.Add("No matches found");
                }

                var visibleRows = Math.Min(matches.Count, input.Autocomplete.MaxHeight);
                suggestionItems.Clear();
                foreach (var match in matches)
                {
                    suggestionItems.Add(match);
                }
                suggestionOverlayRows = visibleRows;
                suggestionOverlay.Height = visibleRows;
                suggestionOverlay.Visible = true;
                input.Autocomplete.Visible = false;
                ApplyLayout();
            };
            
            // Set HostControl on first iteration after app starts
            var hostControlSet = false;
            Application.Iteration += (_, _) =>
            {
                if (!hostControlSet)
                {
                    hostControlSet = true;
                    input.Autocomplete.HostControl = input;
                }
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
                UpdateLogStyle();
                history.MoveEnd();
            }

            void AppendHistoryOnUi(string text) {
                Application.Invoke(() => AppendHistory(text));
            }

            void ApplyLayout() {
                input.Height = inputHeight;
                inputPanel.Height = inputHeight + 1;
                inputPanel.Y = Pos.AnchorEnd(inputHeight + footerHeight + 1);
                infoLayer.Height = infoLayerHeight;
                infoLayer.Y = Pos.AnchorEnd(inputHeight + footerHeight + infoLayerHeight + 1);
                if (sessionStarted) {
                    history.Height = Dim.Fill(sessionHeaderHeight + stageHeight + infoLayerHeight + inputHeight + footerHeight + 3);
                    history.Y = Pos.Bottom(stageLabel);
                }
                if (suggestionOverlay.Visible) {
                    suggestionOverlay.X = 1;
                    suggestionOverlay.Width = Dim.Fill(12);
                    suggestionOverlay.Height = suggestionOverlayRows;
                    suggestionOverlay.Y = Pos.AnchorEnd(inputHeight + footerHeight + suggestionOverlayRows + 2);
                }
                footer.SetNeedsLayout();
                infoLayer.SetNeedsLayout();
                inputPanel.SetNeedsLayout();
                history.SetNeedsLayout();
                startView.SetNeedsLayout();
                sessionView.SetNeedsLayout();
                window.SetNeedsLayout();
                Application.LayoutAndDraw(forceDraw: false);
            }

            void SetStage(string stage) {
                currentStage = stage;
                Application.Invoke(() => { stageLabel.Text = $"Stage: {currentStage}"; });
            }

            void SwitchToSession() {
                if (sessionStarted) {
                    return;
                }

                sessionStarted = true;
                startView.Visible = false;
                sessionView.Visible = true;
                ApplyLayout();
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

                SwitchToSession();
                SetStage("Planning");

                AppendHistoryOnUi($"\nYou: {text}\nBuddy: ");

                turnCts = new CancellationTokenSource();

                try {
                    await agent.RunTurnAsync(
                        currentClient,
                        systemPrompt,
                        projectInstructions,
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
                        cancellationToken: turnCts.Token);
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
                        UpdateLogStyle();
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
                        Application.RequestStop(window);
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
                    if (input.Autocomplete.Visible) {
                        return;
                    }

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
            sessionView.Add(sessionHeader, stageLabel, history);
            window.Add(startView, sessionView, infoLayer, inputPanel, footer, suggestionOverlay);

            input.SetFocus();
            UpdateLogStyle();
            ApplyLayout();

            Application.Run(window);
        }
        finally {
            window.Dispose();
            turnCts?.Dispose();
            Application.Shutdown();
        }

        return 0;
    }
}
