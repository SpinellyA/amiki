using System.Text.Json.Nodes;
using Amiki.Application.Finance;
using Amiki.Domain.Assistant;
using Amiki.Domain.Finance;

namespace Amiki.Application.Assistant.Tools.Lookups;

public sealed class ListTransactionsTool(MoneyBook money) : ILookupTool
{
    public ToolDefinition Definition { get; } = ToolDefinition.Create("list_transactions",
        "Individual transactions in a date range, newest first, optionally filtered by category or note text.",
        AssistantArea.Money, """
        {"type":"object","properties":{
          "from":{"type":"string","description":"yyyy-MM-dd, inclusive"},
          "to":{"type":"string","description":"yyyy-MM-dd, inclusive"},
          "category":{"type":"string"},
          "search":{"type":"string","description":"Text in the note"}},
         "required":["from","to"]}
        """);

    public async Task<JsonNode> RunAsync(ToolArguments args, DateTime today, CancellationToken ct)
    {
        var (from, to) = args.DateRange(today);
        var category = args.Text("category");
        var search = args.Text("search");
        return ToolResult.Rows((await money.FlowsAsync(from, to, ct))
            .Where(f => category is null || string.Equals(f.Category, category, StringComparison.OrdinalIgnoreCase))
            .Where(f => search is null || f.Note.Contains(search, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(f => f.Date)
            .Select(f => new JsonObject
            {
                ["date"] = ToolResult.Day(f.Date),
                ["kind"] = f.Kind == TxKind.Income ? "received" : "spent",
                ["amount"] = f.Amount,
                ["category"] = f.Category,
                ["account"] = f.Account,
                ["note"] = f.Note,
            }));
    }
}
