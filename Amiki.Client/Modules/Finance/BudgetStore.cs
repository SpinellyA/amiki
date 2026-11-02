using Amiki.Data;

namespace Amiki.Modules.Finance;

/// <summary>Your spending budgets on this device, kept in sync with the server.</summary>
public sealed class BudgetStore(Api api, SyncQueue sync, FinanceStore finance) : IRemoteStore
{
    private const string Path = "budgets";

    private List<Budget> _budgets = [];

    public event Action? Changed;

    public BudgetBook Book => new(_budgets);

    public IEnumerable<(string Path, object Data)> Snapshot() => [(Path, _budgets)];

    public async Task LoadAsync(CancellationToken ct)
    {
        _budgets = await api.GetAllAsync<Budget>(Path, ct);
        Changed?.Invoke();
    }

    /// <summary>The budget for <paramref name="period"/> and how spending is going against it; null without a budget.</summary>
    public BudgetPace? PaceFor(MoneyPeriod period)
    {
        if (Book.For(period) is not { } budget) return null;
        var spent = finance.FlowsIn(period).Where(t => t.Kind == TxKind.Expense).Sum(t => t.Amount);
        return new BudgetPace(budget.Amount, spent, period, DateTime.Today);
    }

    /// <summary>The period's own budget, if it has one (rather than one carried over).</summary>
    public Budget? Own(MoneyPeriod period) => _budgets.FirstOrDefault(b => b.Id == Budget.IdFor(period));

    public void Save(Budget budget)
    {
        var i = _budgets.FindIndex(b => b.Id == budget.Id);
        if (i >= 0) _budgets[i] = budget;
        else _budgets.Add(budget);
        sync.Put(Path, budget.Id, budget);
        Changed?.Invoke();
    }

    public void Remove(Budget budget)
    {
        if (_budgets.RemoveAll(b => b.Id == budget.Id) == 0) return;
        sync.Delete(Path, budget.Id);
        Changed?.Invoke();
    }
}
