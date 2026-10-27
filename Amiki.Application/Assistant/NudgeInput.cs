namespace Amiki.Application.Assistant;

/// <summary>What the nudge rules look at, gathered from the database. Empty lists for areas Amiki can't see.</summary>
/// <param name="OpenTasks">Unfinished tasks: title and due date.</param>
/// <param name="ParkedIdeaDates">When each parked (not archived) idea was captured.</param>
/// <param name="AccountsLastChecked">Each account that has any money on record, and its last balance check.</param>
/// <param name="SpendingByDay">Spending per category per day, for the last five weeks.</param>
/// <param name="ReviewedWeeks">Mondays of weeks that have a weekly review note.</param>
public sealed record NudgeInput(
    DateTime Today,
    IReadOnlyList<(string Title, DateTime? Due)> OpenTasks,
    IReadOnlyList<DateTime> ParkedIdeaDates,
    IReadOnlyList<(string Account, DateTime? LastChecked)> AccountsLastChecked,
    IReadOnlyList<(DateTime Day, string Category, decimal Amount)> SpendingByDay,
    int InboxCount,
    IReadOnlyList<DateTime> ReviewedWeeks,
    bool SeeTasks, bool SeeIdeas, bool SeeMoney, bool SeeInbox, bool SeeWeeklyNotes);
