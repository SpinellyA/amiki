using Amiki.Modules.Ideas;
using Amiki.Modules.Tasks;
using MudBlazor;

namespace Amiki.Modules.Inbox;

/// <summary>
/// Moves an inbox item into the module it belongs to. This is the only place the Inbox
/// knows about other modules, and it only uses their public stores and actions.
/// </summary>
public sealed class InboxTriage(InboxStore inbox, IdeaStore ideas, TaskActions tasks, ISnackbar snackbar)
{
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
