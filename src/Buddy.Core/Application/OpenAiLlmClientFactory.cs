using Buddy.Core.Configuration;
using Buddy.LLM;

namespace Buddy.Core.Application;

public sealed class OpenAiLlmClientFactory : ILLMClientFactory {
    private readonly BuddyOptions _options;

    public OpenAiLlmClientFactory(BuddyOptions options) {
        _options = options;
    }

    public ILlmMClient Create(string model) => new OpenAiLlmMClient(_options.ApiKey, model, _options.BaseUrl);
}
