namespace Amiki.Modules.Diary;

public static class DiaryDays
{
    /// <summary>"Today", "Yesterday", "Mon, Oct 12", or "Mon, Oct 12, 2025" for other years.</summary>
    public static string Label(DateTime day) => (DateTime.Today - day.Date).Days switch
    {
        0 => "Today",
        1 => "Yesterday",
        _ => day.ToString(day.Year == DateTime.Today.Year ? "ddd, MMM d" : "ddd, MMM d, yyyy"),
    };
}
