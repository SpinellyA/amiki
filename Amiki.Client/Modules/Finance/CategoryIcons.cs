using MudBlazor;

namespace Amiki.Modules.Finance;

/// <summary>
/// The icons a category can have. Categories store the key ("Restaurant"), not the icon, so the
/// database never holds SVG markup and an icon can be swapped here without touching saved data.
/// </summary>
public static class CategoryIcons
{
    public const string Default = "Label";

    public static readonly IReadOnlyDictionary<string, string> All = new Dictionary<string, string>
    {
        // Spending
        ["Restaurant"] = Icons.Material.Outlined.Restaurant,
        ["LocalCafe"] = Icons.Material.Outlined.LocalCafe,
        ["Fastfood"] = Icons.Material.Outlined.Fastfood,
        ["LocalGroceryStore"] = Icons.Material.Outlined.LocalGroceryStore,
        ["DirectionsCar"] = Icons.Material.Outlined.DirectionsCar,
        ["DirectionsBus"] = Icons.Material.Outlined.DirectionsBus,
        ["LocalGasStation"] = Icons.Material.Outlined.LocalGasStation,
        ["TwoWheeler"] = Icons.Material.Outlined.TwoWheeler,
        ["School"] = Icons.Material.Outlined.School,
        ["MenuBook"] = Icons.Material.Outlined.MenuBook,
        ["PhoneAndroid"] = Icons.Material.Outlined.PhoneAndroid,
        ["Wifi"] = Icons.Material.Outlined.Wifi,
        ["ReceiptLong"] = Icons.Material.Outlined.ReceiptLong,
        ["Home"] = Icons.Material.Outlined.Home,
        ["Bolt"] = Icons.Material.Outlined.Bolt,
        ["ShoppingBag"] = Icons.Material.Outlined.ShoppingBag,
        ["Checkroom"] = Icons.Material.Outlined.Checkroom,
        ["Computer"] = Icons.Material.Outlined.Computer,
        ["LocalHospital"] = Icons.Material.Outlined.LocalHospital,
        ["FitnessCenter"] = Icons.Material.Outlined.FitnessCenter,
        ["ContentCut"] = Icons.Material.Outlined.ContentCut,
        ["SportsEsports"] = Icons.Material.Outlined.SportsEsports,
        ["Movie"] = Icons.Material.Outlined.Movie,
        ["Subscriptions"] = Icons.Material.Outlined.Subscriptions,
        ["Flight"] = Icons.Material.Outlined.Flight,
        ["Pets"] = Icons.Material.Outlined.Pets,
        ["Favorite"] = Icons.Material.Outlined.FavoriteBorder,
        ["Groups"] = Icons.Material.Outlined.Groups,
        ["VolunteerActivism"] = Icons.Material.Outlined.VolunteerActivism,
        ["Toll"] = Icons.Material.Outlined.Toll,
        // Income
        ["Savings"] = Icons.Material.Outlined.Savings,
        ["Work"] = Icons.Material.Outlined.Work,
        ["CardGiftcard"] = Icons.Material.Outlined.CardGiftcard,
        ["Replay"] = Icons.Material.Outlined.Replay,
        ["Sell"] = Icons.Material.Outlined.Sell,
        ["EmojiEvents"] = Icons.Material.Outlined.EmojiEvents,
        ["TrendingUp"] = Icons.Material.Outlined.TrendingUp,
        ["AttachMoney"] = Icons.Material.Outlined.AttachMoney,
        // Anything
        ["FactCheck"] = Icons.Material.Outlined.FactCheck,
        ["MoreHoriz"] = Icons.Material.Outlined.MoreHoriz,
        ["Star"] = Icons.Material.Outlined.StarBorder,
        [Default] = Icons.Material.Outlined.Label,
    };

    public static string Get(string? key) => key is not null && All.TryGetValue(key, out var icon) ? icon : All[Default];
}
