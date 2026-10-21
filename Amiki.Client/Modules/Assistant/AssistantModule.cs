using MudBlazor;

namespace Amiki.Modules.Assistant;

public static class AssistantModule
{
    public static readonly IReadOnlyList<Amiki.Components.ViewSwitch.View> Views =
    [
        new("Chat", Icons.Material.Outlined.Forum, "amiki"),
        new("Settings", Icons.Material.Outlined.Tune, "amiki/settings"),
    ];

    public static IServiceCollection AddAssistantModule(this IServiceCollection services)
    {
        services.AddSingleton<AssistantSettingsStore>();
        services.AddSingleton<Amiki.Data.IRemoteStore>(sp => sp.GetRequiredService<AssistantSettingsStore>());
        services.AddSingleton<ChatSession>();
        services.AddScoped<ProposalRunner>();
        services.AddSingleton(new ModuleInfo(
            Name: "Amiki",
            Icon: Icons.Material.Outlined.AutoAwesome,
            Href: "amiki",
            Order: 0,
            Section: NavSection.Assistant));
        return services;
    }
}
