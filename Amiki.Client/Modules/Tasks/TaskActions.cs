using MudBlazor;

namespace Amiki.Modules.Tasks;

/// <summary>UI-level task operations shared by the Tasks page and the dashboard widgets.</summary>
public sealed class TaskActions(TaskStore store, IDialogService dialogs, ISnackbar snackbar)
{
    /// <summary>
    /// Opens the task dialog. Pass an existing task to edit it, a new TaskItem to start from a
    /// prefilled draft, or nothing for a blank one. Returns whether the user saved.
    /// </summary>
    public async Task<bool> EditAsync(TaskItem? item = null)
    {
        var isNew = item is null || !store.All.Contains(item);
        var draft = item?.Clone() ?? new TaskItem();
        var parameters = new DialogParameters<TaskEditDialog> { { d => d.Item, draft } };
        var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true, CloseOnEscapeKey = true };

        var dialog = await dialogs.ShowAsync<TaskEditDialog>(isNew ? "New task" : "Edit task", parameters, options);
        if (await dialog.Result is not { Canceled: false, Data: TaskItem saved }) return false;

        store.Upsert(saved);
        return true;
    }

    /// <summary>Ticks a task on or off. Completing one offers an undo, since a misclick shouldn't cost anything.</summary>
    public void Toggle(TaskItem item)
    {
        store.Toggle(item);
        if (!item.IsDone) return;
        snackbar.Add($"Completed “{item.Title}”", Severity.Normal, config =>
        {
            config.Action = "Undo";
            config.ActionColor = Color.Primary;
            config.OnClick = _ =>
            {
                if (store.All.FirstOrDefault(t => t.Id == item.Id) is { IsDone: true } current) store.Toggle(current);
                return Task.CompletedTask;
            };
        });
    }

    public void DeleteWithUndo(TaskItem item)
    {
        store.Delete(item);
        snackbar.Add($"Deleted “{item.Title}”", Severity.Normal, config =>
        {
            config.Action = "Undo";
            config.ActionColor = Color.Primary;
            config.OnClick = _ =>
            {
                store.Upsert(item);
                return Task.CompletedTask;
            };
        });
    }
}
