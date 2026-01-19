namespace Buddy.Cli.ViewModels;

public enum LogEntryType {
    User,
    AssistantText,
    ToolStatus
}

public sealed class LogEntry(LogEntryType type, string content) {
    public LogEntryType Type { get; } = type;
    public string Content { get; private set; } = content;
    public DateTime Timestamp { get; } = DateTime.Now;

    public void AppendContent(string text) {
        Content += text;
    }
}
