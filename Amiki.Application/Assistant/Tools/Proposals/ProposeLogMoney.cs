using Amiki.Application.Data;
using Amiki.Domain;
using Amiki.Domain.Assistant;
using Amiki.Domain.Finance;
using Microsoft.EntityFrameworkCore;

namespace Amiki.Application.Assistant.Tools.Proposals;

/// <summary>Category and account are matched to real ones, so the card always saves.</summary>
public sealed class ProposeLogMoney(IAmikiDb db) : IProposalTool
{
    public ToolDefinition Definition { get; } = ToolDefinition.Create("propose_log_money",
        "Suggest logging money spent or received (pesos). The user confirms it in the app.",
        AssistantArea.Money, """
        {"type":"object","properties":{
          "kind":{"type":"string","enum":["spent","received"]},
          "amount":{"type":"number"},
          "note":{"type":"string","description":"What it was for, e.g. 'Gas'"},
          "category":{"type":"string","description":"One of the user's categories"},
          "account":{"type":"string","description":"One of the user's accounts; Cash if unclear"},
          "date":{"type":"string","description":"yyyy-MM-dd; omit for today"}},
         "required":["kind","amount","note"]}
        """);

    public async Task<AssistantProposal> ProposeAsync(ToolArguments args, DateTime today, CancellationToken ct)
    {
        var kind = args.Text("kind") == "received" ? TxKind.Income : TxKind.Expense;
        var amount = Math.Round(args.Amount("amount") ?? 0, 2);
        if (amount <= 0) throw new ToolException("'amount' must be more than zero.");

        var categories = await db.Categories.AsNoTracking()
            .Where(c => c.Kind == kind && c.Name != Category.BalanceFix && c.Name != Category.Fees)
            .Select(c => c.Name).ToListAsync(ct);
        var accounts = await db.Accounts.AsNoTracking().Select(a => a.Name).ToListAsync(ct);

        return new AssistantProposal(Guid.NewGuid(), ProposalKind.LogMoney,
            args.RequiredText("note", Transaction.MaxNote),
            MoneyKind: kind,
            Amount: amount,
            Category: KnownNames.Match(args.Text("category"), categories) ?? Category.FallbackFor(kind),
            Account: KnownNames.Match(args.Text("account"), accounts) ?? KnownNames.Match("Cash", accounts) ?? accounts.FirstOrDefault(),
            Date: args.Date("date") ?? today);
    }
}
