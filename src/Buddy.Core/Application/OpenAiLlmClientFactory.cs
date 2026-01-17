using Buddy.Core.Configuration;
using Buddy.LLM;

namespace Buddy.Core.Application;

public sealed class OpenAiLlmClientFactory : ILLMClientFactory {
    private readonly BuddyOptions _options;

    public OpenAiLlmClientFactory(BuddyOptions options) {
        _options = options;
    }

    public ILLMClient Create(string model) => new OpenAiLlmClient(_options.ApiKey, model, _options.BaseUrl);
}
