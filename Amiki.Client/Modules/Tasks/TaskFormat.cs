using MudBlazor;

namespace Amiki.Modules.Tasks;

public static class TaskFormat
{
    public static string Due(DateTime due)
    {
        var days = (due.Date - DateTime.Today).Days;
        return days switch
        {
            0 => "Today",
            1 => "Tomorrow",
            -1 => "Yesterday",
            > 1 and < 7 => due.ToString("dddd"),
            < -1 and > -7 => $"{-days} days ago",
            _ => due.ToString("MMM d"),
        };
    }

    public static Color PriorityColor(Priority p) => p switch
    {
        Priority.High => Color.Error,
        Priority.Medium => Color.Warning,
        Priority.Low => Color.Info,
        _ => Color.Default,
    };
}
