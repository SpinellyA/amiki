using MudBlazor;

namespace Amiki.Modules.Inbox;

public static class InboxModule
{
    /// <summary>Depends on the Tasks, Ideas and Money modules being registered too.</summary>
    public static IServiceCollection AddInboxModule(this IServiceCollection services)
    {
        services.AddSingleton<InboxStore>();
        services.AddSingleton<InboxSuggestions>();
        services.AddSingleton<Amiki.Data.IRemoteStore>(sp => sp.GetRequiredService<InboxStore>());
        services.AddScoped<InboxTriage>();
        services.AddSingleton(new ModuleInfo(
            Name: "Inbox",
            Icon: Icons.Material.Outlined.Inbox,
            Href: "inbox",
            Order: 5,
            Section: NavSection.Plan,
            Widgets: [new(typeof(InboxWidget), 5)],
            Overlay: typeof(QuickCaptureButton)));
        return services;
    }
}
