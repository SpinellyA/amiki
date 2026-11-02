using System.Text.Json.Nodes;
using Amiki.Application.Finance;
using Amiki.Domain.Assistant;

namespace Amiki.Application.Assistant.Tools.Lookups;

public sealed class BudgetStatusTool(BudgetCheck budgets) : ILookupTool
{
    public ToolDefinition Definition { get; } = ToolDefinition.Create("budget_status",
        "The user's spending budgets for this week and this month: the limit, spent so far, what's left, what they'll spend at this rate, and how much a day keeps them within it. Use for 'am I within budget', 'how much can I still spend', 'am I on pace'.",
        AssistantArea.Money);

    public async Task<JsonNode> RunAsync(ToolArguments args, DateTime today, CancellationToken ct)
    {
        var paces = await budgets.NowAsync(today, ct);
        if (paces.Count == 0) return new JsonObject { ["note"] = "No budget is set for this week or this month." };

        return ToolResult.Rows(paces.Select(p => new JsonObject
        {
            ["period"] = p.Period.Length.ToString().ToLowerInvariant(),
            ["from"] = ToolResult.Day(p.Period.Start),
            ["to"] = ToolResult.Day(p.Period.End.AddDays(-1)),
            ["budget"] = p.Limit,
            ["spent"] = p.Spent,
            ["left"] = p.Left,
            ["at_this_rate_by_the_end"] = p.Projected,
            ["per_day_to_stay_within"] = p.PerDayLeft,
            ["days_left_including_today"] = p.DaysLeft,
            ["status"] = p.Status switch
            {
                Domain.Finance.BudgetStatus.Over => "over budget",
                Domain.Finance.BudgetStatus.AtRisk => "within budget so far, but on pace to go over",
                _ => "on track",
            },
        }));
    }
}
