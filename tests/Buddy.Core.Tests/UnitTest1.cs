using Buddy.Core.Configuration;

namespace Buddy.Core.Tests;

public sealed class BuddyOptionsLoaderTests {
    [Fact]
    public void Loads_from_environment_variables() {
        var original = Environment.GetEnvironmentVariable("BUDDY_MODEL");
        try {
            Environment.SetEnvironmentVariable("BUDDY_MODEL", "env-model");

            var opts = BuddyOptionsLoader.Load(Directory.GetCurrentDirectory(), Array.Empty<string>());
            Assert.Equal("env-model", opts.Model);
        }
        finally {
            Environment.SetEnvironmentVariable("BUDDY_MODEL", original);
        }
    }

    [Fact]
    public void Command_line_overrides_environment() {
        var original = Environment.GetEnvironmentVariable("BUDDY_MODEL");
        try {
            Environment.SetEnvironmentVariable("BUDDY_MODEL", "env-model");

            var opts = BuddyOptionsLoader.Load(
                Directory.GetCurrentDirectory(),
                new[] { "--model", "cli-model" });

            Assert.Equal("cli-model", opts.Model);
        }
        finally {
            Environment.SetEnvironmentVariable("BUDDY_MODEL", original);
        }
    }
}
