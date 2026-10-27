using Amiki.Domain;
using Amiki.Domain.Assistant;
using Amiki.Domain.Ideas;

namespace Amiki.Application.Assistant.Tools.Proposals;

public sealed class ProposeAddIdea : IProposalTool
{
    public ToolDefinition Definition { get; } = ToolDefinition.Create("propose_add_idea",
        "Suggest saving an idea for later.",
        AssistantArea.Ideas, """
        {"type":"object","properties":{
          "text":{"type":"string"},
          "tags":{"type":"array","items":{"type":"string"},"description":"Tags without #"}},
         "required":["text"]}
        """);

    public Task<AssistantProposal> ProposeAsync(ToolArguments args, DateTime today, CancellationToken ct) =>
        Task.FromResult(new AssistantProposal(Guid.NewGuid(), ProposalKind.AddIdea,
            args.RequiredText("text", Idea.MaxText),
            Tags: args.TextList("tags").Select(Tags.Clean).OfType<string>().Distinct().ToList()));
}
