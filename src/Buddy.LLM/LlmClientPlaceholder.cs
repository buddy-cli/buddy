using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http.Headers;

namespace Buddy.LLM;

public enum MessageRole {
    System,
    User,
    Assistant,
    Tool
}

public sealed record ToolCall(string Id, string Name, string ArgumentsJson);

public sealed record Message(
    MessageRole Role,
    string? Content,
    string? ToolCallId = null,
    IReadOnlyList<ToolCall>? ToolCalls = null);

public sealed record ToolDefinition(string Name, string Description, JsonElement ParameterSchema);

/// <summary>
/// A streamed tool-call delta.
///
/// Note: OpenAI-compatible streaming emits <c>function.arguments</c> as a JSON string fragment,
/// which may not be valid JSON until all chunks are concatenated.
/// </summary>
public sealed record ToolCallDelta(int Index, string Id, string Name, string ArgumentsJsonDelta);

public sealed record ChatResponseChunk(string? TextDelta, ToolCallDelta? ToolCall) {
    public static ChatResponseChunk FromText(string text) => new(text, null);
    public static ChatResponseChunk FromToolCall(ToolCallDelta toolCall) => new(null, toolCall);
}

public interface ILLMClient {
    IAsyncEnumerable<ChatResponseChunk> GetStreamingResponseAsync(
        IReadOnlyList<Message> messages,
        IReadOnlyList<ToolDefinition> tools,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// OpenAI-compatible client that uses the <c>/v1/chat/completions</c> streaming SSE API.
///
/// This intentionally uses raw HTTP for broad compatibility with OpenAI-style gateways.
/// </summary>
public sealed class OpenAiLlmClient : ILLMClient, IDisposable {
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

    // Minimal DTOs for request serialization.
    private sealed class ChatCompletionsRequest {
        [JsonPropertyName("model")]
        public string Model { get; init; } = string.Empty;

        [JsonPropertyName("stream")]
        public bool Stream { get; init; }

        [JsonPropertyName("messages")]
        public ApiMessage[] Messages { get; init; } = [];

        [JsonPropertyName("tools")]
        public ApiToolDefinition[]? Tools { get; init; }
    }

    private sealed class ApiMessage {
        [JsonPropertyName("role")]
        public string Role { get; init; } = string.Empty;

        [JsonPropertyName("content")]
        public string? Content { get; init; }

        [JsonPropertyName("tool_call_id")]
        public string? ToolCallId { get; init; }

        [JsonPropertyName("tool_calls")]
        public ApiToolCall[]? ToolCalls { get; init; }
    }

    private sealed class ApiToolCall {
        [JsonPropertyName("id")]
        public string Id { get; init; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; init; } = "function";

        [JsonPropertyName("function")]
        public ApiToolCallFunction Function { get; init; } = new();
    }

    private sealed class ApiToolCallFunction {
        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("arguments")]
        public string Arguments { get; init; } = string.Empty;
    }

    private sealed class ApiToolDefinition {
        [JsonPropertyName("type")]
        public string Type { get; init; } = "function";

        [JsonPropertyName("function")]
        public ApiToolFunction Function { get; init; } = new();
    }

    private sealed class ApiToolFunction {
        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; init; } = string.Empty;

        [JsonPropertyName("parameters")]
        public JsonElement Parameters { get; init; }
    }

    public void Dispose() {
        if (_disposeHttpClient) {
            _httpClient.Dispose();
        }
    }
}
