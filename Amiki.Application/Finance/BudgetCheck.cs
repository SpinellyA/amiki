using Amiki.Application.Data;
using Amiki.Domain.Finance;
using Microsoft.EntityFrameworkCore;

namespace Amiki.Application.Finance;

/// <summary>How this week's and this month's spending is going against their budgets.</summary>
public sealed class BudgetCheck(IAmikiDb db, MoneyBook money)
{
    /// <summary>The current week and month that have a budget, each with its pace.</summary>
    public async Task<IReadOnlyList<BudgetPace>> NowAsync(DateTime today, CancellationToken ct)
    {
        var book = new BudgetBook(await db.Budgets.AsNoTracking().ToListAsync(ct));
        var paces = new List<BudgetPace>();
        foreach (var length in new[] { PeriodLength.Week, PeriodLength.Month })
        {
            var period = MoneyPeriod.Containing(length, today);
            if (book.For(period) is not { } budget) continue;
            var spent = (await money.FlowsAsync(period.Start, period.End, ct)).Where(f => f.Kind == TxKind.Expense).Sum(f => f.Amount);
            paces.Add(new BudgetPace(budget.Amount, spent, period, today));
        }
        return paces;
    }
}
