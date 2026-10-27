using Amiki.Application.Data;
using Amiki.Domain.Finance;
using Microsoft.EntityFrameworkCore;

namespace Amiki.Application.Finance;

/// <summary>
/// Money refers to accounts and categories by name, so a name has to be a real one before
/// anything is saved with it. Each check returns the problem, or null when it's fine.
/// </summary>
public sealed class MoneyNames(IAmikiDb db)
{
    public async Task<string?> AccountProblemAsync(string account, CancellationToken ct) =>
        await db.Accounts.AnyAsync(a => a.Name == account, ct) ? null : $"There's no account called “{account}”.";

    public async Task<string?> CategoryProblemAsync(string category, TxKind kind, CancellationToken ct) =>
        await db.Categories.AnyAsync(c => c.Name == category && c.Kind == kind, ct)
            ? null
            : $"There's no {(kind == TxKind.Income ? "income" : "spending")} category called “{category}”.";
}
