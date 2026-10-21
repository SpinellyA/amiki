using Amiki.Data;

namespace Amiki.Modules.Assistant;

/// <summary>Amiki's settings (what she can see, what she knows about you). One set, shared by your devices.</summary>
public sealed class AssistantSettingsStore(Api api, SyncQueue sync) : IRemoteStore
{
    private const string Path = "assistant-settings";

    public event Action? Changed;

    /// <summary>The saved settings, or the defaults until you change something.</summary>
    public AssistantSettings Current { get; private set; } = new();

    public IEnumerable<(string Path, object Data)> Snapshot() => [(Path, new[] { Current })];

    public async Task LoadAsync(CancellationToken ct)
    {
        Current = (await api.GetAllAsync<AssistantSettings>(Path, ct)).FirstOrDefault() ?? new AssistantSettings();
        Changed?.Invoke();
    }

    public void Save(AssistantSettings settings)
    {
        settings.UpdatedAt = DateTime.Now;
        Current = settings;
        sync.Put(Path, settings.Id, settings);
        Changed?.Invoke();
    }
}
