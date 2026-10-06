using Amiki.Data;

namespace Amiki.Modules.Finance;

/// <summary>
/// Your spending and income categories. Renaming or deleting one also changes its transactions;
/// <see cref="FinanceActions"/> does both together so they never disagree.
/// </summary>
public sealed class CategoryStore(Api api, SyncQueue sync) : IRemoteStore
{
    private const string Path = "categories";
    private List<Category> _items = [];

    public event Action? Changed;

    public IReadOnlyList<Category> All => _items;

    public IEnumerable<(string Path, object Data)> Snapshot() => [(Path, _items)];

    /// <summary>
    /// What you can file money under, A–Z with the built-in catch-alls last. Balance fixes are
    /// left out: only a balance check makes those.
    /// </summary>
    public IEnumerable<Category> Pickable(TxKind kind) =>
        _items.Where(c => c.Kind == kind && !Category.IsCorrection(c.Name))
            .OrderBy(c => c.IsBuiltIn)
            .ThenBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase);

    public Category? Find(string name, TxKind kind) => _items.FirstOrDefault(c => c.Name == name && c.Kind == kind);

    public string IconOf(string name, TxKind kind) => CategoryIcons.Get(Find(name, kind)?.Icon);

    /// <summary>Whether another category of the same kind already has this name (ignoring case).</summary>
    public bool NameTaken(string name, TxKind kind, Guid exceptId) =>
        _items.Any(c => c.Id != exceptId && c.Kind == kind && string.Equals(c.Name, name.Trim(), StringComparison.CurrentCultureIgnoreCase));

    public async Task LoadAsync(CancellationToken ct)
    {
        _items = await api.GetAllAsync<Category>(Path, ct);
        Changed?.Invoke();
    }

    public void Save(Category category)
    {
        var i = _items.FindIndex(c => c.Id == category.Id);
        if (i >= 0) _items[i] = category;
        else _items.Add(category);
        sync.Put(Path, category.Id, category);
        Changed?.Invoke();
    }

    public void Remove(Category category)
    {
        if (_items.RemoveAll(c => c.Id == category.Id) == 0) return;
        sync.Delete(Path, category.Id);
        Changed?.Invoke();
    }
}
