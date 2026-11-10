namespace Amiki.Tests.Clock;

public class DayPlanTests
{
    private static readonly DateTime Day = new(2026, 11, 11);
    private static readonly Guid LabReport = Guid.NewGuid();

    private static TimeBlock Block(double fromHour, double toHour, string label = "Lab report", Guid? task = null, DateTime? day = null) =>
        new() { Day = day ?? Day, StartMinute = (int)(fromHour * 60), EndMinute = (int)(toHour * 60), Label = label, TaskId = task };

    private static readonly DayPlan Plan = new(Day,
    [
        Block(14, 16, task: LabReport),
        Block(9, 10, "Gym"),
        Block(19, 20, task: LabReport),
        Block(10, 11, "Other day", day: Day.AddDays(1)),
    ]);

    [Fact]
    public void Only_that_days_blocks_in_order()
    {
        Assert.Equal(["Gym", "Lab report", "Lab report"], Plan.Blocks.Select(b => b.Label));
        Assert.Equal(4 * 60, Plan.PlannedMinutes);
    }

    [Fact]
    public void Knows_whats_on_now_and_next()
    {
        Assert.Equal("Gym", Plan.At(Day.AddHours(9.5))?.Label);
        Assert.Null(Plan.At(Day.AddHours(12)));
        Assert.Equal(14 * 60, Plan.Next(Day.AddHours(12))?.StartMinute);
        Assert.Null(Plan.Next(Day.AddHours(21)));
    }

    [Fact]
    public void Adds_up_each_tasks_time_most_first()
    {
        var byTask = Plan.ByTask;

        Assert.Equal(((Guid?)LabReport, "Lab report", 180), byTask[0]);
        Assert.Equal(((Guid?)null, "Gym", 60), byTask[1]);
    }

    [Fact]
    public void Finds_the_first_free_stretch_long_enough()
    {
        Assert.Equal((10 * 60, 11 * 60), Plan.FirstFree(60, 9 * 60));            // right after the gym
        Assert.Equal((16 * 60, 18 * 60), Plan.FirstFree(120, 13 * 60 + 30));     // 13:30–14:00 is too short
        Assert.Equal((12 * 60 + 5, 13 * 60 + 5), Plan.FirstFree(60, 12 * 60 + 2)); // snapped to 5 minutes
        Assert.Equal((20 * 60, 24 * 60), Plan.FirstFree(240, 19 * 60 + 30));
        Assert.Null(Plan.FirstFree(300, 20 * 60));
    }

    [Fact]
    public void Spots_a_clash_before_saving()
    {
        Assert.Equal("Gym", Plan.ClashWith(Block(9.5, 10.5, "Read"))?.Label);
        Assert.Null(Plan.ClashWith(Block(10, 14, "Read")));
    }
}
