using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Amiki.Core;
using Microsoft.AspNetCore.Components;

namespace Amiki.Data;

/// <summary>
/// Who's signed in. The server holds the actual session in an HttpOnly cookie; this asks it,
/// and sends the browser to the server's Google sign-in when needed.
///
/// A device that signed in before remembers who it was, so the installed app can open straight
/// into your data (offline too). The server is still asked in the background, and if it says the
/// session is over, the saved data is wiped and the sign-in screen comes back.
/// </summary>
public sealed class Session(Api api, NavigationManager nav)
{
    private const string CacheKey = "session";

    public SessionInfo? Current { get; private set; }

    /// <summary>Null when not signed in (or signed in with an account that isn't allowed).</summary>
    public async Task<SessionInfo?> LoadAsync()
    {
        if (Saved() is { } saved)
        {
            Current = saved;
            _ = ConfirmAsync();
            return saved;
        }
        return await AskServerAsync();
    }

    /// <summary>Full-page trip to Google and back to the page you were on.</summary>
    public void SignIn()
    {
        var path = "/" + nav.ToBaseRelativePath(nav.Uri);
        if (path.Contains("auth=")) path = "/";
        nav.NavigateTo($"auth/login?returnUrl={Uri.EscapeDataString(path)}", forceLoad: true);
    }

    public async Task SignOutAsync()
    {
        // Wipe this device's copy first, so it's gone even if the server can't be reached.
        api.ClearCache();
        try { await api.Http.PostAsync("auth/logout", content: null); }
        catch { /* offline: the cookie expires on its own; the device copy is already gone */ }
        nav.NavigateTo("", forceLoad: true);
    }

    private async Task<SessionInfo?> AskServerAsync()
    {
        using var response = await api.Http.GetAsync("auth/me");
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            api.ClearCache();
            return Current = null;
        }
        response.EnsureSuccessStatusCode();
        Current = await response.Content.ReadFromJsonAsync<SessionInfo>(Api.Json);
        if (Current is not null) api.WriteCache(CacheKey, JsonSerializer.Serialize(Current, Api.Json));
        return Current;
    }

    private async Task ConfirmAsync()
    {
        try
        {
            if (await AskServerAsync() is null) nav.NavigateTo(nav.Uri, forceLoad: true);
        }
        catch
        {
            // Offline or the server is waking up: keep going with the saved session for now.
        }
    }

    private SessionInfo? Saved()
    {
        try
        {
            return api.ReadCache(CacheKey) is { } json ? JsonSerializer.Deserialize<SessionInfo>(json, Api.Json) : null;
        }
        catch
        {
            return null;
        }
    }
}
