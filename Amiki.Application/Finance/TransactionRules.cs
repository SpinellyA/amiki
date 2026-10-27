using Amiki.Application.Entities;
using Amiki.Domain.Finance;

namespace Amiki.Application.Finance;

/// <summary>A transaction's account and category must be real ones. Every change is audited.</summary>
public sealed class TransactionRules(MoneyNames names) : EntityRules<Transaction>
{
    public override async Task<string?> CheckAsync(Transaction incoming, CancellationToken ct) =>
        await names.AccountProblemAsync(incoming.Account, ct) ?? await names.CategoryProblemAsync(incoming.Category, incoming.Kind, ct);

    public override string AuditAs => "transactions";

    public override string Describe(Transaction item) => item.Describe();
}
