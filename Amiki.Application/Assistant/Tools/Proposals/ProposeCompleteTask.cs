using Amiki.Application.Data;
using Amiki.Domain.Assistant;
using Amiki.Domain.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Amiki.Application.Assistant.Tools.Proposals;

/// <summary>Finds the open task meant. Several close matches go back to her, so she asks you which one.</summary>
public sealed class ProposeCompleteTask(IAmikiDb db) : IProposalTool
{
    private const int MaxListed = 10;

    public ToolDefinition Definition { get; } = ToolDefinition.Create("propose_complete_task",
        "Suggest marking one of the user's open tasks as done. Name it by words from its title.",
        AssistantArea.Tasks,
        """{"type":"object","properties":{"task":{"type":"string","description":"Words from the task's title"}},"required":["task"]}""");

    public async Task<AssistantProposal> ProposeAsync(ToolArguments args, DateTime today, CancellationToken ct)
    {
        var words = args.RequiredText("task", TaskItem.MaxTitle);
        var open = await db.Tasks.AsNoTracking().Where(t => t.CompletedAt == null).Select(t => new { t.Id, t.Title }).ToListAsync(ct);
        var exact = open.Where(t => string.Equals(t.Title, words, StringComparison.OrdinalIgnoreCase)).ToList();
        var matches = exact.Count > 0 ? exact : open.Where(t => t.Title.Contains(words, StringComparison.OrdinalIgnoreCase)).ToList();

        return matches.Count switch
        {
            1 => new AssistantProposal(Guid.NewGuid(), ProposalKind.CompleteTask, matches[0].Title, TaskId: matches[0].Id),
            0 => throw new ToolException($"No open task matches '{words}'. Open tasks include: {string.Join("; ", open.Take(MaxListed).Select(t => t.Title))}."),
            _ => throw new ToolException($"Several open tasks match '{words}': {string.Join("; ", matches.Take(MaxListed).Select(t => t.Title))}. Ask the user which one."),
        };
    }
}
