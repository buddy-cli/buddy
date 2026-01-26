using Buddy.Cli.Logging;
using Buddy.Core.Configuration;
using Buddy.LLM;

namespace Buddy.Cli.Services;

/// <summary>
/// Manages the current LLM client, wrapping it with logging and handling model switches.
/// </summary>
internal sealed class LlmClientProvider : ILlmClientProvider, IDisposable {
    private readonly ISessionLogger _sessionLogger;
    private readonly string _version;
    private ILlmMClient? _currentClient;
    private MarkdownSessionLogger? _currentSessionLogger;

    public LlmClientProvider(
        BuddyOptions options,
        ISessionLogger sessionLogger,
        string version) {
        _sessionLogger = sessionLogger;
        _version = version;

        // Find initial provider/model from options
        var provider = options.Providers
            .FirstOrDefault(p => p.Models.Any(m => m.System == options.Model));

        if (provider is not null) {
            var model = provider.Models.First(m => m.System == options.Model);
            InitializeClient(provider, model);
        }
        else if (!string.IsNullOrEmpty(options.Model)) {
            // Fallback: use options directly (legacy single-provider config)
            var fallbackProvider = new LlmProviderConfig {
                ApiKey = options.ApiKey,
                BaseUrl = options.BaseUrl ?? string.Empty
            };
            var fallbackModel = new LlmModelConfig { System = options.Model };
            InitializeClient(fallbackProvider, fallbackModel);
        }
    }

    public ILlmMClient Current => _currentClient
        ?? throw new InvalidOperationException("No model has been configured. Call SetModel first.");

    public string CurrentModelName { get; private set; } = string.Empty;

    public void SetModel(LlmProviderConfig provider, LlmModelConfig model) {
        // Dispose old client if it exists
        if (_currentClient is IDisposable disposable) {
            disposable.Dispose();
        }

        InitializeClient(provider, model);
    }

    private void InitializeClient(LlmProviderConfig provider, LlmModelConfig model) {
        CurrentModelName = model.System;

        // Create session logger for this session (or reuse existing)
        _currentSessionLogger ??= _sessionLogger.Create(_version, CurrentModelName);

        // Create raw client
        var rawClient = new OpenAiLlmMClient(provider.ApiKey, model.System, provider.BaseUrl);

        // Wrap with logging - use func to always get current model name
        _currentClient = new LoggingLlmMClient(rawClient, _currentSessionLogger, () => CurrentModelName);
    }

    public void Dispose() {
        if (_currentClient is IDisposable disposable) {
            disposable.Dispose();
        }
    }
}
