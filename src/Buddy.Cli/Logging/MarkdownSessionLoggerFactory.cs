namespace Buddy.Cli.Logging;

internal sealed class MarkdownSessionLoggerFactory : ISessionLogger {
    public MarkdownSessionLogger Create(string version, string modelName) => MarkdownSessionLogger.Create(version, modelName);
}
