using Amiki.Modules.Diary;

namespace Amiki.Tests.Diary;

public class DiaryEntryTests
{
    private static readonly DateTime Today = new(2026, 10, 14);

    [Fact]
    public void A_days_page_always_gets_the_same_id()
    {
        Assert.Equal(DiaryEntry.For(Today).Id, DiaryEntry.For(Today.AddHours(15)).Id);
        Assert.NotEqual(DiaryEntry.For(Today).Id, DiaryEntry.For(Today.AddDays(1)).Id);
        Assert.Null(DiaryEntry.For(Today).Validate());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Mood_must_be_one_to_five(int mood)
    {
        var entry = DiaryEntry.For(Today);
        entry.Mood = mood;

        Assert.NotNull(entry.Validate());
    }

    [Fact]
    public void An_id_that_doesnt_match_the_day_is_rejected()
    {
        Assert.NotNull(new DiaryEntry { Id = Guid.NewGuid(), Day = Today }.Validate());
    }

    [Fact]
    public void A_page_with_only_a_mood_is_not_empty()
    {
        var entry = DiaryEntry.For(Today);
        Assert.True(entry.IsEmpty);

        entry.Mood = 4;
        Assert.False(entry.IsEmpty);
    }

    [Fact]
    public void Counts_words()
    {
        var entry = DiaryEntry.For(Today);
        entry.Text = "  Long exam went okay.\nThen   milk tea. ";

        Assert.Equal(7, entry.WordCount);
    }

    [Fact]
    public void Streak_counts_back_from_today()
    {
        DateTime[] days = [Today, Today.AddDays(-1), Today.AddDays(-2), Today.AddDays(-4)];

        Assert.Equal(3, DiaryEntry.Streak(days, Today));
    }

    [Fact]
    public void Streak_isnt_broken_just_because_today_isnt_written_yet()
    {
        DateTime[] days = [Today.AddDays(-1), Today.AddDays(-2)];

        Assert.Equal(2, DiaryEntry.Streak(days, Today));
    }

    [Fact]
    public void Streak_is_zero_after_a_missed_day()
    {
        DateTime[] days = [Today.AddDays(-2), Today.AddDays(-3)];

        Assert.Equal(0, DiaryEntry.Streak(days, Today));
    }
}
