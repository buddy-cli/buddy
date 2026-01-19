using Buddy.Core.Configuration;
using Buddy.LLM;

namespace Buddy.Cli.Services;

/// <summary>
/// Provides access to the current LLM client and manages client lifecycle during model switches.
/// </summary>
public interface ILlmClientProvider {
    /// <summary>
    /// Gets the current LLM client (with logging wrapper applied).
    /// </summary>
    ILlmMClient Current { get; }

    /// <summary>
    /// Gets the current model's system name.
    /// </summary>
    string CurrentModelName { get; }

    /// <summary>
    /// Switches to a new model, disposing the old client and creating a new wrapped client.
    /// </summary>
    void SetModel(LlmProviderConfig provider, LlmModelConfig model);
}
