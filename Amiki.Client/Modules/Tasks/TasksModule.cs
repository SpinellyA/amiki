using MudBlazor;

namespace Amiki.Modules.Tasks;

public static class TasksModule
{
    public static readonly IReadOnlyList<Amiki.Components.ViewSwitch.View> Views =
    [
        new("List", Icons.Material.Outlined.ViewList, "tasks"),
        new("Calendar", Icons.Material.Outlined.CalendarMonth, "tasks/calendar"),
    ];

    public static IServiceCollection AddTasksModule(this IServiceCollection services)
    {
        services.AddSingleton<TaskStore>();
        services.AddSingleton<Amiki.Data.IRemoteStore>(sp => sp.GetRequiredService<TaskStore>());
        services.AddScoped<TaskActions>();
        services.AddSingleton(new ModuleInfo(
            Name: "Tasks",
            Icon: Icons.Material.Outlined.TaskAlt,
            Href: "tasks",
            Order: 10,
            Widgets: [new(typeof(TasksTodayWidget), 7), new(typeof(TasksWeekWidget), 5)]));
        return services;
    }
}
