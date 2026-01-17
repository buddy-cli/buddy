using Buddy.Core.Configuration;

namespace Buddy.Core.Tests;

public sealed class BuddyOptionsLoaderTests {
    [Fact]
    public void Picks_first_provider_model() {
        var configDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(configDirectory);
        var configPath = Path.Combine(configDirectory, "config.json");

        try {
            var config = new BuddyConfigFile {
                Providers = new List<LlmProviderConfig> {
                    new() {
                        Name = "GitHub Models",
                        BaseUrl = "https://models.github.ai/inference",
                        ApiKey = "key_here",
                        Models = new List<LlmModelConfig> {
                            new() {
                                Name = "GPT-5-Mini",
                                System = "openai/gpt-5-mini"
                            }
                        }
                    }
                }
            };

            var json = System.Text.Json.JsonSerializer.Serialize(
                config,
                new System.Text.Json.JsonSerializerOptions {
                    WriteIndented = true
                });

            File.WriteAllText(configPath, json);

            var opts = BuddyOptionsLoader.Load(Directory.GetCurrentDirectory(), Array.Empty<string>(), configPath);

            Assert.Equal("https://models.github.ai/inference", opts.BaseUrl);
            Assert.Equal("key_here", opts.ApiKey);
            Assert.Equal("openai/gpt-5-mini", opts.Model);
        }
        finally {
            if (Directory.Exists(configDirectory)) {
                Directory.Delete(configDirectory, recursive: true);
            }
        }
    }
}
