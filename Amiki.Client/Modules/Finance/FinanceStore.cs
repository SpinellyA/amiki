using Amiki.Data;

namespace Amiki.Modules.Finance;

/// <summary>
/// Balances are never stored: each account's balance is its opening balance plus every
/// transaction on it, so editing or deleting an old transaction can't leave a stale total.
/// </summary>
public sealed class FinanceStore(Api api, SyncQueue sync) : IRemoteStore
{
    private const string Path = "transactions";
    private List<Transaction> _items = [];

    public event Action? Changed;

    public IReadOnlyList<Account> Accounts { get; private set; } = [];

    public IReadOnlyList<Transaction> All => _items;

    public decimal Balance(string account) =>
        (Accounts.FirstOrDefault(a => a.Name == account)?.OpeningBalance ?? 0)
        + _items.Where(t => t.Account == account).Sum(t => t.Signed);

    public decimal TotalBalance => Accounts.Sum(a => Balance(a.Name));

    public IEnumerable<Transaction> InMonth(DateTime month) =>
        _items.Where(t => t.Date.Year == month.Year && t.Date.Month == month.Month);

    public async Task LoadAsync(CancellationToken ct)
    {
        var accounts = api.GetAllAsync<Account>("accounts", ct);
        var transactions = api.GetAllAsync<Transaction>(Path, ct);
        Accounts = await accounts;
        _items = await transactions;
        Changed?.Invoke();
    }

    public void Upsert(Transaction tx)
    {
        var i = _items.FindIndex(t => t.Id == tx.Id);
        if (i >= 0) _items[i] = tx;
        else _items.Add(tx);
        sync.Put(Path, tx.Id, tx);
        Changed?.Invoke();
    }

    public void Remove(Transaction tx)
    {
        if (_items.RemoveAll(t => t.Id == tx.Id) == 0) return;
        sync.Delete(Path, tx.Id);
        Changed?.Invoke();
    }
}
