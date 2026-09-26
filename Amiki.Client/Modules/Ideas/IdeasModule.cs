using MudBlazor;

namespace Amiki.Modules.Ideas;

public static class IdeasModule
{
    public static IServiceCollection AddIdeasModule(this IServiceCollection services)
    {
        services.AddSingleton<IdeaStore>();
        services.AddSingleton<Amiki.Data.IRemoteStore>(sp => sp.GetRequiredService<IdeaStore>());
        services.AddSingleton(new ModuleInfo(
            Name: "Ideas",
            Icon: Icons.Material.Outlined.Lightbulb,
            Href: "ideas",
            Order: 20,
            Widgets: [new(typeof(RecentIdeasWidget), 7)]));
        return services;
    }
}
