using System.Collections.ObjectModel;
using System.Reactive.Linq;
using Buddy.Cli.Ui;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace Buddy.Cli.ViewModels;

public partial class SlashCommandsViewModel : ReactiveObject {
    [Reactive]
    private bool _isActive;

    [Reactive]
    private string _filterText = string.Empty;

    [Reactive]
    private int _selectedIndex;

    public SlashCommandOption? SelectedCommand =>
        _selectedIndex >= 0 && _selectedIndex < _filteredCommands.Count
            ? _filteredCommands[_selectedIndex]
            : null;

    public ReadOnlyObservableCollection<SlashCommandOption> FilteredCommands { get; }

    private readonly ObservableCollection<SlashCommandOption> _filteredCommands = [];

    private readonly List<SlashCommandOption> _allCommands =
    [
        new() { Command = "clear", Description = "Clear chat history" },
        new() { Command = "model", Description = "Select AI model", ParameterHint = "<name>" },
        new() { Command = "provider", Description = "Configure provider" },
        new() { Command = "compact", Description = "Summarize conversation" },
        new() { Command = "exit", Description = "Exit Buddy" }
    ];

    public SlashCommandsViewModel() {
        FilteredCommands = new ReadOnlyObservableCollection<SlashCommandOption>(_filteredCommands);

        this.WhenAnyValue(x => x.FilterText)
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(UpdateFilteredCommands);
    }

    /// <summary>
    /// Returns the completed command text (e.g., "/exit ") if there's a valid selection.
    /// </summary>
    public string? GetCompletedText() {
        if (SelectedCommand is null) return null;
        return $"/{SelectedCommand.Command}";
    }

    private void UpdateFilteredCommands(string filter) {
        _filteredCommands.Clear();

        var matches = string.IsNullOrEmpty(filter)
            ? _allCommands
            : _allCommands.Where(cmd =>
                cmd.Command.StartsWith(filter, StringComparison.OrdinalIgnoreCase));

        foreach (var cmd in matches) {
            _filteredCommands.Add(cmd);
        }

        SelectedIndex = 0;
    }
}
