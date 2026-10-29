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
            Section: NavSection.Track));
        return services;
    }
}
