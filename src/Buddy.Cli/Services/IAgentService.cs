namespace Buddy.Cli.Services;

public interface IAgentService {
    Task RunTurnAsync(
        string userInput,
        Func<string, Task> onTextDelta,
        Func<string, Task> onToolStatus,
        CancellationToken cancellationToken);

    void ClearHistory();
}
