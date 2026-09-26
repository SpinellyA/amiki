using Amiki.Core;
using Amiki.Data;

namespace Amiki.Modules.Finance;

/// <summary>
/// The server's money history (read-only; the server writes it alongside each change). Loaded
/// when the History page opens; not part of the background refresh or the device's saved copy.
/// </summary>
public sealed class AuditStore(Api api)
{
    public event Action? Changed;

    public IReadOnlyList<AuditEntry> Entries { get; private set; } = [];

    public async Task LoadAsync(CancellationToken ct)
    {
        Entries = await api.GetAllAsync<AuditEntry>("audit", ct);
        Changed?.Invoke();
    }
}
