using MudBlazor;

namespace Amiki.Modules.Clock;

public static class ClockModule
{
    public static IServiceCollection AddClockModule(this IServiceCollection services)
    {
        services.AddSingleton<ClockStore>();
        services.AddSingleton<Amiki.Data.IRemoteStore>(sp => sp.GetRequiredService<ClockStore>());
        services.AddSingleton<MinuteTicker>();
        services.AddScoped<ClockActions>();
        services.AddSingleton(new ModuleInfo(
            Name: "Clock",
            Icon: Icons.Material.Outlined.Schedule,
            Href: "clock",
            Order: 12,
            Section: NavSection.Plan,
            Widgets: [new(typeof(ClockWidget), 5)]));
        return services;
    }
}
