using Amiki.Domain.Assistant;

namespace Amiki.Application.Assistant.Tools;

/// <summary>
/// A change Amiki can suggest. It never writes anything: it checks the suggestion against your
/// real data and returns it as a card, which only your "Do it" makes happen. To add one: write a
/// class implementing this and register it in DependencyInjection.
/// </summary>
public interface IProposalTool
{
    /// <summary>Its name starts with <see cref="ToolDefinition.ProposalPrefix"/>.</summary>
    ToolDefinition Definition { get; }

    /// <exception cref="ToolException">The suggestion doesn't make sense; the model is told why.</exception>
    Task<AssistantProposal> ProposeAsync(ToolArguments args, DateTime today, CancellationToken ct);
}
