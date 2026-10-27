using Amiki.Domain;
using System.Globalization;
using Amiki.Domain.Assistant;

namespace Amiki.Application.Assistant;

/// <summary>
/// The dashboard's heads-ups, by plain rules (no AI), most pressing first. Each one only for an
/// area Amiki is allowed to see.
/// </summary>
public static class NudgeRules
{
    public static readonly TimeSpan CheckBalancesEvery = TimeSpan.FromDays(14);
    public static readonly TimeSpan IdeasGoStale = TimeSpan.FromDays(30);
    public const int StaleIdeasWorthMentioning = 3;
    public const int FullInbox = 5;

    public static IReadOnlyList<Nudge> From(NudgeInput d)
    {
        var nudges = new List<Nudge>();
        var today = d.Today.Date;

        if (d.SeeTasks)
        {
            var overdue = d.OpenTasks.Where(t => t.Due?.Date < today).OrderBy(t => t.Due).ToList();
            if (overdue.Count > 0)
                nudges.Add(new("overdue", $"{Count(overdue.Count, "task")} overdue (oldest: “{overdue[0].Title}”)", "tasks"));

            var dueToday = d.OpenTasks.Where(t => t.Due?.Date == today).ToList();
            if (dueToday.Count > 0)
                nudges.Add(new("due-today", $"Due today: {string.Join(", ", dueToday.Take(3).Select(t => $"“{t.Title}”"))}{(dueToday.Count > 3 ? $" and {dueToday.Count - 3} more" : "")}", "tasks"));
        }

        if (d.SeeWeeklyNotes)
        {
            var thisWeek = WeekRange.Of(today).Start;
            if (today.DayOfWeek == DayOfWeek.Sunday && !d.ReviewedWeeks.Contains(thisWeek))
                nudges.Add(new("review", "It's Sunday: a few minutes on your weekly review?", "review"));
            else if (today.DayOfWeek == DayOfWeek.Monday && !d.ReviewedWeeks.Contains(thisWeek.AddDays(-7)))
                nudges.Add(new("review", "Last week hasn't been reviewed yet.", "review"));
        }

        if (d.SeeMoney)
        {
            var notChecked = d.AccountsLastChecked
                .Where(a => a.LastChecked is not { } at || today - at.Date >= CheckBalancesEvery)
                .Select(a => a.Account).Order().ToList();
            if (notChecked.Count > 0)
                nudges.Add(new("balance", $"{JoinNames(notChecked)} {(notChecked.Count == 1 ? "hasn't" : "haven't")} been checked against the real balance in 2+ weeks.", "money"));

            nudges.AddRange(Spending(d.SpendingByDay, today));
        }

        if (d.SeeIdeas)
        {
            var stale = d.ParkedIdeaDates.Count(c => today - c.Date >= IdeasGoStale);
            if (stale >= StaleIdeasWorthMentioning)
                nudges.Add(new("ideas", $"{Count(stale, "idea")} parked for over a month: still want them?", "ideas"));
        }

        if (d.SeeInbox && d.InboxCount >= FullInbox)
            nudges.Add(new("inbox", $"{Count(d.InboxCount, "note")} waiting in your Inbox.", "inbox"));

        return nudges;
    }

    /// <summary>
    /// Categories where this week's spending is well above your usual week (the average of the four
    /// before it): at least 1.5× and ₱300 more. At most two, biggest jump first.
    /// </summary>
    internal static IEnumerable<Nudge> Spending(IReadOnlyList<(DateTime Day, string Category, decimal Amount)> spending, DateTime today)
    {
        var thisWeek = WeekRange.Of(today).Start;
        var before = thisWeek.AddDays(-28);
        return spending
            .GroupBy(s => s.Category)
            .Select(g => (
                Category: g.Key,
                Now: g.Where(s => s.Day >= thisWeek).Sum(s => s.Amount),
                Usual: g.Where(s => s.Day >= before && s.Day < thisWeek).Sum(s => s.Amount) / 4))
            .Where(c => c.Now >= c.Usual * 1.5m && c.Now - c.Usual >= 300)
            .OrderByDescending(c => c.Now - c.Usual)
            .Take(2)
            .Select(c => new Nudge("spending",
                c.Usual == 0
                    ? $"{c.Category}: {Peso(c.Now)} this week, more than you usually spend there."
                    : $"{c.Category}: {Peso(c.Now)} this week, usually about {Peso(c.Usual)}.",
                "money"));
    }

    private static string Count(int n, string word) => $"{n} {word}{(n == 1 ? "" : "s")}";

    private static string JoinNames(IReadOnlyList<string> names) =>
        names.Count == 1 ? names[0] : $"{string.Join(", ", names.Take(names.Count - 1))} and {names[^1]}";

    private static string Peso(decimal amount) => "₱" + Math.Round(amount).ToString("#,0", CultureInfo.InvariantCulture);
}
