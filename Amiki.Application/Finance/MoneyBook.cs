using Amiki.Application.Data;
using Amiki.Domain.Finance;
using Microsoft.EntityFrameworkCore;

namespace Amiki.Application.Finance;

/// <summary>Loads money from the database into the shared <see cref="Ledger"/> rules.</summary>
public sealed class MoneyBook(IAmikiDb db)
{
    /// <summary>Everything, for balances. One person's data is small.</summary>
    public async Task<Ledger> AllAsync(CancellationToken ct) => new(
        await db.Accounts.AsNoTracking().ToListAsync(ct),
        await db.Transactions.AsNoTracking().ToListAsync(ct),
        await db.Transfers.AsNoTracking().ToListAsync(ct));

    /// <summary>Real money in and out in [from, to): see <see cref="Ledger.FlowsBetween"/>.</summary>
    public async Task<IReadOnlyList<Transaction>> FlowsAsync(DateTime from, DateTime to, CancellationToken ct)
    {
        var ledger = new Ledger([],
            await db.Transactions.AsNoTracking().Where(t => t.Date >= from && t.Date < to).ToListAsync(ct),
            await db.Transfers.AsNoTracking().Where(t => t.Date >= from && t.Date < to).ToListAsync(ct));
        return [.. ledger.FlowsBetween(from, to)];
    }
}
