using System.Collections.ObjectModel;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace Buddy.Cli.ViewModels;

public partial class AgentLogViewModel : ReactiveObject {
    [Reactive]
    private bool _isProcessing;

    public ObservableCollection<LogEntry> Entries { get; } = [];

    private LogEntry? _currentAssistantEntry;

    public void AddUserMessage(string message) {
        Entries.Add(new LogEntry(LogEntryType.User, message));
        _currentAssistantEntry = null;
    }

    public void AppendAssistantText(string text) {
        if (_currentAssistantEntry is null || _currentAssistantEntry.Type != LogEntryType.AssistantText) {
            _currentAssistantEntry = new LogEntry(LogEntryType.AssistantText, text);
            Entries.Add(_currentAssistantEntry);
        } else {
            _currentAssistantEntry.AppendContent(text);
        }
        
        // Notify that the collection changed (for UI refresh of streaming text)
        this.RaisePropertyChanged(nameof(Entries));
    }

    public void AddToolStatus(string status) {
        Entries.Add(new LogEntry(LogEntryType.ToolStatus, status));
        _currentAssistantEntry = null;
    }

    public void Clear() {
        Entries.Clear();
        _currentAssistantEntry = null;
    }
}
