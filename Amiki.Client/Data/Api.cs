using System.Net.Http.Json;
using System.Text.Json;
using Amiki.Core;

namespace Amiki.Data;

/// <summary>Reads from the Amiki API. Writes go through <see cref="SyncQueue"/> instead.</summary>
public sealed class Api(HttpClient http)
{
    public static readonly JsonSerializerOptions Json = AmikiJson.Create();

    public HttpClient Http => http;

    public async Task<List<T>> GetAllAsync<T>(string path, CancellationToken ct = default) =>
        await http.GetFromJsonAsync<List<T>>($"api/{path}", Json, ct) ?? [];
}

/// <summary>A module store whose contents come from the API.</summary>
public interface IRemoteStore
{
    Task LoadAsync(CancellationToken ct);
}
