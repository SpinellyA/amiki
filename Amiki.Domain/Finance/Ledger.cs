namespace Amiki.Domain.Finance;

/// <summary>
/// The money rules that turn transactions and transfers into balances and real money flows. The
/// Money page and Amiki's lookups both use this, so they always agree.
///
/// Balances are never stored: an account's balance is its opening balance plus every transaction
/// and transfer touching it, so editing or deleting an old entry can't leave a stale total.
/// </summary>
public sealed class Ledger(IEnumerable<Account> accounts, IEnumerable<Transaction> transactions, IEnumerable<Transfer> transfers)
{
    private readonly IReadOnlyList<Account> _accounts = [.. accounts];
    private readonly IReadOnlyList<Transaction> _transactions = [.. transactions];
    private readonly IReadOnlyList<Transfer> _transfers = [.. transfers];

    public decimal Balance(string account) =>
        (_accounts.FirstOrDefault(a => a.Name == account)?.OpeningBalance ?? 0)
        + _transactions.Where(t => t.Account == account).Sum(t => t.Signed)
        + _transfers.Where(t => t.To == account).Sum(t => t.Amount)
        - _transfers.Where(t => t.From == account).Sum(t => t.TotalOut);

    public decimal TotalBalance => _accounts.Sum(a => Balance(a.Name));

    /// <summary>
    /// Real money in and out, from <paramref name="start"/> up to (not including) <paramref name="end"/>:
    /// transactions minus balance corrections, plus transfer fees as spending. Moving money between
    /// your own accounts isn't spending, so the transfers themselves don't appear.
    /// </summary>
    public IEnumerable<Transaction> FlowsBetween(DateTime start, DateTime end) =>
        _transactions.Where(t => t.Date >= start && t.Date < end && !Category.IsCorrection(t.Category))
            .Concat(_transfers.Where(t => t.Date >= start && t.Date < end && t.Fee > 0).Select(FeeAsSpending));

    private static Transaction FeeAsSpending(Transfer t) => new()
    {
        Id = t.Id, Date = t.Date, Amount = t.Fee, Kind = TxKind.Expense, Category = Category.Fees,
        Account = t.From, Note = $"Fee: {t.From} → {t.To}", CreatedAt = t.CreatedAt,
    };
}
