using Microsoft.JSInterop;

namespace Amiki.Modules.Ideas;

/// <summary>
/// Remembers your Ideas layout in this browser, so the phone can use List while the second
/// monitor keeps the Board. Falls back to Board when storage isn't available.
/// </summary>
public sealed class IdeaLayoutPreference(IJSRuntime js)
{
    private const string Key = "amiki.ideas.layout";

    public IdeaLayout Get()
    {
        try
        {
            var saved = ((IJSInProcessRuntime)js).Invoke<string?>("localStorage.getItem", Key);
            return Enum.TryParse<IdeaLayout>(saved, out var layout) ? layout : IdeaLayout.Board;
        }
        catch
        {
            return IdeaLayout.Board;
        }
    }

    public void Set(IdeaLayout layout)
    {
        try { ((IJSInProcessRuntime)js).InvokeVoid("localStorage.setItem", Key, layout.ToString()); }
        catch { /* not remembered; nothing else breaks */ }
    }
}
