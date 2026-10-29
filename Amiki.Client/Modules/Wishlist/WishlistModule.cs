using MudBlazor;

namespace Amiki.Modules.Wishlist;

public static class WishlistModule
{
    public static IServiceCollection AddWishlistModule(this IServiceCollection services)
    {
        services.AddSingleton<WishlistStore>();
        services.AddSingleton<Amiki.Data.IRemoteStore>(sp => sp.GetRequiredService<WishlistStore>());
        services.AddScoped<WishlistActions>();
        services.AddSingleton(new ModuleInfo(
            Name: "Wishlist",
            Icon: Icons.Material.Outlined.FavoriteBorder,
            Href: "wishlist",
            Order: 35,
            Section: NavSection.Track,
            Widgets: [new(typeof(WishlistWidget), 5)],
            // Listens for income on every page, to grow wishlist money.
            Overlay: typeof(WishlistIncomeWatcher)));
        return services;
    }
}
