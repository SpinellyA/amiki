using System.Net.Http.Json;
using Amiki.Contracts.People;

namespace Amiki.Data;

/// <summary>
/// Calls for the People page (owner only) and for your own API key. These go straight to the
/// server rather than through the offline queue: they're rare, and you want to know right away
/// if one didn't work. Each returns null on success, or the reason it failed.
/// </summary>
public sealed class PeopleClient(Api api)
{
    private HttpClient Http => api.Http;

    public async Task<List<Person>> ListAsync() =>
        await Http.GetFromJsonAsync<List<Person>>("api/people", Api.Json) ?? [];

    public Task<string?> AddAsync(AddPersonRequest request) => SendAsync(Http.PostAsJsonAsync("api/people", request, Api.Json));

    public Task<string?> SetEnabledAsync(Guid id, bool enabled) =>
        SendAsync(Http.PutAsJsonAsync($"api/people/{id}/enabled", new EnabledRequest(enabled), Api.Json));

    public Task<string?> SetKeyAsync(Guid id, string? key) =>
        SendAsync(Http.PutAsJsonAsync($"api/people/{id}/gemini-key", new GeminiKeyRequest(key), Api.Json));

    public async Task<AssistantWords> WordsAsync(Guid id) =>
        await Http.GetFromJsonAsync<AssistantWords>($"api/people/{id}/assistant", Api.Json) ?? new AssistantWords("", "");

    public Task<string?> SetWordsAsync(Guid id, AssistantWords words) =>
        SendAsync(Http.PutAsJsonAsync($"api/people/{id}/assistant", words, Api.Json));

    public Task<string?> RemoveAsync(Guid id) => SendAsync(Http.DeleteAsync($"api/people/{id}"));

    public async Task<MyKeyInfo> MyKeyAsync() =>
        await Http.GetFromJsonAsync<MyKeyInfo>("api/me/gemini-key", Api.Json) ?? new MyKeyInfo(null);

    public Task<string?> SetMyKeyAsync(string? key) =>
        SendAsync(Http.PutAsJsonAsync("api/me/gemini-key", new GeminiKeyRequest(key), Api.Json));

    public Task<string?> NoticeSeenAsync() => SendAsync(Http.PostAsync("api/me/notice-seen", null));

    private static async Task<string?> SendAsync(Task<HttpResponseMessage> sending)
    {
        try
        {
            using var response = await sending;
            if (response.IsSuccessStatusCode) return null;
            var problem = await response.Content.ReadFromJsonAsync<ProblemBody>();
            return problem?.Detail ?? $"The server said no ({(int)response.StatusCode}).";
        }
        catch (Exception ex) when (ex is HttpRequestException or System.Text.Json.JsonException)
        {
            return "Couldn't reach the server.";
        }
    }

    private sealed record ProblemBody(string? Detail);
}
