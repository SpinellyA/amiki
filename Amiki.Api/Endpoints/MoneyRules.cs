using Amiki.Api.Data;
using Amiki.Modules.Finance;
using Microsoft.EntityFrameworkCore;

namespace Amiki.Api.Endpoints;

/// <summary>Money rules that need the database, enforced by the API whatever the client sends.</summary>
public static class MoneyRules
{
    public static async Task<string?> AccountExists(AmikiDb db, string account, CancellationToken ct) =>
        await db.Accounts.AnyAsync(a => a.Name == account, ct) ? null : $"There's no account called “{account}”.";

    /// <summary>A mismatched check must point at its correction, which has to be saved first and on the same account.</summary>
    public static async Task<string?> CheckIsBacked(AmikiDb db, BalanceCheck check, CancellationToken ct)
    {
        if (await AccountExists(db, check.Account, ct) is { } missing) return missing;
        if (check.AdjustmentId is not { } adjustmentId) return null;

        var adjustment = await db.Transactions.AsNoTracking().FirstOrDefaultAsync(t => t.Id == adjustmentId, ct);
        return adjustment is null ? "The correction for this balance check hasn't been saved."
            : adjustment.Account != check.Account ? "The correction is on a different account."
            : adjustment.Signed != check.Difference ? "The correction doesn't match the difference."
            : null;
    }

    /// <summary>A recorded check is evidence; re-sending the same one is fine, changing it isn't.</summary>
    public static string? ChecksAreFinal(BalanceCheck stored, BalanceCheck incoming) =>
        stored.Account == incoming.Account && stored.Expected == incoming.Expected
        && stored.Actual == incoming.Actual && stored.AdjustmentId == incoming.AdjustmentId
            ? null
            : "A recorded balance check can't be changed. Record a new one instead.";
}
