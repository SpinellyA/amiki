using System.Text.Json;
using Amiki.Contracts.People;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Amiki.Data;

/// <summary>
/// The owner looking at someone else's Amiki, in this tab only (sessionStorage), look-only.
///
/// Starting or stopping reloads the app, so every screen loads that person's data fresh and
/// nothing of theirs mixes with yours: while viewing, every request carries the
/// X-Amiki-View-As header, this device's saved copy of your data is left alone, and changes
/// aren't sent (the server would refuse them anyway). Their diary is never shown.
/// </summary>
public sealed class Viewing(IJSRuntime js, NavigationManager nav)
{
    public const string Header = "X-Amiki-View-As";
    private const string StorageKey = "amiki.viewing";

    private sealed record Target(Guid Id, string Label);

    private Target? _target;

    public bool IsViewing => _target is not null;

    /// <summary>Whose Amiki this is, e.g. "Mika", while viewing.</summary>
    public string? Label => _target?.Label;

    /// <summary>Reads this tab's choice and puts the header on every request. Called once at startup.</summary>
    public void Apply(HttpClient http)
    {
        try
        {
            var json = Runtime.Invoke<string?>("sessionStorage.getItem", StorageKey);
            _target = string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<Target>(json);
        }
        catch
        {
            _target = null; // unreadable: just your own Amiki
        }
        if (_target is not null) http.DefaultRequestHeaders.Add(Header, _target.Id.ToString());
    }

    public void Start(Person person)
    {
        Runtime.InvokeVoid("sessionStorage.setItem", StorageKey, JsonSerializer.Serialize(new Target(person.Id, person.Label)));
        nav.NavigateTo("", forceLoad: true);
    }

    public void Stop()
    {
        Runtime.InvokeVoid("sessionStorage.removeItem", StorageKey);
        nav.NavigateTo("people", forceLoad: true);
    }

    private IJSInProcessRuntime Runtime => (IJSInProcessRuntime)js;
}
