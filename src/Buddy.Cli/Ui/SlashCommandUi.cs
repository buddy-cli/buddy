using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Terminal.Gui;

namespace Buddy.Cli.Ui;

internal sealed class SlashCommandUi {
    private readonly TextView _input;
    private readonly ListView _suggestionOverlay;
    private readonly ObservableCollection<string> _suggestionItems;
    private readonly List<SlashCommandOption> _commands;
    private readonly ChatSessionState _state;
    private readonly Action _applyLayout;
    private bool _hostControlSet;

    public SlashCommandUi(
        TextView input,
        ListView suggestionOverlay,
        ObservableCollection<string> suggestionItems,
        List<SlashCommandOption> commands,
        ChatSessionState state,
        Action applyLayout) {
        _input = input;
        _suggestionOverlay = suggestionOverlay;
        _suggestionItems = suggestionItems;
        _commands = commands;
        _state = state;
        _applyLayout = applyLayout;
    }

    public void Initialize() {
        _input.Autocomplete.SuggestionGenerator = new SlashCommandSuggestionGenerator(_commands);
        _input.Autocomplete.SelectionKey = KeyCode.Tab;
        _input.Autocomplete.PopupInsideContainer = false;
        _input.Autocomplete.MaxHeight = 8;

        _input.ContentsChanged += (_, _) => OnInputChanged();

        Application.Iteration += (_, _) => {
            if (_hostControlSet) {
                return;
            }

            _hostControlSet = true;
            _input.Autocomplete.HostControl = _input;
        };
    }

    private void OnInputChanged() {
        var text = _input.Text?.ToString() ?? string.Empty;

        if (string.IsNullOrEmpty(text) || !text.StartsWith("/")) {
            _suggestionOverlay.Visible = false;
            _suggestionItems.Clear();
            _state.SuggestionOverlayRows = 0;
            _input.Autocomplete.ClearSuggestions();
            _input.Autocomplete.Visible = false;
            _applyLayout();
            return;
        }

        var commandPrefix = text.Length > 1 ? text.Substring(1).Split(' ')[0] : string.Empty;
        var matches = _commands
            .Where(cmd => cmd.Command.StartsWith(commandPrefix, StringComparison.OrdinalIgnoreCase))
            .Select(cmd => $"/{cmd.Command.PadRight(12)} {cmd.Description}")
            .ToList();

        if (matches.Count == 0) {
            matches.Add("No matches found");
        }

        var visibleRows = Math.Min(matches.Count, _input.Autocomplete.MaxHeight);
        _suggestionItems.Clear();
        foreach (var match in matches) {
            _suggestionItems.Add(match);
        }
        _state.SuggestionOverlayRows = visibleRows;
        _suggestionOverlay.Height = visibleRows;
        _suggestionOverlay.Visible = true;
        _input.Autocomplete.Visible = false;
        _applyLayout();
    }
}