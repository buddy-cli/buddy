using Buddy.LLM;

namespace Buddy.Core.Application;

public interface ILLMClientFactory {
    ILLMClient Create(string model);
}
