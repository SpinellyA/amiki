using Amiki.Application.Assistant;
using Amiki.Infrastructure.Assistant;
using Amiki.Modules.Assistant;
using Amiki.Modules.Finance;

namespace Amiki.Tests.Assistant;

public class ProposalTests
{
    private static readonly DateTime Today = new(2026, 10, 21);

    [Fact]
    public void Suggesting_follows_the_switches()
    {
        var proposals = new AssistantProposals(null!);

        Assert.Equal(["propose_add_task", "propose_complete_task", "propose_log_money", "propose_add_idea", "propose_inbox_note", "propose_remember"],
            proposals.For(new AssistantSettings()).Select(t => t.Name));
        Assert.Empty(proposals.For(new AssistantSettings { CanPropose = false }));
        Assert.DoesNotContain(proposals.For(new AssistantSettings { SeeMoney = false }), t => t.Area == "money");
    }

    [Fact]
    public async Task A_task_suggestion_defaults_to_today_and_tidies_the_project()
    {
        var proposals = new AssistantProposals(null!);

        var p = await proposals.ProposeAsync(new ToolCall("propose_add_task",
            new() { ["title"] = "Quiz ch 5-6", ["project"] = "#CMSC125", ["priority"] = "high" }), Today, CancellationToken.None);

        Assert.Equal((ProposalKind.AddTask, Today, "cmsc125"), (p.Kind, p.Due!.Value, p.Project));
        Assert.Equal(ProposalStatus.Waiting, p.Status);
    }

    [Fact]
    public async Task Bad_arguments_go_back_to_her_as_tool_errors()
    {
        var proposals = new AssistantProposals(null!);

        await Assert.ThrowsAsync<ToolException>(() => proposals.ProposeAsync(new ToolCall("propose_add_task", []), Today, CancellationToken.None));
        await Assert.ThrowsAsync<ToolException>(() => proposals.ProposeAsync(
            new ToolCall("propose_add_task", new() { ["title"] = "x", ["due"] = "next friday" }), Today, CancellationToken.None));
        await Assert.ThrowsAsync<ToolException>(() => proposals.ProposeAsync(
            new ToolCall("propose_log_money", new() { ["kind"] = "spent", ["amount"] = -5, ["note"] = "x" }), Today, CancellationToken.None));
    }

    [Fact]
    public void Proposals_describe_themselves()
    {
        var money = new AssistantProposal(Guid.NewGuid(), ProposalKind.LogMoney, "Gas", MoneyKind: TxKind.Expense, Amount: 500, Category: "Transport", Account: "Cash", Date: Today);

        Assert.Equal("Spent ₱500 · Transport · Cash · Oct 21 · “Gas”", money.Describe());
        Assert.Equal("Mark “Lab 3” as done", new AssistantProposal(Guid.NewGuid(), ProposalKind.CompleteTask, "Lab 3").Describe());
    }

    [Fact]
    public void She_is_told_what_became_of_her_suggestions()
    {
        var done = new AssistantProposal(Guid.NewGuid(), ProposalKind.AddIdea, "Pomodoro groups", Status: ProposalStatus.Done);
        var skipped = new AssistantProposal(Guid.NewGuid(), ProposalKind.AddInboxNote, "call mom", Status: ProposalStatus.Skipped);

        var remembered = new ChatLine(true, "Here you go!", Proposals: [done, skipped]).ForModel();

        Assert.Contains("Here you go!", remembered);
        Assert.Contains("“Pomodoro groups”: the user confirmed it", remembered);
        Assert.Contains("“call mom”: the user skipped it, so it did NOT happen", remembered);
    }

    [Fact]
    public void Her_instructions_say_suggestions_wait_for_confirmation()
    {
        var settings = new AssistantSettings();
        var allowed = new[] { new AssistantTools(null!).For(settings), new AssistantProposals(null!).For(settings) }.SelectMany(t => t).ToList();

        var withSuggestions = AssistantPrompt.Build(settings, Today, allowed);
        var without = AssistantPrompt.Build(settings, Today, new AssistantTools(null!).For(settings));

        Assert.Contains("it has NOT happened until they tap 'Do it'", withSuggestions);
        Assert.Contains("You can't change anything", without);
    }
}
