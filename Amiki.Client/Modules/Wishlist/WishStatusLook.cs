using MudBlazor;

namespace Amiki.Modules.Wishlist;

/// <summary>How each wishlist status is shown: its name, icon and the heading of its section.</summary>
public static class WishStatusLook
{
    public static readonly WishStatus[] Order = [WishStatus.Saving, WishStatus.Want, WishStatus.Bought, WishStatus.Skipped];

    public static string Name(WishStatus status) => status switch
    {
        WishStatus.Saving => "Saving for",
        WishStatus.Bought => "Bought",
        WishStatus.Skipped => "Skipped",
        _ => "Want",
    };

    public static string Icon(WishStatus status) => status switch
    {
        WishStatus.Saving => Icons.Material.Outlined.Savings,
        WishStatus.Bought => Icons.Material.Outlined.CheckCircle,
        WishStatus.Skipped => Icons.Material.Outlined.DoNotDisturbOn,
        _ => Icons.Material.Outlined.FavoriteBorder,
    };

    public static string Hint(WishStatus status) => status switch
    {
        WishStatus.Saving => "Decided: you're putting money aside for these.",
        WishStatus.Bought => "Got them.",
        WishStatus.Skipped => "Changed your mind.",
        _ => "Someday, maybe.",
    };
}
