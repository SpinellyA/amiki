using Amiki.Components;
using Amiki.Modules.Finance;
using Amiki.Modules.Ideas;
using Amiki.Modules.Inbox;
using Amiki.Modules.Tasks;

namespace Amiki.Modules.Review;

/// <param name="ThisWeek">Spent in the category this week.</param>
/// <param name="LastWeek">Spent in it the week before, for comparison.</param>
public sealed record CategoryChange(string Name, decimal ThisWeek, decimal LastWeek);

/// <summary>
/// One week, looked back on: what got done, what's slipping, where the money went. Built from the
/// data every module already has; nothing here is stored.
/// </summary>
public sealed record WeeklySummary(
    WeekRange Week,
    int InboxWaiting,
    IReadOnlyList<TaskItem> TasksDone,
    IReadOnlyList<TaskItem> Overdue,
    IReadOnlyList<TaskItem> DueNextWeek,
    IReadOnlyList<Idea> IdeasCaptured,
    IReadOnlyList<Idea> IdeasFinished,
    IReadOnlyList<Idea> StaleParked,
    decimal Spent,
    decimal Received,
    decimal SpentLastWeek,
    IReadOnlyList<CategoryChange> TopSpending)
{
    /// <summary>A parked idea untouched this long gets a "still want this?" nudge.</summary>
    public static readonly TimeSpan StaleAfter = TimeSpan.FromDays(30);

    public static WeeklySummary Build(
        WeekRange week,
        DateTime today,
        IEnumerable<InboxItem> inbox,
        IEnumerable<TaskItem> tasks,
        IEnumerable<Idea> ideas,
        IEnumerable<Transaction> flowsThisWeek,
        IEnumerable<Transaction> flowsLastWeek)
    {
        var taskList = tasks.ToList();
        var activeIdeas = ideas.ToList();
        var spending = flowsThisWeek.Where(t => t.Kind == TxKind.Expense).ToList();
        var spendingBefore = flowsLastWeek.Where(t => t.Kind == TxKind.Expense).ToList();
        var next = week.Next;

        return new WeeklySummary(
            Week: week,
            InboxWaiting: inbox.Count(),
            TasksDone: taskList.Where(t => t.CompletedAt is { } done && week.Contains(done)).OrderBy(t => t.CompletedAt).ToList(),
            Overdue: taskList.Where(t => !t.IsDone && t.Due?.Date < today.Date).OrderBy(t => t.Due).ToList(),
            DueNextWeek: taskList.Where(t => !t.IsDone && t.Due is { } due && next.Contains(due)).OrderBy(t => t.Due).ToList(),
            IdeasCaptured: activeIdeas.Where(i => week.Contains(i.CreatedAt)).OrderBy(i => i.CreatedAt).ToList(),
            IdeasFinished: activeIdeas.Where(i => i.Status == IdeaStatus.Done && i.DoneAt is { } done && week.Contains(done)).ToList(),
            StaleParked: activeIdeas
                .Where(i => i.Status == IdeaStatus.Parked && !i.IsArchived && week.End - i.CreatedAt >= StaleAfter)
                .OrderBy(i => i.CreatedAt).ToList(),
            Spent: spending.Sum(t => t.Amount),
            Received: flowsThisWeek.Where(t => t.Kind == TxKind.Income).Sum(t => t.Amount),
            SpentLastWeek: spendingBefore.Sum(t => t.Amount),
            TopSpending: spending.GroupBy(t => t.Category)
                .Select(g => new CategoryChange(g.Key, g.Sum(t => t.Amount), spendingBefore.Where(t => t.Category == g.Key).Sum(t => t.Amount)))
                .OrderByDescending(c => c.ThisWeek)
                .Take(3)
                .ToList());
    }
}
