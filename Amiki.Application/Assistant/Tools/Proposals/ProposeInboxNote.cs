using Amiki.Domain.Assistant;
using Amiki.Domain.Inbox;

namespace Amiki.Application.Assistant.Tools.Proposals;

public sealed class ProposeInboxNote : IProposalTool
{
    public ToolDefinition Definition { get; } = ToolDefinition.Create("propose_inbox_note",
        "Suggest jotting something into the Inbox to sort later (when it's unclear what it is).",
        AssistantArea.Inbox,
        """{"type":"object","properties":{"text":{"type":"string"}},"required":["text"]}""");

    public Task<AssistantProposal> ProposeAsync(ToolArguments args, DateTime today, CancellationToken ct) =>
        Task.FromResult(new AssistantProposal(Guid.NewGuid(), ProposalKind.AddInboxNote, args.RequiredText("text", InboxItem.MaxText)));
}
