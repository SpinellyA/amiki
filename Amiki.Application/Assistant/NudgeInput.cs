namespace Amiki.Application.Assistant;

/// <summary>What the nudge rules look at, gathered from the database. Empty lists for areas Amiki can't see.</summary>
/// <param name="OpenTasks">Unfinished tasks: title and due date.</param>
/// <param name="ParkedIdeaDates">When each parked (not archived) idea was captured.</param>
/// <param name="AccountsLastChecked">Each account that has any money on record, and its last balance check.</param>
/// <param name="SpendingByDay">Spending per category per day, for the last five weeks.</param>
/// <param name="ReviewedWeeks">Mondays of weeks that have a weekly review note.</param>
/// <param name="CooledOffWants">Wishlist items sitting in Want past the cooling-off time: name and when added.</param>
public sealed record NudgeInput(
    DateTime Today,
    IReadOnlyList<(string Title, DateTime? Due)> OpenTasks,
    IReadOnlyList<DateTime> ParkedIdeaDates,
    IReadOnlyList<(string Account, DateTime? LastChecked)> AccountsLastChecked,
    IReadOnlyList<(DateTime Day, string Category, decimal Amount)> SpendingByDay,
    int InboxCount,
    IReadOnlyList<DateTime> ReviewedWeeks,
    IReadOnlyList<(string Name, DateTime Added)> CooledOffWants,
    bool SeeTasks, bool SeeIdeas, bool SeeMoney, bool SeeInbox, bool SeeWeeklyNotes);
