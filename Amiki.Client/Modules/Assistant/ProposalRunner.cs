using Amiki.Modules.Finance;
using Amiki.Modules.Ideas;
using Amiki.Modules.Inbox;
using Amiki.Modules.Tasks;
using Amiki.Modules.Wishlist;
using MudBlazor;

namespace Amiki.Modules.Assistant;

/// <summary>
/// Carries out a change Amiki suggested, once you confirm it, through the same stores your own
/// taps use. So it syncs, lands in the audit history, keeps the idea↔task links working, and can
/// be undone like anything else.
/// </summary>
public sealed class ProposalRunner(
    ChatSession chat, TaskStore tasks, TaskActions taskActions, FinanceStore money, FinanceActions moneyActions,
    IdeaStore ideas, InboxStore inbox, AssistantSettingsStore settings, WishlistStore wishlist, WishlistActions wishlistActions, ISnackbar snackbar)
{
    /// <summary>Does it as suggested.</summary>
    public void Do(AssistantProposal p)
    {
        Action? undo = p.Kind switch
        {
            ProposalKind.AddTask => AddTask(p),
            ProposalKind.LogMoney => LogMoney(p),
            ProposalKind.AddIdea => AddIdea(p),
            ProposalKind.CompleteTask => CompleteTask(p),
            ProposalKind.AddInboxNote => AddInboxNote(p),
            ProposalKind.Remember => Remember(p),
            ProposalKind.AddWish => AddWish(p),
            _ => null,
        };
        if (undo is null) return;

        chat.SetStatus(p.Id, ProposalStatus.Done);
        snackbar.Add($"Done: {p.Describe()}", Severity.Normal, c =>
        {
            c.Action = "Undo";
            c.ActionColor = Color.Primary;
            c.OnClick = _ =>
            {
                undo();
                chat.SetStatus(p.Id, ProposalStatus.Waiting);
                return Task.CompletedTask;
            };
        });
    }

    /// <summary>Opens the usual dialog filled in from the suggestion, for tasks, money and the wishlist; the rest just happen.</summary>
    public async Task EditAsync(AssistantProposal p)
    {
        var saved = p.Kind switch
        {
            ProposalKind.AddTask => await taskActions.EditAsync(TaskFrom(p)),
            ProposalKind.LogMoney => await moneyActions.EditAsync(TransactionFrom(p)),
            ProposalKind.AddWish => await wishlistActions.EditAsync(WishFrom(p)),
            _ => false,
        };
        if (saved) chat.SetStatus(p.Id, ProposalStatus.Done);
    }

    public void Skip(AssistantProposal p) => chat.SetStatus(p.Id, ProposalStatus.Skipped);

    private Action AddTask(AssistantProposal p)
    {
        var task = TaskFrom(p);
        tasks.Upsert(task);
        return () => tasks.Delete(task);
    }

    private Action LogMoney(AssistantProposal p)
    {
        var tx = TransactionFrom(p);
        money.Upsert(tx);
        return () => money.Remove(tx);
    }

    private Action AddIdea(AssistantProposal p)
    {
        var idea = new Idea { Text = p.Title, Tags = [.. p.Tags ?? []] };
        ideas.Upsert(idea);
        return () => ideas.Remove(idea);
    }

    // The task may have been finished or deleted since she suggested it.
    private Action? CompleteTask(AssistantProposal p)
    {
        if (tasks.All.FirstOrDefault(t => t.Id == p.TaskId) is not { IsDone: false } task)
        {
            snackbar.Add($"“{p.Title}” is already done or no longer exists.", Severity.Normal);
            chat.SetStatus(p.Id, ProposalStatus.Skipped);
            return null;
        }
        tasks.Toggle(task);
        return () =>
        {
            if (tasks.All.FirstOrDefault(t => t.Id == task.Id) is { IsDone: true } current) tasks.Toggle(current);
        };
    }

    private Action AddWish(AssistantProposal p)
    {
        var item = WishFrom(p);
        wishlist.Upsert(item);
        return () => wishlist.Remove(item);
    }

    private Action AddInboxNote(AssistantProposal p)
    {
        var item = new InboxItem { Text = p.Title };
        inbox.Restore(item);
        return () => inbox.Remove(item);
    }

    // Adds the fact as a new line of "About you", which you can still edit in her settings.
    private Action? Remember(AssistantProposal p)
    {
        var before = settings.Current;
        var aboutMe = string.IsNullOrWhiteSpace(before.AboutMe) ? p.Title : $"{before.AboutMe.TrimEnd()}\n{p.Title}";
        if (aboutMe.Length > AssistantSettings.MaxAboutMe)
        {
            snackbar.Add("“About you” is full. Tidy it up in Amiki's settings first.", Severity.Warning);
            return null;
        }
        var updated = before.Clone();
        updated.AboutMe = aboutMe;
        settings.Save(updated);
        return () => settings.Save(before);
    }

    private static TaskItem TaskFrom(AssistantProposal p) =>
        new() { Title = p.Title, Due = p.Due ?? DateTime.Today, Project = p.Project, Priority = p.Priority };

    private static WishItem WishFrom(AssistantProposal p) => new() { Name = p.Title, Price = p.Amount ?? 0 };

    private static Transaction TransactionFrom(AssistantProposal p) => new()
    {
        Kind = p.MoneyKind ?? TxKind.Expense,
        Amount = p.Amount ?? 0,
        Category = p.Category ?? Category.FallbackFor(p.MoneyKind ?? TxKind.Expense),
        Account = p.Account ?? "Cash",
        Note = p.Title,
        Date = p.Date ?? DateTime.Today,
    };
}
