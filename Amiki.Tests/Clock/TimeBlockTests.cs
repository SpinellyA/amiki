namespace Amiki.Tests.Clock;

public class TimeBlockTests
{
    private static readonly DateTime Day = new(2026, 11, 11);

    private static TimeBlock Block(int start, int end, string label = "Lab report") =>
        new() { Day = Day, StartMinute = start, EndMinute = end, Label = label };

    [Fact]
    public void A_tidy_block_is_valid() => Assert.Null(Block(14 * 60, 16 * 60).Validate());

    [Theory]
    [InlineData(-5, 60)]          // before the day starts
    [InlineData(23 * 60, 1445)]   // past midnight
    [InlineData(60, 63)]          // off the 5-minute steps
    [InlineData(60, 70)]          // shorter than 15 minutes
    [InlineData(120, 60)]         // ends before it starts
    public void A_block_that_doesnt_fit_its_day_is_refused(int start, int end) =>
        Assert.NotNull(Block(start, end).Validate());

    [Fact]
    public void A_block_needs_a_date_and_a_label()
    {
        Assert.NotNull(new TimeBlock { Day = Day.AddHours(3), StartMinute = 0, EndMinute = 60, Label = "x" }.Validate());
        Assert.NotNull(Block(0, 60, label: "  ").Validate());
    }

    [Fact]
    public void A_block_can_run_to_midnight() => Assert.Null(Block(22 * 60, TimeBlock.MinutesInDay).Validate());

    [Fact]
    public void Blocks_that_share_time_overlap_but_touching_ones_dont()
    {
        var afternoon = Block(14 * 60, 16 * 60);

        Assert.True(afternoon.Overlaps(Block(15 * 60, 17 * 60)));
        Assert.True(afternoon.Overlaps(Block(14 * 60 + 30, 15 * 60)));
        Assert.False(afternoon.Overlaps(Block(16 * 60, 17 * 60)));
        Assert.False(afternoon.Overlaps(afternoon.Clone()), "a block never clashes with itself");
        Assert.False(afternoon.Overlaps(new TimeBlock { Day = Day.AddDays(1), StartMinute = 14 * 60, EndMinute = 16 * 60, Label = "x" }));
    }

    [Fact]
    public void Contains_is_true_from_the_start_up_to_but_not_at_the_end()
    {
        var block = Block(14 * 60, 16 * 60);

        Assert.True(block.Contains(Day.AddHours(14)));
        Assert.True(block.Contains(Day.AddHours(15.99)));
        Assert.False(block.Contains(Day.AddHours(16)));
    }
}
