using Amiki.Core;

namespace Amiki.Modules.Diary;

/// <summary>One day's diary page: how the day felt (optional) and whatever you wrote.</summary>
public sealed class DiaryEntry : IEntity
{
    public const int MaxLength = 20000;
    public const int LowestMood = 1;
    public const int HighestMood = 5;

    public Guid Id { get; init; }
    /// <summary>The day this page is for (date only).</summary>
    public DateTime Day { get; init; }
    /// <summary>1 (rough) to 5 (great), or empty if you didn't pick one.</summary>
    public int? Mood { get; set; }
    public string Text { get; set; } = "";
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public bool IsEmpty => Mood is null && string.IsNullOrWhiteSpace(Text);

    /// <summary>
    /// A new, blank page for the day. Its id comes from the date, so writing on the phone and the
    /// PC the same day edits one page instead of creating two.
    /// </summary>
    public static DiaryEntry For(DateTime day) => new() { Id = IdFor(day), Day = day.Date };

    public static Guid IdFor(DateTime day) => Guid.Parse($"d1a40000-0000-4000-8000-{day:yyyyMMdd}0000");

    public DiaryEntry Clone() => (DiaryEntry)MemberwiseClone();

    public int WordCount => Text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

    public string? Validate() =>
        Day != Day.Date ? "A diary page is for a whole day."
        : Id != IdFor(Day) ? "This page's id doesn't match its day."
        : Mood is < LowestMood or > HighestMood ? $"Mood goes from {LowestMood} to {HighestMood}."
        : Text.Length > MaxLength ? $"That's a long day! ({MaxLength} characters max)"
        : null;

    /// <summary>Days in a row you've written, counting back from today (or yesterday, if today isn't written yet).</summary>
    public static int Streak(IEnumerable<DateTime> daysWritten, DateTime today)
    {
        var days = daysWritten.Select(d => d.Date).ToHashSet();
        var day = days.Contains(today.Date) ? today.Date : today.Date.AddDays(-1);
        var streak = 0;
        while (days.Contains(day))
        {
            streak++;
            day = day.AddDays(-1);
        }
        return streak;
    }
}
