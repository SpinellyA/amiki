using Amiki.Modules.Tasks;
using MudBlazor;

namespace Amiki.Modules.Ideas;

/// <summary>UI-level idea operations: edit, move between columns, turn into a task, delete with undo.</summary>
public sealed class IdeaActions(IdeaStore store, TaskActions tasks, IDialogService dialogs, ISnackbar snackbar)
{
    private static string Name(IdeaStatus status) => IdeaStatusLook.Name(status);

    public async Task EditAsync(Idea idea)
    {
        var parameters = new DialogParameters<IdeaEditDialog> { { d => d.Text, idea.ToEditableText() } };
        var dialog = await dialogs.ShowAsync<IdeaEditDialog>("Edit idea", parameters,
            new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true, CloseOnEscapeKey = true });
        if (await dialog.Result is not { Canceled: false, Data: string text }) return;

        var edited = (store.Get(idea.Id) ?? idea).Clone();
        var parsed = Idea.Parse(text);
        edited.Text = parsed.Text;
        edited.Tags = parsed.Tags;
        store.Upsert(edited);
    }

    public void Move(Idea idea, IdeaStatus status)
    {
        var from = idea.Status;
        store.SetStatus(idea, status);
        WithUndo($"Moved to {Name(status)}", () => store.SetStatus(idea, from));
    }

    /// <summary>
    /// Opens a new task prefilled from the idea and linked to it. Saving it puts the idea in
    /// progress; ticking off its tasks later finishes the idea.
    /// </summary>
    public async Task MakeTaskAsync(Idea idea)
    {
        var draft = new TaskItem { Title = idea.Text, Project = idea.Tags.FirstOrDefault(), IdeaId = idea.Id };
        if (!await tasks.EditAsync(draft)) return;
        if (idea.Status != IdeaStatus.InProgress)
            snackbar.Add($"Task added · idea moved to {Name(IdeaStatus.InProgress)}", Severity.Normal);
        store.SetStatus(idea, IdeaStatus.InProgress);
    }

    public void Delete(Idea idea)
    {
        store.Remove(idea);
        WithUndo("Idea deleted", () => store.Upsert(idea));
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
