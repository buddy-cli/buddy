using Buddy.Core.Configuration;

namespace Buddy.Core.Application;

public interface ISystemPromptBuilder {
    string Build(BuddyOptions options, DateTimeOffset now, string osEnvironment);
}
