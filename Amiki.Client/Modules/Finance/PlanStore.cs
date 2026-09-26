using Amiki.Data;

namespace Amiki.Modules.Finance;

/// <summary>
/// What-if plans. Edits on the page change the plan objects directly and call
/// <see cref="Touch"/>, which queues the save; there is no draft state. Pages should hold on to
/// a plan's id rather than the object, since a refresh from the server replaces the objects.
/// </summary>
public sealed class PlanStore(Api api, SyncQueue sync, FinanceStore finance) : IRemoteStore
{
    private const string Path = "plans";
    private List<Plan> _plans = [];

    public event Action? Changed;

    public IReadOnlyList<Plan> All => _plans;

    public IReadOnlyList<Plan> Active => _plans.Where(p => !p.IsArchived).ToList();

    public IReadOnlyList<Plan> Archived => _plans.Where(p => p.IsArchived).OrderByDescending(p => p.ArchivedAt).ToList();

    public Plan? Get(Guid id) => _plans.FirstOrDefault(p => p.Id == id);

    public async Task LoadAsync(CancellationToken ct)
    {
        _plans = (await api.GetAllAsync<Plan>(Path, ct)).OrderBy(p => p.CreatedAt).ToList();
        Changed?.Invoke();
    }

    public Plan Create(string name = "New plan")
    {
        var plan = new Plan { Name = name, StartBalance = finance.TotalBalance, StartDate = DateTime.Today };
        _plans.Add(plan);
        Save(plan);
        return plan;
    }

    /// <summary>Copies a plan: the quickest way to try a different version.</summary>
    public Plan Duplicate(Plan source)
    {
        var copy = source.Clone(newIdentity: true);
        copy.Name = $"{source.Name} (copy)";
        copy.ArchivedAt = null;
        _plans.Add(copy);
        Touch(copy);
        return copy;
    }

    public void Touch(Plan plan)
    {
        plan.UpdatedAt = DateTime.Now;
        Save(plan);
    }

    /// <summary>Re-snapshots from today's real balance. Items that were "now" stay "now".</summary>
    public void UpdateToToday(Plan plan)
    {
        foreach (var item in plan.Items.Where(i => i.Date == plan.StartDate)) item.Date = DateTime.Today;
        plan.StartBalance = finance.TotalBalance;
        plan.StartDate = DateTime.Today;
        Touch(plan);
    }

    public void Archive(Plan plan)
    {
        plan = Get(plan.Id) ?? plan;
        plan.ArchivedAt = DateTime.Now;
        Save(plan);
    }

    // Also used by Undo, which may hold an object a refresh has since replaced.
    public void Unarchive(Plan plan)
    {
        plan = Get(plan.Id) ?? plan;
        plan.ArchivedAt = null;
        Save(plan);
    }

    public void Delete(Plan plan)
    {
        if (_plans.RemoveAll(p => p.Id == plan.Id) == 0) return;
        sync.Delete(Path, plan.Id);
        Changed?.Invoke();
    }

    public void Restore(Plan plan)
    {
        if (_plans.Any(p => p.Id == plan.Id)) return;
        _plans.Add(plan);
        _plans = _plans.OrderBy(p => p.CreatedAt).ToList();
        Save(plan);
    }

    private void Save(Plan plan)
    {
        sync.Put(Path, plan.Id, plan);
        Changed?.Invoke();
    }
}
