using MudBlazor;

namespace Amiki.Modules.Diary;

public static class DiaryModule
{
    public static IServiceCollection AddDiaryModule(this IServiceCollection services)
    {
        services.AddSingleton<DiaryStore>();
        services.AddSingleton<Amiki.Data.IRemoteStore>(sp => sp.GetRequiredService<DiaryStore>());
        services.AddSingleton(new ModuleInfo(
            Name: "Diary",
            Icon: Icons.Material.Outlined.AutoStories,
            Href: "diary",
            Order: 25,
            Section: NavSection.Reflect,
            Widgets: [new(typeof(DiaryWidget), 5)]));
        return services;
    }
}
