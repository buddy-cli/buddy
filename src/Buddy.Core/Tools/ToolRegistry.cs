using System.Text.Json;
using Buddy.LLM;

namespace Buddy.Core.Tools;

public sealed class ToolRegistry
{
    private readonly IReadOnlyList<ITool> _tools;
    private readonly Dictionary<string, ITool> _byName;

    public ToolRegistry(IEnumerable<ITool> tools)
    {
        _tools = tools.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase).ToArray();
        _byName = _tools.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<ITool> Tools => _tools;

    public IReadOnlyList<ToolDefinition> GetToolDefinitions()
        => _tools
            .Select(t => new ToolDefinition(t.Name, t.Description, t.ParameterSchema))
            .ToArray();

    public bool TryGet(string name, out ITool tool)
        => _byName.TryGetValue(name, out tool!);

    public async Task<string> ExecuteAsync(string name, string argumentsJson, CancellationToken cancellationToken)
    {
        if (!TryGet(name, out var tool))
        {
            return $"error: unknown tool '{name}'";
        }

        JsonElement args;
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(argumentsJson) ? "{}" : argumentsJson);
            args = doc.RootElement.Clone();
        }
        catch (Exception ex)
        {
            return $"error: invalid tool arguments JSON ({ex.Message})\nraw: {argumentsJson}";
        }

        return await tool.ExecuteAsync(args, cancellationToken);
    }
}
