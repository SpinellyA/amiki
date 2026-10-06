using Amiki.Api.Data;
using Amiki.Modules.Finance;
using Microsoft.EntityFrameworkCore;

namespace Amiki.Api.Endpoints;

/// <summary>Money rules that need the database, enforced by the API whatever the client sends.</summary>
public static class MoneyRules
{
    public static async Task<string?> AccountExists(AmikiDb db, string account, CancellationToken ct) =>
        await db.Accounts.AnyAsync(a => a.Name == account, ct) ? null : $"There's no account called “{account}”.";

    public static async Task<string?> CategoryExists(AmikiDb db, Transaction tx, CancellationToken ct) =>
        await db.Categories.AnyAsync(c => c.Name == tx.Category && c.Kind == tx.Kind, ct)
            ? null
            : $"There's no {(tx.Kind == TxKind.Income ? "income" : "spending")} category called “{tx.Category}”.";

    /// <summary>No two spending (or two income) categories share a name, ignoring case.</summary>
    public static async Task<string?> CategoryNameIsFree(AmikiDb db, Category category, CancellationToken ct)
    {
        var name = category.Name.ToLower();
        return await db.Categories.AnyAsync(c => c.Id != category.Id && c.Kind == category.Kind && c.Name.ToLower() == name, ct)
            ? $"There's already a category called “{category.Name}”."
            : null;
    }

    /// <summary>Built-in categories stay; others go only once their transactions have moved elsewhere.</summary>
    public static async Task<string?> CategoryCanBeDeleted(AmikiDb db, Category category, CancellationToken ct)
    {
        if (category.IsBuiltIn) return $"“{category.Name}” is built in, so it can't be deleted.";
        var used = await db.Transactions.CountAsync(t => t.Category == category.Name && t.Kind == category.Kind, ct);
        return used == 0 ? null : $"“{category.Name}” still has {used} transaction{(used == 1 ? "" : "s")}. Move them to another category first.";
    }

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
