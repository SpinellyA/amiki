using MudBlazor;

namespace Amiki.Modules.Finance;

public static class FinanceModule
{
    public static readonly IReadOnlyList<Amiki.Components.ViewSwitch.View> Views =
    [
        new("Overview", Icons.Material.Outlined.AccountBalanceWallet, "money"),
        new("What if", Icons.Material.Outlined.ShowChart, "money/plan"),
    ];

    public static IServiceCollection AddFinanceModule(this IServiceCollection services)
    {
        services.AddSingleton<FinanceStore>();
        services.AddSingleton<PlanStore>();
        services.AddSingleton<Amiki.Data.IRemoteStore>(sp => sp.GetRequiredService<FinanceStore>());
        services.AddSingleton<Amiki.Data.IRemoteStore>(sp => sp.GetRequiredService<PlanStore>());
        services.AddScoped<FinanceActions>();
        services.AddSingleton(new ModuleInfo(
            Name: "Money",
            Icon: Icons.Material.Outlined.AccountBalanceWallet,
            Href: "money",
            Order: 30,
            Widgets: [new(typeof(MoneyWidget), 5)]));
        return services;
    }
}
