using System.Net;
using System.Net.Http.Json;
using Amiki.Core;
using Microsoft.AspNetCore.Components;

namespace Amiki.Data;

/// <summary>
/// Who's signed in. The server holds the actual session in an HttpOnly cookie; this just asks
/// it, and sends the browser to the server's Google sign-in when needed.
/// </summary>
public sealed class Session(Api api, NavigationManager nav)
{
    public SessionInfo? Current { get; private set; }

    /// <summary>Null when not signed in (or signed in with an account that isn't allowed).</summary>
    public async Task<SessionInfo?> LoadAsync()
    {
        using var response = await api.Http.GetAsync("auth/me");
        if (response.StatusCode == HttpStatusCode.Unauthorized) return Current = null;
        response.EnsureSuccessStatusCode();
        return Current = await response.Content.ReadFromJsonAsync<SessionInfo>(Api.Json);
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
        await api.Http.PostAsync("auth/logout", content: null);
        nav.NavigateTo("", forceLoad: true);
    }
}
