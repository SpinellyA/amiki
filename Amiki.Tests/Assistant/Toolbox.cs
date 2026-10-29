using Amiki.Application.Assistant;
using Amiki.Application.Assistant.Tools.Lookups;
using Amiki.Application.Assistant.Tools.Proposals;

namespace Amiki.Tests.Assistant;

/// <summary>
/// Amiki's real tools, in the order the app registers them, for tests that only look at which
/// tools are offered (so they never touch the database they'd need to run).
/// </summary>
internal static class Toolbox
{
    public static AssistantTools Lookups() => new(
    [
        new ListTasksTool(null!), new ListIdeasTool(null!), new AccountBalancesTool(null!, null!), new MoneySummaryTool(null!),
        new SpendingByPeriodTool(null!), new ListTransactionsTool(null!), new ListInboxTool(null!), new WeeklyNotesTool(null!), new ListWishlistTool(null!, null!),
    ]);

    public static AssistantProposals Proposals() => new(
    [
        new ProposeAddTask(), new ProposeCompleteTask(null!), new ProposeLogMoney(null!),
        new ProposeAddIdea(), new ProposeInboxNote(), new ProposeRemember(),
    ]);
}
