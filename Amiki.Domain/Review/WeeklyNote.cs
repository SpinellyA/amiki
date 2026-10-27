using Amiki.Domain;

namespace Amiki.Domain.Review;

/// <summary>Your few lines of reflection for one week (Monday to Sunday). One per week.</summary>
public sealed class WeeklyNote : IEntity
{
    public const int MaxLength = 4000;

    public Guid Id { get; init; }
    /// <summary>The Monday the week starts on (date only).</summary>
    public DateTime WeekStart { get; init; }

    /// <summary>
    /// A new, empty note for the week. Its id comes from the date, so the phone and the PC writing
    /// that week's note at the same time save the same note instead of clashing.
    /// </summary>
    public static WeeklyNote For(DateTime weekStart) => new() { Id = IdFor(weekStart), WeekStart = weekStart.Date };

    public static Guid IdFor(DateTime weekStart) => Guid.Parse($"7eec0000-0000-4000-8000-{weekStart:yyyyMMdd}0000");
    public string WentWell { get; set; } = "";
    public string ToChange { get; set; } = "";
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public bool IsEmpty => string.IsNullOrWhiteSpace(WentWell) && string.IsNullOrWhiteSpace(ToChange);

    public WeeklyNote Clone() => (WeeklyNote)MemberwiseClone();

    public string? Validate() =>
        WeekStart.DayOfWeek != DayOfWeek.Monday || WeekStart != WeekStart.Date ? "A week starts on a Monday."
        : Id != IdFor(WeekStart) ? "This note's id doesn't match its week."
        : WentWell.Length > MaxLength || ToChange.Length > MaxLength ? $"Too long ({MaxLength} characters max per box)."
        : null;
}
