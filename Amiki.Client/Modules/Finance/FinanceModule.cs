using MudBlazor;

namespace Amiki.Modules.Finance;

public static class FinanceModule
{
    public static readonly IReadOnlyList<Amiki.Components.ViewSwitch.View> Views =
    [
        new("Overview", Icons.Material.Outlined.AccountBalanceWallet, "money"),
        new("What if", Icons.Material.Outlined.ShowChart, "money/plan"),
        new("History", Icons.Material.Outlined.History, "money/history"),
        new("Categories", Icons.Material.Outlined.Category, "money/categories"),
    ];

    public static IServiceCollection AddFinanceModule(this IServiceCollection services)
    {
        services.AddSingleton<CategoryStore>();
        services.AddSingleton<FinanceStore>();
        services.AddSingleton<PlanStore>();
        // Loaded when the History page opens, not on every background refresh.
        services.AddSingleton<AuditStore>();
        services.AddSingleton<Amiki.Data.IRemoteStore>(sp => sp.GetRequiredService<CategoryStore>());
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
