using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Amiki.Modules.Clock;

public partial class TimeBlockDialog
{
    private static readonly int[] Lengths = [30, 60, 90, 120];

    [CascadingParameter] private IMudDialogInstance Dialog { get; set; } = default!;

    /// <summary>The draft being edited; returned as the result when saved.</summary>
    [Parameter, EditorRequired] public TimeBlock Block { get; set; } = default!;

    /// <summary>The day it's on, to catch overlaps before saving.</summary>
    [Parameter, EditorRequired] public DayPlan Plan { get; set; } = default!;

    /// <summary>The tasks you can give time to.</summary>
    [Parameter] public IReadOnlyList<TaskItem> Tasks { get; set; } = [];

    // The overlap warning waits until you change something, so a fresh dialog doesn't open on a complaint.
    private bool Touched { get; set; }

    private TimeSpan? Start => TimeSpan.FromMinutes(Block.StartMinute);

    // Midnight at the end of the day shows as 12:00 AM, the picker's own way of saying it.
    private TimeSpan? End => TimeSpan.FromMinutes(Block.EndMinute % TimeBlock.MinutesInDay);

    private string? Problem => Block.Validate()
        ?? (Plan.ClashWith(Block) is { } clash ? $"That overlaps “{clash.Label}” ({ClockFormat.Range(clash)})." : null);

    private string TaskName(Guid? id) =>
        id is null ? "Something else…" : Tasks.FirstOrDefault(t => t.Id == id)?.Title ?? Block.Label;

    private void PickTask(Guid? id)
    {
        Block.TaskId = id;
        Block.Label = id is null ? "" : TaskName(id);
        Touched = true;
    }

    private void SetStart(TimeSpan? time)
    {
        if (time is not { } start) return;
        var length = Math.Max(Block.Minutes, TimeBlock.ShortestMinutes);
        Block.StartMinute = Snap(start);
        Block.EndMinute = Math.Min(Block.StartMinute + length, TimeBlock.MinutesInDay);
        Touched = true;
    }

    private void SetEnd(TimeSpan? time)
    {
        if (time is not { } end) return;
        var minute = Snap(end);
        Block.EndMinute = minute == 0 ? TimeBlock.MinutesInDay : minute;
        Touched = true;
    }

    private void SetLength(int minutes)
    {
        Block.EndMinute = Math.Min(Block.StartMinute + minutes, TimeBlock.MinutesInDay);
        Touched = true;
    }

    private static int Snap(TimeSpan time) =>
        (int)Math.Round(time.TotalMinutes / TimeBlock.Step) * TimeBlock.Step % TimeBlock.MinutesInDay;
}
