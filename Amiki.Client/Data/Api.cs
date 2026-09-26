using System.Net.Http.Json;
using System.Text.Json;
using Amiki.Core;
using Microsoft.JSInterop;

namespace Amiki.Data;

/// <summary>
/// Reads from the Amiki API (writes go through <see cref="SyncQueue"/>), and keeps the on-device
/// copy of your data in localStorage so the installed app can open instantly and offline.
/// </summary>
public sealed class Api(HttpClient http, IJSRuntime js)
{
    public static readonly JsonSerializerOptions Json = AmikiJson.Create();

    private const string CachePrefix = "amiki.cache:";
    private const string CachedAtKey = "amiki.cache.at";

    public HttpClient Http => http;

    /// <summary>While true, loads come from the on-device copy instead of the network.</summary>
    public bool CacheOnly { get; set; }

    public async Task<List<T>> GetAllAsync<T>(string path, CancellationToken ct = default)
    {
        if (CacheOnly)
        {
            var cached = Storage.Invoke<string?>("localStorage.getItem", CachePrefix + path)
                ?? throw new InvalidOperationException($"No saved copy of {path}.");
            return JsonSerializer.Deserialize<List<T>>(cached, Json) ?? [];
        }
        return await http.GetFromJsonAsync<List<T>>($"api/{path}", Json, ct) ?? [];
    }

    /// <summary>When the saved copy was last updated from the server; null if there isn't one.</summary>
    public DateTime? CachedAt =>
        DateTime.TryParse(Read(CachedAtKey), System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var at) ? at : null;

    public void SaveCache(string path, object data) => Write(CachePrefix + path, JsonSerializer.Serialize(data, Json));

    public void MarkCacheFresh() => Write(CachedAtKey, DateTime.Now.ToString("O"));

    public string? ReadCache(string key) => Read(CachePrefix + key);

    public void WriteCache(string key, string value) => Write(CachePrefix + key, value);

    /// <summary>Removes every saved copy from this device (on sign-out).</summary>
    public void ClearCache()
    {
        try { Storage.InvokeVoid("amiki.clearCache"); } catch { /* storage unavailable: nothing saved */ }
    }

    private IJSInProcessRuntime Storage => (IJSInProcessRuntime)js;

    private string? Read(string key)
    {
        try { return Storage.Invoke<string?>("localStorage.getItem", key); }
        catch { return null; }
    }

    private void Write(string key, string value)
    {
        // Full or blocked storage just means no offline copy this time; the app still works online.
        try { Storage.InvokeVoid("localStorage.setItem", key, value); }
        catch { }
    }
}

/// <summary>A module store whose contents come from the API.</summary>
public interface IRemoteStore
{
    event Action? Changed;

    Task LoadAsync(CancellationToken ct);

    /// <summary>What to keep on the device, per API path, so the app can open offline with it.</summary>
    IEnumerable<(string Path, object Data)> Snapshot();
}
