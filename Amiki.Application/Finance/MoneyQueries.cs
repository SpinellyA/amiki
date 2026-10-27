using Amiki.Application.Data;
using Amiki.Domain;
using Amiki.Domain.Finance;
using Microsoft.EntityFrameworkCore;

namespace Amiki.Application.Finance;

/// <summary>Read-only money lists that aren't plain "save this item" kinds.</summary>
public sealed class MoneyQueries(IAmikiDb db)
{
    public const int AuditShown = 500;

    public async Task<IReadOnlyList<Account>> AccountsAsync(CancellationToken ct) =>
        await db.Accounts.AsNoTracking().OrderBy(a => a.Name).ToListAsync(ct);

    /// <summary>The newest history first. The log is only ever written alongside the change it records.</summary>
    public async Task<IReadOnlyList<AuditEntry>> HistoryAsync(CancellationToken ct) =>
        await db.AuditLog.AsNoTracking().OrderByDescending(e => e.Id).Take(AuditShown).ToListAsync(ct);
}
