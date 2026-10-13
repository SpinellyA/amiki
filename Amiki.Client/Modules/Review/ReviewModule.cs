using MudBlazor;

namespace Amiki.Modules.Review;

public static class ReviewModule
{
    public static IServiceCollection AddReviewModule(this IServiceCollection services)
    {
        services.AddSingleton<WeeklyNoteStore>();
        services.AddSingleton<Amiki.Data.IRemoteStore>(sp => sp.GetRequiredService<WeeklyNoteStore>());
        services.AddSingleton(new ModuleInfo(
            Name: "Weekly review",
            Icon: Icons.Material.Outlined.EventNote,
            Href: "review",
            Order: 50));
        return services;
    }
}
