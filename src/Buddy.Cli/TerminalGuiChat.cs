using System.Collections.ObjectModel;
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
        var state = new ChatSessionState(llmClient, inputHeight: 3, currentStage: "Idle");
        var metrics = new ChatLayoutMetrics(
            SessionHeaderHeight: 2,
            StageHeight: 1,
            InfoLayerHeight: 1,
            FooterHeight: 2);

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
        var suggestionItems = new ObservableCollection<string>();
        var bannerText = TerminalGuiLayout.BannerText;
        var bannerLines = TerminalGuiLayout.BannerLines;
        var layoutParts = TerminalGuiLayout.Build(
            options,
            version,
            bannerText,
            bannerLines,
            metrics,
            state.CurrentStage,
            state.InputHeight,
            suggestionItems);
        using var cancellationRegistration = cancellationToken.Register(() => Application.RequestStop(layoutParts.Window));
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
            var layoutManager = new ChatLayoutManager(metrics);
            var controller = new ChatController(
                agent,
                llmClientFactory,
                options,
                systemPrompt,
                projectInstructions,
                state,
                layoutParts,
                layoutManager,
                idleLogScheme,
                activeLogScheme);

            var slashCommandUi = new SlashCommandUi(
                layoutParts.Input,
                layoutParts.SuggestionOverlay,
                suggestionItems,
                slashCommands,
                state,
                () => layoutManager.ApplyLayout(state, layoutParts));
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
                    state.InputHeight = Math.Min(10, state.InputHeight + 1);
                    layoutManager.ApplyLayout(state, layoutParts);
                    return;
                }

                if (key.IsAlt && key.KeyCode == KeyCode.CursorDown) {
                    key.Handled = true;
                    state.InputHeight = Math.Max(2, state.InputHeight - 1);
                    layoutManager.ApplyLayout(state, layoutParts);
                    return;
                }

                if (key.IsCtrl && key.KeyCode == KeyCode.Enter) {
                    key.Handled = true;
                    _ = controller.SendAsync();
                }
            };

            layoutParts.Input.SetFocus();
            controller.UpdateLogStyle();
            layoutManager.ApplyLayout(state, layoutParts);

            Application.Run(layoutParts.Window);
        }
        finally {
            layoutParts.Window.Dispose();
            state.TurnCts?.Dispose();
            Application.Shutdown();
        }

        return 0;
    }
}
