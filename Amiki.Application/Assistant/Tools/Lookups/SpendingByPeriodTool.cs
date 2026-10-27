using System.Globalization;
using System.Text.Json.Nodes;
using Amiki.Application.Finance;
using Amiki.Domain;
using Amiki.Domain.Assistant;
using Amiki.Domain.Finance;

namespace Amiki.Application.Assistant.Tools.Lookups;

public sealed class SpendingByPeriodTool(MoneyBook money) : ILookupTool
{
    private const int DefaultDays = 91;
    private const int TopCount = 3;

    public ToolDefinition Definition { get; } = ToolDefinition.Create("spending_by_period",
        "Spending and income per week (Monday to Sunday) or per month across a date range, each with its top spending categories and biggest transactions. Use this for 'most expensive week/month', trends, or comparing periods: one call covers the whole range.",
        AssistantArea.Money, """
        {"type":"object","properties":{
          "period":{"type":"string","enum":["week","month"]},
          "from":{"type":"string","description":"yyyy-MM-dd, inclusive; omit for about the last 3 months"},
          "to":{"type":"string","description":"yyyy-MM-dd, inclusive; omit for today"}},
         "required":["period"]}
        """);

    public async Task<JsonNode> RunAsync(ToolArguments args, DateTime today, CancellationToken ct)
    {
        var (from, to) = args.DateRange(today, defaultFrom: today.AddDays(-DefaultDays));
        return PerPeriod(await money.FlowsAsync(from, to, ct), byMonth: args.Text("period") == "month");
    }

    /// <summary>Groups real money flows into weeks or months, newest first, with what drove each period's spending.</summary>
    internal static JsonObject PerPeriod(IEnumerable<Transaction> flows, bool byMonth)
    {
        var periods = flows
            .GroupBy(f => byMonth ? new DateTime(f.Date.Year, f.Date.Month, 1) : WeekRange.Of(f.Date).Start)
            .OrderByDescending(g => g.Key)
            .Select(g =>
            {
                var spending = g.Where(f => f.Kind == TxKind.Expense).ToList();
                return new JsonObject
                {
                    [byMonth ? "month" : "week_of"] = byMonth ? g.Key.ToString("MMMM yyyy", CultureInfo.InvariantCulture) : ToolResult.Day(g.Key),
                    ["spent"] = spending.Sum(f => f.Amount),
                    ["received"] = g.Where(f => f.Kind == TxKind.Income).Sum(f => f.Amount),
                    ["top_categories"] = new JsonObject(spending.GroupBy(f => f.Category)
                        .OrderByDescending(c => c.Sum(f => f.Amount)).Take(TopCount)
                        .Select(c => KeyValuePair.Create(c.Key, (JsonNode?)JsonValue.Create(c.Sum(f => f.Amount))))),
                    ["biggest"] = new JsonArray(spending.OrderByDescending(f => f.Amount).Take(TopCount)
                        .Select(f => (JsonNode)new JsonObject { ["date"] = ToolResult.Day(f.Date), ["amount"] = f.Amount, ["category"] = f.Category, ["note"] = f.Note })
                        .ToArray()),
                };
            })
            .ToList();
        var top = periods.OrderByDescending(p => p["spent"]!.GetValue<decimal>()).FirstOrDefault();
        return new JsonObject
        {
            ["periods"] = new JsonArray(periods.Cast<JsonNode>().ToArray()),
            ["most_spent"] = top?.DeepClone(),
            ["note"] = "Periods with no money in or out are left out.",
        };
    }
}
