using Amiki.Modules.Assistant;
using Amiki.Modules.Finance;
using Amiki.Modules.Ideas;
using Amiki.Modules.Tasks;
using MudBlazor;

namespace Amiki.Modules.Inbox;

/// <summary>
/// Moves an inbox item into the module it belongs to. This is the only place the Inbox
/// knows about other modules, and it only uses their public stores and actions.
/// </summary>
public sealed class InboxTriage(
    InboxStore inbox, IdeaStore ideas, TaskStore taskStore, TaskActions tasks,
    FinanceStore money, FinanceActions moneyActions, ISnackbar snackbar)
{
    /// <summary>Does what the suggestion says in one tap: files the note as a task, idea or transaction. Undo puts it back.</summary>
    public void Accept(InboxItem item, TriageSuggestion s)
    {
        switch (s.Kind)
        {
            case TriageKind.Task:
                var task = TaskFrom(s);
                taskStore.Upsert(task);
                inbox.Remove(item);
                WithUndo($"Added task “{task.Title}”", () => { taskStore.Delete(task); inbox.Restore(item); });
                break;
            case TriageKind.Idea:
                var idea = new Idea { Text = s.Title, Tags = s.Project is { } p ? [p] : [], CreatedAt = item.CapturedAt };
                ideas.Upsert(idea);
                inbox.Remove(item);
                WithUndo("Saved to Ideas", () => { ideas.Remove(idea); inbox.Restore(item); });
                break;
            case TriageKind.Expense or TriageKind.Income:
                var tx = TransactionFrom(s, item);
                money.Upsert(tx);
                inbox.Remove(item);
                WithUndo($"Logged {Money.Signed(tx.Signed)} · {tx.Category} · {tx.Account}", () => { money.Remove(tx); inbox.Restore(item); });
                break;
        }
    }

    /// <summary>Opens the task or transaction dialog filled in from the suggestion, to change anything first.</summary>
    public async Task AdjustAsync(InboxItem item, TriageSuggestion s)
    {
        var saved = s.Kind switch
        {
            TriageKind.Task => await tasks.EditAsync(TaskFrom(s)),
            TriageKind.Expense or TriageKind.Income => await moneyActions.EditAsync(TransactionFrom(s, item)),
            _ => false,
        };
        if (saved) inbox.Remove(item);
    }

    private static TaskItem TaskFrom(TriageSuggestion s) =>
        new() { Title = s.Title, Due = s.Due ?? DateTime.Today, Project = s.Project, Priority = s.Priority };

    // Logged on the day it was captured: "bought gas 500" written at school was spent that day.
    private static Transaction TransactionFrom(TriageSuggestion s, InboxItem item) => new()
    {
        Kind = s.Kind == TriageKind.Income ? TxKind.Income : TxKind.Expense,
        Amount = s.Amount ?? 0,
        Category = s.Category ?? Category.FallbackFor(s.Kind == TriageKind.Income ? TxKind.Income : TxKind.Expense),
        Account = s.Account ?? "Cash",
        Note = s.Title,
        Date = item.CapturedAt.Date,
    };

    /// <summary>
    /// Opens the task dialog prefilled from the note (#project, !priority, @day still parse).
    /// The note only leaves the inbox if the task is saved.
    /// </summary>
    public async Task ToTaskAsync(InboxItem item)
    {
        if (await tasks.EditAsync(QuickAdd.Parse(item.Text)))
            inbox.Remove(item);
    }

    public void ToIdea(InboxItem item)
    {
        var idea = Idea.Parse(item.Text, item.CapturedAt);
        ideas.Upsert(idea);
        inbox.Remove(item);
        WithUndo("Saved to Ideas", () =>
        {
            ideas.Remove(idea);
            inbox.Restore(item);
        });
    }

    public void Discard(InboxItem item)
    {
        inbox.Remove(item);
        WithUndo("Discarded", () => inbox.Restore(item));
    }

    private void WithUndo(string message, Action undo) =>
        snackbar.Add(message, Severity.Normal, c =>
        {
            c.Action = "Undo";
            c.ActionColor = Color.Primary;
            c.OnClick = _ =>
            {
                undo();
                return Task.CompletedTask;
            };
        });
}
