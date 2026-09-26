using System.Globalization;

namespace Amiki.Modules.Finance;

public static class Money
{
    public static string Format(decimal value) =>
        "₱" + Math.Abs(value).ToString("#,0.##", CultureInfo.InvariantCulture);

    public static string Signed(decimal value) => (value < 0 ? "−" : "+") + Format(value);

    /// <summary>₱12.4K style for tight spaces like chart labels. Negative values get a leading −.</summary>
    public static string Compact(decimal value)
    {
        var abs = Math.Abs(value);
        var body = abs switch
        {
            >= 1_000_000 => string.Create(CultureInfo.InvariantCulture, $"₱{abs / 1_000_000:0.#}M"),
            >= 10_000 => string.Create(CultureInfo.InvariantCulture, $"₱{abs / 1_000:0.#}K"),
            _ => Format(abs),
        };
        return (value < 0 ? "−" : "") + body;
    }
}
