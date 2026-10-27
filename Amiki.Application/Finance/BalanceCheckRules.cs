using Amiki.Application.Data;
using Amiki.Application.Entities;
using Amiki.Domain.Finance;
using Microsoft.EntityFrameworkCore;

namespace Amiki.Application.Finance;

/// <summary>
/// A recorded check is evidence: it can't be changed, and a mismatched one must point at its
/// correction, saved first and on the same account. Every change is audited.
/// </summary>
public sealed class BalanceCheckRules(IAmikiDb db, MoneyNames names) : EntityRules<BalanceCheck>
{
    public override async Task<string?> CheckAsync(BalanceCheck incoming, CancellationToken ct)
    {
        if (await names.AccountProblemAsync(incoming.Account, ct) is { } missing) return missing;
        if (incoming.AdjustmentId is not { } adjustmentId) return null;

        var adjustment = await db.Transactions.AsNoTracking().FirstOrDefaultAsync(t => t.Id == adjustmentId, ct);
        return adjustment is null ? "The correction for this balance check hasn't been saved."
            : adjustment.Account != incoming.Account ? "The correction is on a different account."
            : adjustment.Signed != incoming.Difference ? "The correction doesn't match the difference."
            : null;
    }

    // Re-sending the same check is fine (a retried save); changing it isn't.
    public override string? CheckReplace(BalanceCheck stored, BalanceCheck incoming) =>
        stored.IsSameRecordAs(incoming) ? null : "A recorded balance check can't be changed. Record a new one instead.";

    public override string AuditAs => "balance-checks";

    public override string Describe(BalanceCheck item) => item.Describe();
}
