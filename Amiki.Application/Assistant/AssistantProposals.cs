using System.Text.Json.Nodes;
using Amiki.Application.Assistant.Tools;
using Amiki.Domain.Assistant;

namespace Amiki.Application.Assistant;

/// <summary>
/// The changes Amiki can suggest in chat (every registered <see cref="IProposalTool"/>). They
/// never write anything: each comes back as a card, and only your "Do it" makes it happen.
/// </summary>
public sealed class AssistantProposals(IEnumerable<IProposalTool> tools)
{
    private readonly IReadOnlyList<IProposalTool> _tools = [.. tools];

    /// <summary>
    /// The suggestions allowed by the settings: only if suggesting is on, only for areas she can
    /// see, and "remember this" only when <paramref name="canRemember"/> (the owner).
    /// </summary>
    public IReadOnlyList<ToolDefinition> For(AssistantSettings settings, bool canRemember = true) =>
        !settings.CanPropose
            ? []
            : [.. _tools.Select(t => t.Definition).Where(d => settings.CanSee(d.Area) && (d.Area != AssistantArea.Memory || canRemember))];

    public Task<AssistantProposal> ProposeAsync(ToolCall call, DateTime today, CancellationToken ct)
    {
        var tool = _tools.FirstOrDefault(t => t.Definition.Name == call.Name) ?? throw new ToolException($"Unknown tool {call.Name}.");
        return tool.ProposeAsync(new ToolArguments(call.Args), today, ct);
    }

    /// <summary>What the model is told after suggesting: it's a card now, not a done deal.</summary>
    public static JsonObject Result(AssistantProposal proposal) => new()
    {
        ["proposed"] = proposal.Describe(),
        ["status"] = "Shown to the user as a card. It only happens if they tap 'Do it'; don't say it's done.",
    };
}
