namespace Amiki.Domain.Finance;

/// <summary>
/// The accounts each person starts with. They start at zero: the first balance check on each sets
/// its real amount (as a labeled correction), instead of made-up opening balances skewing totals.
/// </summary>
public static class StarterAccounts
{
    private static readonly string[] Names = ["Cash", "GCash", "Maribank", "Landbank"];

    /// <summary>A fresh set each time, so they can be added for more than one person.</summary>
    public static Account[] Create() => [.. Names.Select(name => new Account(name, 0))];
}
