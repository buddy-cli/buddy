using Buddy.LLM;

namespace Buddy.Core.Application;

public interface ILLMClientFactory {
    ILlmMClient Create(string model);
}
