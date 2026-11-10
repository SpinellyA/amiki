namespace Amiki.Domain.Clock;

/// <summary>One day's blocks: in order, what's on now, what's free, and the hours each task gets.</summary>
public sealed class DayPlan(DateTime day, IEnumerable<TimeBlock> blocks)
{
    public DateTime Day { get; } = day.Date;

    public IReadOnlyList<TimeBlock> Blocks { get; } =
        [.. blocks.Where(b => b.Day.Date == day.Date).OrderBy(b => b.StartMinute)];

    public int PlannedMinutes => Blocks.Sum(b => b.Minutes);

    public TimeBlock? At(DateTime moment) => Blocks.FirstOrDefault(b => b.Contains(moment));

    /// <summary>The next block starting after <paramref name="moment"/>.</summary>
    public TimeBlock? Next(DateTime moment) => Blocks.FirstOrDefault(b => b.Starts > moment);

    /// <summary>The block <paramref name="candidate"/> would clash with, if any.</summary>
    public TimeBlock? ClashWith(TimeBlock candidate) => Blocks.FirstOrDefault(candidate.Overlaps);

    /// <summary>Minutes given to each task (or label, for blocks without one), most first.</summary>
    public IReadOnlyList<(Guid? TaskId, string Label, int Minutes)> ByTask =>
        [.. Blocks.GroupBy(b => b.TaskId?.ToString() ?? "label:" + b.Label)
            .Select(g => (g.First().TaskId, g.First().Label, g.Sum(b => b.Minutes)))
            .OrderByDescending(t => t.Item3)];

    /// <summary>
    /// The first free stretch of at least <paramref name="minutes"/> from <paramref name="fromMinute"/>
    /// on (snapped to <see cref="TimeBlock.Step"/>), or null if the day is full from there.
    /// </summary>
    public (int Start, int End)? FirstFree(int minutes, int fromMinute)
    {
        var start = (int)Math.Ceiling(Math.Max(0, fromMinute) / (double)TimeBlock.Step) * TimeBlock.Step;
        foreach (var block in Blocks)
        {
            if (block.EndMinute <= start) continue;
            if (block.StartMinute - start >= minutes) return (start, start + minutes);
            start = Math.Max(start, block.EndMinute);
        }
        return TimeBlock.MinutesInDay - start >= minutes ? (start, start + minutes) : null;
    }
}
