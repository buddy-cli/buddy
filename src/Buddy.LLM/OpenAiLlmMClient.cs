using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Buddy.LLM.Api;

namespace Buddy.LLM;

/// <summary>
/// OpenAI-compatible client that uses the <c>/v1/chat/completions</c> streaming SSE API.
///
/// This intentionally uses raw HTTP for broad compatibility with OpenAI-style gateways.
/// </summary>
public sealed class OpenAiLlmClient : ILlmClient, IDisposable {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly string _apiKey;
    private readonly string _model;
    private readonly Uri _baseUri;
    private readonly HttpClient _httpClient;
    private readonly bool _disposeHttpClient;

    public OpenAiLlmClient(string apiKey, string model, string? baseUrl)
        : this(new HttpClient(), apiKey, model, baseUrl, disposeHttpClient: true) {
    }

    public OpenAiLlmClient(HttpClient httpClient, string apiKey, string model, string? baseUrl)
        : this(httpClient, apiKey, model, baseUrl, disposeHttpClient: false) {
    }

    private OpenAiLlmClient(HttpClient httpClient, string apiKey, string model, string? baseUrl, bool disposeHttpClient) {
        _apiKey = apiKey;
        _model = model;
        _baseUri = NormalizeBaseUri(baseUrl);
        _httpClient = httpClient;
        _disposeHttpClient = disposeHttpClient;
    }

    public async IAsyncEnumerable<ChatResponseChunk> GetStreamingResponseAsync(
        IReadOnlyList<Message> messages,
        IReadOnlyList<ToolDefinition> tools,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default) {
        if (messages is null) throw new ArgumentNullException(nameof(messages));
        if (tools is null) throw new ArgumentNullException(nameof(tools));

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_baseUri, "chat/completions"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        if (!string.IsNullOrWhiteSpace(_apiKey)) {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        var payload = new ChatCompletionsRequest {
            Model = _model,
            Stream = true,
            Messages = messages.Select(ToApiMessage).ToArray(),
            Tools = tools.Count > 0 ? tools.Select(ToApiTool).ToArray() : null
        };

        request.Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions));
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!response.IsSuccessStatusCode) {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"OpenAI-compatible request failed: {(int)response.StatusCode} {response.ReasonPhrase}\n{body}");
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(responseStream);

        while (true) {
            cancellationToken.ThrowIfCancellationRequested();

            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null) break;

            if (line.Length == 0) continue;
            if (line.StartsWith(':')) continue; // SSE comment/keepalive
            if (!line.StartsWith("data:", StringComparison.Ordinal)) continue;

            var data = line[5..].Trim();
            if (data == "[DONE]") yield break;
            if (data.Length == 0) continue;

            foreach (var chunk in ParseChunkData(data)) {
                yield return chunk;
            }
        }
    }

    private IEnumerable<ChatResponseChunk> ParseChunkData(string json) {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("choices", out var choices) || choices.ValueKind != JsonValueKind.Array || choices.GetArrayLength() == 0) {
            yield break;
        }

        // For now, only stream the first choice.
        var choice0 = choices[0];
        if (!choice0.TryGetProperty("delta", out var delta) || delta.ValueKind != JsonValueKind.Object) {
            yield break;
        }

        if (delta.TryGetProperty("content", out var contentEl) && contentEl.ValueKind == JsonValueKind.String) {
            var content = contentEl.GetString();
            if (!string.IsNullOrEmpty(content)) {
                yield return ChatResponseChunk.FromText(content);
            }
        }

        if (delta.TryGetProperty("tool_calls", out var toolCallsEl) && toolCallsEl.ValueKind == JsonValueKind.Array) {
            foreach (var toolCall in toolCallsEl.EnumerateArray()) {
                var index = toolCall.TryGetProperty("index", out var idxEl) && idxEl.ValueKind == JsonValueKind.Number
                    ? idxEl.GetInt32()
                    : 0;

                var id = toolCall.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.String
                    ? idEl.GetString() ?? string.Empty
                    : string.Empty;

                string name = string.Empty;
                string argsDelta = string.Empty;

                if (toolCall.TryGetProperty("function", out var fn) && fn.ValueKind == JsonValueKind.Object) {
                    if (fn.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String) {
                        name = nameEl.GetString() ?? string.Empty;
                    }

                    if (fn.TryGetProperty("arguments", out var argsEl) && argsEl.ValueKind == JsonValueKind.String) {
                        argsDelta = argsEl.GetString() ?? string.Empty;
                    }
                }

                if (!string.IsNullOrEmpty(name) || !string.IsNullOrEmpty(argsDelta) || !string.IsNullOrEmpty(id)) {
                    yield return ChatResponseChunk.FromToolCall(new ToolCallDelta(index, id, name, argsDelta));
                }
            }
        }
    }

    private static Uri NormalizeBaseUri(string? baseUrl) {
        // Default to OpenAI's v1.
        var raw = string.IsNullOrWhiteSpace(baseUrl) ? "https://api.openai.com/v1/" : baseUrl;

        if (!raw.EndsWith("/", StringComparison.Ordinal)) raw += "/";

        // Common user input: https://api.openai.com (no /v1)
        if (Uri.TryCreate(raw, UriKind.Absolute, out var uri) && !uri.AbsolutePath.TrimEnd('/').EndsWith("v1", StringComparison.OrdinalIgnoreCase)) {
            // If path is empty or just '/', append v1/
            if (uri.AbsolutePath == "/") {
                return new Uri(uri, "v1/");
            }
        }

        if (!Uri.TryCreate(raw, UriKind.Absolute, out uri)) {
            throw new ArgumentException($"Invalid baseUrl: '{baseUrl}'", nameof(baseUrl));
        }

        return uri;
    }

    private static ApiMessage ToApiMessage(Message message) {
        var role = message.Role switch {
            MessageRole.System => "system",
            MessageRole.User => "user",
            MessageRole.Assistant => "assistant",
            MessageRole.Tool => "tool",
            _ => "user"
        };

        return new ApiMessage {
            Role = role,
            Content = message.Content,
            ToolCallId = message.ToolCallId,
            ToolCalls = message.ToolCalls?.Select(tc => new ApiToolCall {
                Id = tc.Id,
                Type = "function",
                Function = new ApiToolCallFunction {
                    Name = tc.Name,
                    Arguments = tc.ArgumentsJson
                }
            }).ToArray()
        };
    }

    private static ApiToolDefinition ToApiTool(ToolDefinition tool)
        => new() {
            Type = "function",
            Function = new ApiToolFunction {
                Name = tool.Name,
                Description = tool.Description,
                Parameters = tool.ParameterSchema
            }
        };

    public void Dispose() {
        if (_disposeHttpClient) {
            _httpClient.Dispose();
        }
    }
}
