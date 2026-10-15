using Microsoft.JSInterop;

namespace Amiki.Data;

/// <summary>
/// Small view choices remembered in this browser only, like the Ideas layout or Money's
/// month/week view, so the phone and the second monitor can each keep their own. Falls back to
/// the default when storage isn't available.
/// </summary>
public sealed class DevicePreferences(IJSRuntime js)
{
    public TEnum Get<TEnum>(string key, TEnum fallback) where TEnum : struct, Enum
    {
        try
        {
            var saved = ((IJSInProcessRuntime)js).Invoke<string?>("localStorage.getItem", Key(key));
            return Enum.TryParse<TEnum>(saved, out var value) ? value : fallback;
        }
        catch
        {
            return fallback;
        }
    }

    public void Set<TEnum>(string key, TEnum value) where TEnum : struct, Enum
    {
        try { ((IJSInProcessRuntime)js).InvokeVoid("localStorage.setItem", Key(key), value.ToString()); }
        catch { /* not remembered; nothing else breaks */ }
    }

    private static string Key(string key) => $"amiki.{key}";
}
