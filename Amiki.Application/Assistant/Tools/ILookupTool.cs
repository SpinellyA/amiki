using System.Text.Json.Nodes;

namespace Amiki.Application.Assistant.Tools;

/// <summary>
/// Something Amiki can look up while chatting. Read-only by design. To add one: write a class
/// implementing this and register it in DependencyInjection; nothing else changes.
/// </summary>
public interface ILookupTool
{
    ToolDefinition Definition { get; }

    /// <exception cref="ToolException">The arguments don't make sense; the model is told why.</exception>
    Task<JsonNode> RunAsync(ToolArguments args, DateTime today, CancellationToken ct);
}
