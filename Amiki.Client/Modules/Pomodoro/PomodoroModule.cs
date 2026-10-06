using MudBlazor;

namespace Amiki.Modules.Pomodoro;

public static class PomodoroModule
{
    public static IServiceCollection AddPomodoroModule(this IServiceCollection services)
    {
        // Singletons: one timer for the whole app, so it keeps going as you move between pages.
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<PomodoroTimer>();
        services.AddSingleton<PomodoroPrefs>();
        services.AddSingleton<PomodoroChime>();
        services.AddSingleton(new ModuleInfo(
            Name: "Pomodoro",
            Icon: Icons.Material.Outlined.Timer,
            Href: "pomodoro",
            Order: 10,
            Overlay: typeof(PomodoroMiniTimer),
            Section: NavSection.Tools));
        return services;
    }
}
