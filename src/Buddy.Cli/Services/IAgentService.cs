using Buddy.Core.Configuration;

namespace Buddy.Cli.Services;

public interface IAgentService {
    Task RunTurnAsync(
        string userInput,
        Func<string, Task> onTextDelta,
        Func<string, Task> onToolStatus,
        CancellationToken cancellationToken);

    void ClearHistory();

    /// <summary>
    /// Changes the active model used for subsequent turns.
    /// </summary>
    void ChangeModel(LlmProviderConfig provider, LlmModelConfig model);
}
