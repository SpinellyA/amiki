using System.Text.Json.Nodes;
using Amiki.Application.Data;
using Amiki.Application.Finance;
using Amiki.Domain.Assistant;
using Microsoft.EntityFrameworkCore;

namespace Amiki.Application.Assistant.Tools.Lookups;

public sealed class AccountBalancesTool(IAmikiDb db, MoneyBook money) : ILookupTool
{
    public ToolDefinition Definition { get; } = ToolDefinition.Create("account_balances",
        "Current balance of each money account (Philippine pesos) and when it was last checked against the real account.",
        AssistantArea.Money);

    public async Task<JsonNode> RunAsync(ToolArguments args, DateTime today, CancellationToken ct)
    {
        var ledger = await money.AllAsync(ct);
        var accounts = await db.Accounts.AsNoTracking().OrderBy(a => a.Name).Select(a => a.Name).ToListAsync(ct);
        var lastChecks = await db.BalanceChecks.AsNoTracking()
            .GroupBy(c => c.Account).Select(g => new { Account = g.Key, At = g.Max(c => c.CheckedAt) }).ToListAsync(ct);

        return ToolResult.Rows(accounts.Select(account => new JsonObject
        {
            ["account"] = account,
            ["balance"] = ledger.Balance(account),
            ["last_checked"] = ToolResult.Day(lastChecks.FirstOrDefault(c => c.Account == account)?.At) ?? "never",
        }));
    }
}
