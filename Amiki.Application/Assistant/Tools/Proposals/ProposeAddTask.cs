using Amiki.Domain;
using Amiki.Domain.Assistant;
using Amiki.Domain.Tasks;

namespace Amiki.Application.Assistant.Tools.Proposals;

public sealed class ProposeAddTask : IProposalTool
{
    public ToolDefinition Definition { get; } = ToolDefinition.Create("propose_add_task",
        "Suggest adding a task. The user confirms it in the app; nothing changes until then.",
        AssistantArea.Tasks, """
        {"type":"object","properties":{
          "title":{"type":"string"},
          "due":{"type":"string","description":"yyyy-MM-dd; omit for today"},
          "project":{"type":"string","description":"A project tag without #"},
          "priority":{"type":"string","enum":["low","medium","high"]}},
         "required":["title"]}
        """);

    public Task<AssistantProposal> ProposeAsync(ToolArguments args, DateTime today, CancellationToken ct) =>
        Task.FromResult(new AssistantProposal(
            Guid.NewGuid(), ProposalKind.AddTask,
            args.RequiredText("title", TaskItem.MaxTitle),
            Due: args.Date("due") ?? today,
            Project: Tags.Clean(args.Text("project")),
            Priority: Enum.TryParse<Priority>(args.Text("priority"), ignoreCase: true, out var p) ? p : Priority.None));
}
