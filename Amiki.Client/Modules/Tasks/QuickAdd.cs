namespace Amiki.Modules.Tasks;

/// <summary>
/// Parses one-line capture: "Finish lab report #cmsc128 !high @fri".
///   #word            project
///   !high !med !low  priority (also !1 !2 !3, !h !m !l)
///   @today @tomorrow @mon..@sun (next occurrence)
/// Anything unrecognised stays in the title.
/// </summary>
public static class QuickAdd
{
    public static TaskItem Parse(string input)
    {
        var item = new TaskItem();
        var words = new List<string>();

        foreach (var token in input.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var rest = token.Length > 1 ? token[1..] : "";
            if (token[0] == '#' && rest.Length > 0) item.Project = rest.ToLowerInvariant();
            else if (token[0] == '!' && ParsePriority(rest) is { } p) item.Priority = p;
            else if (token[0] == '@' && ParseDue(rest) is { } d) item.Due = d;
            else words.Add(token);
        }

        item.Title = string.Join(' ', words);
        return item;
    }

    private static Priority? ParsePriority(string s) => s.ToLowerInvariant() switch
    {
        "1" or "h" or "high" => Priority.High,
        "2" or "m" or "med" or "medium" => Priority.Medium,
        "3" or "l" or "low" => Priority.Low,
        _ => null,
    };

    private static DateTime? ParseDue(string s)
    {
        var today = DateTime.Today;
        switch (s.ToLowerInvariant())
        {
            case "today": return today;
            case "tom" or "tomorrow": return today.AddDays(1);
        }
        if (s.Length < 3) return null;
        for (var i = 1; i <= 7; i++)
        {
            var d = today.AddDays(i);
            if (d.DayOfWeek.ToString().StartsWith(s, StringComparison.OrdinalIgnoreCase)) return d;
        }
        return null;
    }
}
