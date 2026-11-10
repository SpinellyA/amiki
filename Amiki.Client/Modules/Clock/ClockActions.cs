using Amiki.Modules.Tasks;
using MudBlazor;

namespace Amiki.Modules.Clock;

/// <summary>Planning, changing and removing time blocks, with the dialog and "Undo".</summary>
public sealed class ClockActions(ClockStore store, TaskStore tasks, IDialogService dialogs, ISnackbar snackbar)
{
    private const int DefaultLength = 60;
    private const int DayStartsAt = 8 * 60;

    private static readonly DialogOptions Options = new() { MaxWidth = MaxWidth.Small, FullWidth = true, CloseOnEscapeKey = true };

    /// <summary>Blocks out new time on <paramref name="day"/>: the first free hour from now (today) or from 8 AM.</summary>
    public Task PlanAsync(DateTime day, TaskItem? task = null)
    {
        var plan = store.PlanFor(day);
        var from = day.Date == DateTime.Today ? Math.Max(DayStartsAt, ClockFormat.MinuteOf(DateTime.Now)) : DayStartsAt;
        var (start, end) = plan.FirstFree(DefaultLength, from) ?? plan.FirstFree(TimeBlock.ShortestMinutes, 0) ?? (from, from + DefaultLength);
        var draft = new TimeBlock
        {
            Day = day.Date,
            StartMinute = start,
            EndMinute = Math.Min(end, TimeBlock.MinutesInDay),
            TaskId = task?.Id,
            Label = task?.Title ?? "",
        };
        return ShowAsync(draft, plan, "Plan time");
    }

    public Task EditAsync(TimeBlock block) => ShowAsync(block.Clone(), store.PlanFor(block.Day), "Change time");

    public void DeleteWithUndo(TimeBlock block)
    {
        store.Remove(block);
        snackbar.Add($"Removed “{block.Label}” ({ClockFormat.Range(block)})", Severity.Normal, c =>
        {
            c.Action = "Undo";
            c.ActionColor = Color.Primary;
            c.OnClick = _ =>
            {
                store.Save(block);
                return Task.CompletedTask;
            };
        });
    }

    /// <summary>The open tasks you can give time to, soonest due first (plus the block's own task, even if done).</summary>
    private List<TaskItem> TasksFor(TimeBlock block) =>
        [.. tasks.All.Where(t => !t.IsDone || t.Id == block.TaskId)
            .OrderBy(t => t.Due ?? DateTime.MaxValue).ThenByDescending(t => t.Priority)];

    private async Task ShowAsync(TimeBlock draft, DayPlan plan, string title)
    {
        var parameters = new DialogParameters<TimeBlockDialog>
        {
            { d => d.Block, draft },
            { d => d.Plan, plan },
            { d => d.Tasks, TasksFor(draft) },
        };
        var dialog = await dialogs.ShowAsync<TimeBlockDialog>($"{title} · {draft.Day:ddd, MMM d}", parameters, Options);
        if (await dialog.Result is { Canceled: false, Data: TimeBlock saved }) store.Save(saved);
    }
}
