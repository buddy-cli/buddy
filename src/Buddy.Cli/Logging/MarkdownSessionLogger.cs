using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Buddy.LLM;

namespace Buddy.Cli.Logging;

internal sealed class MarkdownSessionLogger {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _sessionPath;
    private readonly object _sync = new();
    private int _callIndex;

    private MarkdownSessionLogger(string sessionPath) {
        _sessionPath = sessionPath;
    }

    public static MarkdownSessionLogger Create(string version, string modelName) {
        var startedAt = DateTimeOffset.Now;
        var sessionDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".buddy",
            "sessions");
        Directory.CreateDirectory(sessionDir);

        var fileName = $"{startedAt:yyyy-MM-dd-HH-mm-ss}-{Guid.NewGuid()}.md";
        var sessionPath = Path.Combine(sessionDir, fileName);
        var logger = new MarkdownSessionLogger(sessionPath);
        logger.WriteHeader(version, modelName, startedAt);
        return logger;
    }

    public SessionLogCall BeginCall(string modelName, IReadOnlyList<Message> messages, IReadOnlyList<ToolDefinition> tools) {
        var call = new SessionLogCall(Interlocked.Increment(ref _callIndex), DateTimeOffset.Now, modelName);
        var payload = new SessionLogRequest(messages, tools);
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var builder = new StringBuilder();
        builder.AppendLine($"## Call {call.Index} - {call.Timestamp:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine($"- Model: {call.ModelName}");
        builder.AppendLine();
        builder.AppendLine("### Request");
        builder.AppendLine("```json");
        builder.AppendLine(json);
        builder.AppendLine("```");
        builder.AppendLine();
        Append(builder.ToString());
        return call;
    }

    public void CompleteCall(SessionLogCall call, string responseText, IReadOnlyList<ToolCall> toolCalls) {
        var builder = new StringBuilder();
        builder.AppendLine("### Response");
        builder.AppendLine("```text");
        builder.AppendLine(responseText ?? string.Empty);
        builder.AppendLine("```");
        builder.AppendLine();

        if (toolCalls.Count > 0) {
            var json = JsonSerializer.Serialize(toolCalls, JsonOptions);
            builder.AppendLine("### Response Tool Calls");
            builder.AppendLine("```json");
            builder.AppendLine(json);
            builder.AppendLine("```");
            builder.AppendLine();
        }

        Append(builder.ToString());
    }

    public void FailCall(SessionLogCall call, string responseText, IReadOnlyList<ToolCall> toolCalls, Exception exception) {
        var builder = new StringBuilder();
        builder.AppendLine("### Response");
        builder.AppendLine("```text");
        builder.AppendLine(responseText ?? string.Empty);
        builder.AppendLine("```");
        builder.AppendLine();
        builder.AppendLine("### Error");
        builder.AppendLine("```text");
        builder.AppendLine(exception.ToString());
        builder.AppendLine("```");
        builder.AppendLine();

        if (toolCalls.Count > 0) {
            var json = JsonSerializer.Serialize(toolCalls, JsonOptions);
            builder.AppendLine("### Response Tool Calls");
            builder.AppendLine("```json");
            builder.AppendLine(json);
            builder.AppendLine("```");
            builder.AppendLine();
        }

        Append(builder.ToString());
    }

    private void WriteHeader(string version, string modelName, DateTimeOffset startedAt) {
        var builder = new StringBuilder();
        builder.AppendLine("# Buddy Session");
        builder.AppendLine($"- Version: {version}");
        builder.AppendLine($"- Model: {modelName}");
        builder.AppendLine($"- Started: {startedAt:yyyy-MM-dd HH:mm:ss zzz}");
        builder.AppendLine();
        Append(builder.ToString());
    }

    private void Append(string text) {
        try {
            lock (_sync) {
                File.AppendAllText(_sessionPath, text);
            }
        }
        catch {
            // Logging must not disrupt the main session.
        }
    }

    internal sealed record SessionLogCall(int Index, DateTimeOffset Timestamp, string ModelName);

    private sealed record SessionLogRequest(
        IReadOnlyList<Message> Messages,
        IReadOnlyList<ToolDefinition> Tools);
}
