using System.Net;
using System.Net.Http.Json;
using Amiki.Data;
using Amiki.Modules.Assistant;

namespace Amiki.Modules.Inbox;

/// <summary>
/// Asks the server (and through it, Gemini) what each Inbox note is. One request covers every
/// note that doesn't have a suggestion yet, and answers are remembered for the session, so the
/// free tier's limits go a long way. Off entirely when the server has no Gemini key.
/// </summary>
public sealed class InboxSuggestions(Api api)
{
    private readonly Dictionary<Guid, (string Text, TriageSuggestion Suggestion)> _known = [];
    private static readonly TimeSpan PauseAfterProblem = TimeSpan.FromMinutes(1);

    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool? _enabled;
    private DateTime _quietUntil = DateTime.MinValue; // after a failure, don't spend the free tier retrying every refresh

    public event Action? Changed;

    /// <summary>Whether suggestions are on (known after the first <see cref="RefreshAsync"/>).</summary>
    public bool Enabled => _enabled == true;

    public bool Busy { get; private set; }

    /// <summary>Why the last request didn't work (e.g. the free-tier limit), shown above the Inbox.</summary>
    public string? Problem { get; private set; }

    public TriageSuggestion? For(InboxItem item) =>
        _known.TryGetValue(item.Id, out var known) && known.Text == item.Text ? known.Suggestion : null;

    public async Task RefreshAsync(IEnumerable<InboxItem> items)
    {
        if (!await _gate.WaitAsync(0)) return; // one at a time; the next change will catch up
        try
        {
            _enabled ??= await StatusAsync();
            if (_enabled != true || DateTime.Now < _quietUntil) return;

            var missing = items.Where(i => For(i) is null).Take(TriageRequest.MaxNotes).ToList();
            if (missing.Count == 0) return;

            Busy = true;
            Problem = null;
            Changed?.Invoke();

            var request = new TriageRequest(DateTime.Today, missing.Select(i => new TriageNote(i.Id, i.Text)).ToList());
            using var response = await api.Http.PostAsJsonAsync("api/assistant/triage", request, Api.Json);
            if (response.IsSuccessStatusCode)
            {
                var suggestions = await response.Content.ReadFromJsonAsync<List<TriageSuggestion>>(Api.Json) ?? [];
                var texts = missing.ToDictionary(i => i.Id, i => i.Text);
                foreach (var s in suggestions.Where(s => texts.ContainsKey(s.NoteId)))
                    _known[s.NoteId] = (texts[s.NoteId], s);
            }
            else
            {
                _quietUntil = DateTime.Now + PauseAfterProblem;
                Problem = response.StatusCode == HttpStatusCode.TooManyRequests
                    ? "Gemini's free-tier limit was reached. Suggestions will come back in a minute."
                    : "Couldn't get suggestions right now.";
            }
        }
        catch (HttpRequestException)
        {
            // Offline: sorting by hand still works, and the next refresh tries again.
        }
        finally
        {
            Busy = false;
            _gate.Release();
            Changed?.Invoke();
        }
    }

    /// <summary>Forget a note's suggestion (it was sorted, or you want a fresh one).</summary>
    public void Forget(InboxItem item)
    {
        if (_known.Remove(item.Id)) Changed?.Invoke();
    }

    private async Task<bool> StatusAsync()
    {
        try
        {
            var status = await api.Http.GetFromJsonAsync<Status>("api/assistant/status", Api.Json);
            return status?.Enabled == true;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    private sealed record Status(bool Enabled);
}
