using System.Globalization;
using Amiki.Modules.Tasks;
using Microsoft.AspNetCore.Components;

namespace Amiki.Modules.Clock;

public partial class ClockPage
{
    [Inject] private ClockStore Store { get; set; } = default!;
    [Inject] private ClockActions Actions { get; set; } = default!;
    [Inject] private TaskStore Tasks { get; set; } = default!;
    [Inject] private TaskActions TaskActions { get; set; } = default!;
    [Inject] private MinuteTicker Ticker { get; set; } = default!;

    private DateTime _day = DateTime.Today;
    private Guid? _hovered;

    private DayPlan Plan => Store.PlanFor(_day);
    private bool IsToday => _day == DateTime.Today;
    private DateTime? Now => IsToday ? DateTime.Now : null;
    private int? NowMinute => Now is { } now ? ClockFormat.MinuteOf(now) : null;

    private string DayName => _day == DateTime.Today ? "Today"
        : _day == DateTime.Today.AddDays(1) ? "Tomorrow"
        : _day == DateTime.Today.AddDays(-1) ? "Yesterday"
        : _day.ToString("ddd, MMM d");

    protected override void OnInitialized()
    {
        Store.Changed += Refresh;
        Tasks.Changed += Refresh;
        Ticker.Ticked += Refresh;
    }

    public void Dispose()
    {
        Store.Changed -= Refresh;
        Tasks.Changed -= Refresh;
        Ticker.Ticked -= Refresh;
    }

    private void Refresh() => InvokeAsync(StateHasChanged);

    private void Move(int days) => _day = _day.AddDays(days);

    private TaskItem? TaskFor(TimeBlock block) =>
        block.TaskId is { } id ? Tasks.All.FirstOrDefault(t => t.Id == id) : null;

    /// <summary>A task's bar, as a share of the biggest one.</summary>
    private string Share(int minutes) =>
        (100.0 * minutes / Math.Max(1, Plan.ByTask[0].Minutes)).ToString("0.#", CultureInfo.InvariantCulture);
}
