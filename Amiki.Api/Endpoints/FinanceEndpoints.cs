using Amiki.Application.Finance;
using Amiki.Domain.Finance;
using Amiki.Domain.Wishlist;

namespace Amiki.Api.Endpoints;

/// <summary>
/// Money. Every change to categories, transactions, transfers and balance checks is audited,
/// and account and category names must be real ones (see the rules in Application/Finance).
///   /api/categories, /api/transactions, /api/transfers, /api/balance-checks, /api/plans, /api/wishlist
///   GET /api/accounts    your accounts
///   GET /api/audit       the money history, newest first (read-only)
/// </summary>
public static class FinanceEndpoints
{
    public static void MapFinance(this RouteGroupBuilder api)
    {
        api.MapEntity<Category>("categories");
        api.MapEntity<Transaction>("transactions");
        api.MapEntity<Transfer>("transfers");
        api.MapEntity<BalanceCheck>("balance-checks");
        api.MapEntity<Plan>("plans");
        api.MapEntity<WishItem>("wishlist");

        api.MapGet("/accounts", async (MoneyQueries money, CancellationToken ct) => Results.Ok(await money.AccountsAsync(ct)));
        api.MapGet("/audit", async (MoneyQueries money, CancellationToken ct) => Results.Ok(await money.HistoryAsync(ct)));
    }
}
