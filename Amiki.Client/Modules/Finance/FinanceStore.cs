using Amiki.Data;

namespace Amiki.Modules.Finance;

/// <summary>
/// Your money on this device: accounts, transactions, transfers and balance checks, kept in sync
/// with the server. Balances and flows are worked out by the shared <see cref="Ledger"/>.
/// </summary>
public sealed class FinanceStore(Api api, SyncQueue sync) : IRemoteStore
{
    private const string TransactionsPath = "transactions";
    private const string TransfersPath = "transfers";
    private const string ChecksPath = "balance-checks";

    private List<Transaction> _items = [];
    private List<Transfer> _transfers = [];
    private List<BalanceCheck> _checks = [];

    public event Action? Changed;

    public IReadOnlyList<Account> Accounts { get; private set; } = [];
    public IReadOnlyList<Transaction> All => _items;
    public IReadOnlyList<Transfer> Transfers => _transfers;
    public IReadOnlyList<BalanceCheck> Checks => _checks;

    public IEnumerable<(string Path, object Data)> Snapshot() =>
        [("accounts", Accounts), (TransactionsPath, _items), (TransfersPath, _transfers), (ChecksPath, _checks)];

    // The money rules themselves live in Domain (shared with Amiki's lookups on the server).
    private Ledger Ledger => new(Accounts, _items, _transfers);

    public decimal Balance(string account) => Ledger.Balance(account);

    public decimal TotalBalance => Ledger.TotalBalance;

    public IEnumerable<Transaction> In(MoneyPeriod period) => _items.Where(t => period.Contains(t.Date));

    public IEnumerable<Transfer> TransfersIn(MoneyPeriod period) => _transfers.Where(t => period.Contains(t.Date));

    public IEnumerable<Transaction> FlowsIn(MoneyPeriod period) => FlowsBetween(period.Start, period.End);

    public IEnumerable<Transaction> FlowsInMonth(DateTime month) => FlowsIn(MoneyPeriod.Containing(PeriodLength.Month, month));

    /// <summary>Real money in and out for totals and charts (see <see cref="Ledger.FlowsBetween"/>).</summary>
    public IEnumerable<Transaction> FlowsBetween(DateTime start, DateTime end) => Ledger.FlowsBetween(start, end);

    public BalanceCheck? LastCheck(string account) =>
        _checks.Where(c => c.Account == account).MaxBy(c => c.CheckedAt);

    public async Task LoadAsync(CancellationToken ct)
    {
        var accounts = api.GetAllAsync<Account>("accounts", ct);
        var transactions = api.GetAllAsync<Transaction>(TransactionsPath, ct);
        var transfers = api.GetAllAsync<Transfer>(TransfersPath, ct);
        var checks = api.GetAllAsync<BalanceCheck>(ChecksPath, ct);
        Accounts = await accounts;
        _items = await transactions;
        _transfers = await transfers;
        _checks = await checks;
        Changed?.Invoke();
    }

    public void Upsert(Transaction tx)
    {
        Replace(_items, tx, t => t.Id);
        sync.Put(TransactionsPath, tx.Id, tx);
        Changed?.Invoke();
    }

    public void Remove(Transaction tx)
    {
        if (_items.RemoveAll(t => t.Id == tx.Id) == 0) return;
        sync.Delete(TransactionsPath, tx.Id);
        Changed?.Invoke();
    }

    public IReadOnlyList<Transaction> InCategory(string name, TxKind kind) =>
        _items.Where(t => t.Category == name && t.Kind == kind).ToList();

    /// <summary>Files these transactions under another category, each saved as a normal (audited) edit.</summary>
    public void Recategorize(IEnumerable<Guid> ids, string category)
    {
        var moving = ids.ToHashSet();
        for (var i = 0; i < _items.Count; i++)
        {
            if (!moving.Contains(_items[i].Id) || _items[i].Category == category) continue;
            var moved = _items[i].Clone();
            moved.Category = category;
            _items[i] = moved;
            sync.Put(TransactionsPath, moved.Id, moved);
        }
        Changed?.Invoke();
    }

    public void Move(Transfer transfer)
    {
        Replace(_transfers, transfer, t => t.Id);
        sync.Put(TransfersPath, transfer.Id, transfer);
        Changed?.Invoke();
    }

    public void RemoveTransfer(Transfer transfer)
    {
        if (_transfers.RemoveAll(t => t.Id == transfer.Id) == 0) return;
        sync.Delete(TransfersPath, transfer.Id);
        Changed?.Invoke();
    }

    /// <summary>
    /// Records that the real account shows <paramref name="actual"/>. If Amiki disagrees, a
    /// labeled "Balance fix" entry for the difference is saved first, then the check pointing
    /// at it, so the account matches reality and the history shows why.
    /// </summary>
    public BalanceCheck RecordCheck(string account, decimal actual)
    {
        var check = new BalanceCheck { Account = account, Expected = Balance(account), Actual = actual };
        if (!check.Matched)
        {
            var correction = new Transaction
            {
                Amount = Math.Abs(check.Difference),
                Kind = check.Difference > 0 ? TxKind.Income : TxKind.Expense,
                Category = Category.BalanceFix,
                Account = account,
                Note = $"Balance check: matched {account} to its real balance of {Money.Format(actual)}",
            };
            Upsert(correction); // queued before the check, which the server requires
            check.AdjustmentId = correction.Id;
        }
        _checks.Add(check);
        sync.Put(ChecksPath, check.Id, check);
        Changed?.Invoke();
        return check;
    }

    private static void Replace<T>(List<T> list, T item, Func<T, Guid> id)
    {
        var i = list.FindIndex(x => id(x) == id(item));
        if (i >= 0) list[i] = item;
        else list.Add(item);
    }
}
