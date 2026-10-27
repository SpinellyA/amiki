using System.Text.Json.Nodes;
using Amiki.Application.Assistant.Tools;
using Amiki.Domain.Assistant;

namespace Amiki.Application.Assistant;

/// <summary>
/// Everything Amiki can look up while chatting (every registered <see cref="ILookupTool"/>).
/// All read-only, all capped in size, each offered only if its area is on in her settings.
/// There is no diary tool, by design.
/// </summary>
public sealed class AssistantTools(IEnumerable<ILookupTool> tools)
{
    private readonly IReadOnlyList<ILookupTool> _tools = [.. tools];

    /// <summary>The lookups for the areas she's allowed to see.</summary>
    public IReadOnlyList<ToolDefinition> For(AssistantSettings settings) =>
        [.. _tools.Select(t => t.Definition).Where(d => settings.CanSee(d.Area))];

    /// <summary>Runs a lookup the model asked for. Only tools in <paramref name="allowed"/> run.</summary>
    public Task<JsonNode> RunAsync(ToolCall call, IReadOnlyList<ToolDefinition> allowed, DateTime today, CancellationToken ct)
    {
        if (allowed.All(t => t.Name != call.Name)) throw new ToolException($"There's no tool called {call.Name} available.");
        var tool = _tools.FirstOrDefault(t => t.Definition.Name == call.Name) ?? throw new ToolException($"Unknown tool {call.Name}.");
        return tool.RunAsync(new ToolArguments(call.Args), today, ct);
    }
}
