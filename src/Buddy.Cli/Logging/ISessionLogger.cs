namespace Buddy.Cli.Logging;

internal interface ISessionLogger {
    MarkdownSessionLogger Create(string version, string modelName);
}
