using Amiki.Domain.Assistant;

namespace Amiki.Application.Assistant.Tools.Proposals;

/// <summary>Only offered to the owner: for everyone else, what she knows about them is the owner's to write.</summary>
public sealed class ProposeRemember : IProposalTool
{
    public const int MaxFact = 300;

    public ToolDefinition Definition { get; } = ToolDefinition.Create("propose_remember",
        "Suggest remembering a lasting fact about the user (added to 'About you' in your settings), e.g. their sem end date or a preference. Use when they ask you to remember something, or share something clearly worth keeping.",
        AssistantArea.Memory,
        """{"type":"object","properties":{"fact":{"type":"string","description":"One short sentence, written about the user, e.g. 'Sem ends Dec 12.'"}},"required":["fact"]}""");

    public Task<AssistantProposal> ProposeAsync(ToolArguments args, DateTime today, CancellationToken ct) =>
        Task.FromResult(new AssistantProposal(Guid.NewGuid(), ProposalKind.Remember, args.RequiredText("fact", MaxFact)));
}
