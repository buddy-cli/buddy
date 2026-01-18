namespace Buddy.Cli.ViewModels;

public enum LogEntryType {
    User,
    AssistantText,
    ToolStatus
}

public sealed class LogEntry {
    public LogEntryType Type { get; }
    public string Content { get; private set; }
    public DateTime Timestamp { get; }

    public LogEntry(LogEntryType type, string content) {
        Type = type;
        Content = content;
        Timestamp = DateTime.Now;
    }

    public void AppendContent(string text) {
        Content += text;
    }
}
