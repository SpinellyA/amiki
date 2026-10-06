using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.JSInterop;

namespace Amiki.Data;

/// <summary>
/// Sends local changes to the API in the order they happened, one at a time.
///
/// Stores change their in-memory list first (so the screen updates instantly) and then queue
/// the write here. A failed send (offline at school, server restarting) is retried with backoff
/// until it goes through. Back-to-back saves of the same item that haven't been sent yet collapse
/// into one, with the latest content. If the server rejects a change outright (400/409), it's
/// dropped and <see cref="Rejected"/> says why.
///
/// Waiting changes are also kept in the browser's localStorage, so closing the tab (or the
/// phone killing it) while offline doesn't lose them: they're sent next time Amiki opens.
/// </summary>
public sealed class SyncQueue(Api api, IJSRuntime js)
{
    private const string StorageKey = "amiki.pending";

    private sealed class Op(string method, string url, string? json)
    {
        public string Method { get; } = method;
        public string Url { get; } = url;
        public string? Json { get; set; } = json;
        public bool Sending { get; set; }
    }

    private sealed record StoredOp(string Method, string Url, string? Json);

    private readonly LinkedList<Op> _pending = new();
    private Task? _worker;
    private int _failures;

    /// <summary>Raised whenever the number of waiting changes or the connection state changes.</summary>
    public event Action? Changed;

    /// <summary>Raised with the server's reason when it refuses a change.</summary>
    public event Action<string>? Rejected;

    /// <summary>Raised when everything waiting has been sent, so data can be refreshed.</summary>
    public event Action? Drained;

    public int Pending => _pending.Count;
    public bool Offline => _failures > 0;

    /// <summary>The server said we're not signed in. Changes are kept (and saved) until you sign in again.</summary>
    public bool NeedsSignIn { get; private set; }

    public void Put<T>(string path, Guid id, T item) where T : class
    {
        var url = $"api/{path}/{id}";
        var json = JsonSerializer.Serialize(item, Api.Json);

        // The last thing waiting is an unsent save of this same item (e.g. typing in a plan)? Send
        // the newest content instead of queueing another copy. Only the last one, though: updating
        // an earlier save would send this content ahead of changes it may depend on (a transaction
        // moved to a category that is created further down the queue).
        var waiting = _pending.Last?.Value is { Sending: false, Method: "PUT" } last && last.Url == url ? last : null;
        if (waiting is not null)
        {
            waiting.Json = json;
            Save();
        }
        else Enqueue(new Op("PUT", url, json));
    }

    public void Delete(string path, Guid id)
    {
        var url = $"api/{path}/{id}";
        // An unsent save of something now deleted doesn't need to go out.
        foreach (var op in _pending.Where(op => !op.Sending && op.Url == url).ToList()) _pending.Remove(op);
        Enqueue(new Op("DELETE", url, null));
    }

    /// <summary>Picks up changes left over from a previous visit and starts sending them. Returns how many.</summary>
    public int RestoreSaved()
    {
        try
        {
            var json = ((IJSInProcessRuntime)js).Invoke<string?>("localStorage.getItem", StorageKey);
            if (string.IsNullOrEmpty(json)) return 0;
            foreach (var op in JsonSerializer.Deserialize<List<StoredOp>>(json) ?? [])
                _pending.AddLast(new Op(op.Method, op.Url, op.Json));
        }
        catch
        {
            // Unreadable leftovers: start clean rather than failing to open.
        }
        if (_pending.Count > 0) Start();
        return _pending.Count;
    }

    private void Enqueue(Op op)
    {
        _pending.AddLast(op);
        Start();
    }

    private void Start()
    {
        Save();
        Changed?.Invoke();
        if (_worker is null || _worker.IsCompleted) _worker = RunAsync();
    }

    private void Save()
    {
        try
        {
            var runtime = (IJSInProcessRuntime)js;
            if (_pending.Count == 0) runtime.InvokeVoid("localStorage.removeItem", StorageKey);
            else runtime.InvokeVoid("localStorage.setItem", StorageKey,
                JsonSerializer.Serialize(_pending.Select(o => new StoredOp(o.Method, o.Url, o.Json))));
        }
        catch
        {
            // Storage blocked (private mode, quota): the in-memory queue still works for this visit.
        }
    }

    private async Task RunAsync()
    {
        while (_pending.First is { } node)
        {
            var op = node.Value;
            op.Sending = true;
            try
            {
                using var request = new HttpRequestMessage(new HttpMethod(op.Method), op.Url)
                {
                    Content = op.Json is null ? null : new StringContent(op.Json, Encoding.UTF8, "application/json"),
                };
                using var response = await api.Http.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    // Session expired: keep everything, check back now and then.
                    op.Sending = false;
                    NeedsSignIn = true;
                    Changed?.Invoke();
                    await Task.Delay(TimeSpan.FromSeconds(30));
                    continue;
                }
                NeedsSignIn = false;

                if (response.IsSuccessStatusCode || response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict)
                {
                    if (!response.IsSuccessStatusCode) Rejected?.Invoke(await ReasonAsync(response));
                    _pending.Remove(node);
                    _failures = 0;
                    Save();
                    Changed?.Invoke();
                    if (_pending.Count == 0) Drained?.Invoke();
                    continue;
                }
                throw new HttpRequestException($"Server answered {(int)response.StatusCode}.");
            }
            catch (Exception)
            {
                op.Sending = false;
                _failures++;
                Changed?.Invoke();
                await Task.Delay(TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, _failures))));
            }
        }
    }

    private static async Task<string> ReasonAsync(HttpResponseMessage response)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemBody>();
            return problem?.Detail ?? "The server didn't accept a change.";
        }
        catch
        {
            return "The server didn't accept a change.";
        }
    }

    private sealed record ProblemBody(string? Title, string? Detail);
}
