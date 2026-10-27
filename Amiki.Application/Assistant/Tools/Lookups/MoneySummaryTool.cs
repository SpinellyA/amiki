using System.Text.Json.Nodes;
using Amiki.Application.Finance;
using Amiki.Domain.Assistant;
using Amiki.Domain.Finance;

namespace Amiki.Application.Assistant.Tools.Lookups;

public sealed class MoneySummaryTool(MoneyBook money) : ILookupTool
{
    public ToolDefinition Definition { get; } = ToolDefinition.Create("money_summary",
        "Totals for a date range: spent, received, and spending/income per category. Moving money between own accounts isn't counted; transfer fees are.",
        AssistantArea.Money, """
        {"type":"object","properties":{
          "from":{"type":"string","description":"yyyy-MM-dd, inclusive"},
          "to":{"type":"string","description":"yyyy-MM-dd, inclusive"}},
         "required":["from","to"]}
        """);

    public async Task<JsonNode> RunAsync(ToolArguments args, DateTime today, CancellationToken ct)
    {
        var (from, to) = args.DateRange(today);
        var flows = await money.FlowsAsync(from, to, ct);
        var spending = flows.Where(f => f.Kind == TxKind.Expense).ToList();
        var income = flows.Where(f => f.Kind == TxKind.Income).ToList();
        return new JsonObject
        {
            ["from"] = ToolResult.Day(from),
            ["to"] = ToolResult.Day(to.AddDays(-1)),
            ["spent"] = spending.Sum(f => f.Amount),
            ["received"] = income.Sum(f => f.Amount),
            ["spending_by_category"] = ByCategory(spending),
            ["income_by_category"] = ByCategory(income),
        };
    }

    private static JsonObject ByCategory(IEnumerable<Transaction> flows) => new(flows
        .GroupBy(f => f.Category)
        .OrderByDescending(g => g.Sum(f => f.Amount))
        .Select(g => KeyValuePair.Create(g.Key, (JsonNode?)JsonValue.Create(g.Sum(f => f.Amount)))));
}
