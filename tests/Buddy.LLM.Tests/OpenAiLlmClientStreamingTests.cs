using System.Text;
using System.Text.Json;
using Buddy.LLM;

namespace Buddy.LLM.Tests;

public sealed class OpenAiLlmClientStreamingTests {
    [Fact]
    public async Task Streams_content_deltas_in_order() {
        var sse = string.Join("\n", new[]
        {
            "data: {\"choices\":[{\"delta\":{\"content\":\"Hel\"}}]}",
            "data: {\"choices\":[{\"delta\":{\"content\":\"lo\"}}]}",
            "data: [DONE]",
            ""
        });

        using var http = new HttpClient(new FakeSseHandler(sse));
        var client = new OpenAiLlmClient(http, apiKey: "test", model: "test-model", baseUrl: "https://example.com/v1");

        var chunks = new List<ChatResponseChunk>();
        await foreach (var chunk in client.GetStreamingResponseAsync(
                           new[] { new Message(MessageRole.User, "hello") },
                           Array.Empty<ToolDefinition>())) {
            chunks.Add(chunk);
        }

        var text = string.Concat(chunks.Where(c => c.TextDelta is not null).Select(c => c.TextDelta));
        Assert.Equal("Hello", text);
    }

    [Fact]
    public async Task Streams_tool_call_argument_fragments() {
        static string DataLine(object obj) => "data: " + JsonSerializer.Serialize(obj);

        var sse = string.Join("\n", new[]
        {
            DataLine(new
            {
                choices = new[]
                {
                    new
                    {
                        delta = new
                        {
                            tool_calls = new[]
                            {
                                new
                                {
                                    id = "call_1",
                                    type = "function",
                                    function = new
                                    {
                                        name = "read_file",
                                        arguments = "{\"path\":\"a\""
                                    }
                                }
                            }
                        }
                    }
                }
            }),
            DataLine(new
            {
                choices = new[]
                {
                    new
                    {
                        delta = new
                        {
                            tool_calls = new[]
                            {
                                new
                                {
                                    id = "call_1",
                                    type = "function",
                                    function = new
                                    {
                                        arguments = "}"
                                    }
                                }
                            }
                        }
                    }
                }
            }),
            "data: [DONE]",
            ""
        });

        using var http = new HttpClient(new FakeSseHandler(sse));
        var client = new OpenAiLlmClient(http, apiKey: "test", model: "test-model", baseUrl: "https://example.com/v1");

        var toolDeltas = new List<ToolCallDelta>();
        await foreach (var chunk in client.GetStreamingResponseAsync(
                           new[] { new Message(MessageRole.User, "do it") },
                           Array.Empty<ToolDefinition>())) {
            if (chunk.ToolCall is not null) {
                toolDeltas.Add(chunk.ToolCall);
            }
        }

        Assert.NotEmpty(toolDeltas);
        Assert.Equal("call_1", toolDeltas[0].Id);
        Assert.Equal("read_file", toolDeltas[0].Name);

        var args = string.Concat(toolDeltas.Select(t => t.ArgumentsJsonDelta));
        Assert.Equal("{\"path\":\"a\"}", args);
    }

}
