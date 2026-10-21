using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Amiki.Data;

namespace Amiki.Modules.Assistant;

/// <summary>
/// The current conversation with Amiki. Kept on this device (and cleared when you sign out), so
/// a reload doesn't lose it; "New chat" starts over. Each message goes to the server together
/// with the recent ones, since the model doesn't remember between calls.
/// </summary>
public sealed class ChatSession(Api api)
{
    private const string CacheKey = "chat";
    private const int KeptLines = 60;

    private List<ChatLine>? _lines;

    public event Action? Changed;

    public IReadOnlyList<ChatLine> Lines => _lines ??= Restore();

    public bool Busy { get; private set; }

    /// <summary>Why the last message didn't get an answer (e.g. the free-tier limit).</summary>
    public string? Problem { get; private set; }

    public AssistantStatus? Status { get; private set; }

    public async Task<AssistantStatus> StatusAsync()
    {
        if (Status is not null) return Status;
        try { Status = await api.Http.GetFromJsonAsync<AssistantStatus>("api/assistant/status", Api.Json); }
        catch (HttpRequestException) { /* offline: try again next time */ }
        return Status ?? new AssistantStatus(false, "");
    }

    public async Task SendAsync(string message)
    {
        message = message.Trim();
        if (message.Length == 0 || Busy) return;

        var history = Lines.ToList();
        Add(new ChatLine(false, message));
        Busy = true;
        Problem = null;
        Changed?.Invoke();

        try
        {
            var request = new ChatRequest(DateTime.Now, history.TakeLast(ChatRequest.MaxHistory).ToList(), message);
            using var response = await api.Http.PostAsJsonAsync("api/assistant/chat", request, Api.Json);
            if (response.IsSuccessStatusCode && await response.Content.ReadFromJsonAsync<ChatReply>(Api.Json) is { } reply)
                Add(new ChatLine(true, reply.Reply, reply.Looked, reply.Proposals));
            else
                Problem = await ReasonAsync(response);
        }
        catch (HttpRequestException)
        {
            Problem = "Can't reach Amiki's server right now. Check your connection and try again.";
        }
        finally
        {
            Busy = false;
            Changed?.Invoke();
        }
    }

    /// <summary>Records what you did with one of her suggestions (it's also what she's told next time).</summary>
    public void SetStatus(Guid proposalId, ProposalStatus status)
    {
        _lines = Lines.Select(line => line.Proposals?.Any(p => p.Id == proposalId) == true
            ? line with { Proposals = line.Proposals!.Select(p => p.Id == proposalId ? p with { Status = status } : p).ToList() }
            : line).ToList();
        Save();
        Changed?.Invoke();
    }

    public void NewChat()
    {
        _lines = [];
        Problem = null;
        Save();
        Changed?.Invoke();
    }

    private void Add(ChatLine line)
    {
        _lines = [.. Lines.TakeLast(KeptLines - 1), line];
        Save();
    }

    private static async Task<string> ReasonAsync(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized) return "You're signed out. Sign in again to chat.";
        try
        {
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (doc.RootElement.TryGetProperty("detail", out var detail) && detail.GetString() is { Length: > 0 } text) return text;
        }
        catch (JsonException) { }
        return "Amiki couldn't answer just now. Try again in a moment.";
    }

    private List<ChatLine> Restore()
    {
        try { return JsonSerializer.Deserialize<List<ChatLine>>(api.ReadCache(CacheKey) ?? "[]", Api.Json) ?? []; }
        catch (JsonException) { return []; }
    }

    private void Save() => api.WriteCache(CacheKey, JsonSerializer.Serialize(_lines, Api.Json));
}
