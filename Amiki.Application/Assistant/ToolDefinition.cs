using System.Text.Json.Nodes;
using Amiki.Domain.Assistant;

namespace Amiki.Application.Assistant;

/// <summary>A tool Amiki can use, described so the model knows when and how to ask for it.</summary>
/// <param name="Parameters">OpenAPI-style object schema for the arguments, or null if it takes none.</param>
/// <param name="Area">What it reads or changes; she can only use it if that area is on in her settings.</param>
public sealed record ToolDefinition(string Name, string Description, JsonObject? Parameters, AssistantArea Area)
{
    /// <summary>Suggestion tools start with this; their result is a card, not a change.</summary>
    public const string ProposalPrefix = "propose_";

    /// <param name="schema">The arguments' JSON schema as text, or null for none.</param>
    public static ToolDefinition Create(string name, string description, AssistantArea area, string? schema = null) =>
        new(name, description, schema is null ? null : (JsonObject)JsonNode.Parse(schema)!, area);

    public bool IsProposal => Name.StartsWith(ProposalPrefix, StringComparison.Ordinal);

    /// <summary>How the area reads under her reply and in her instructions ("tasks", "weekly notes"…).</summary>
    public string AreaName => Area switch
    {
        AssistantArea.WeeklyNotes => "weekly notes",
        _ => Area.ToString().ToLowerInvariant(),
    };
}
