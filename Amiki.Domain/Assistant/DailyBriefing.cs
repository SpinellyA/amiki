using Amiki.Domain;

namespace Amiki.Domain.Assistant;

/// <summary>
/// Amiki's note for one day, saved so it's written once (one model request a day) and your phone
/// and PC show the same one. Its id comes from the date, like diary pages.
/// </summary>
public sealed class DailyBriefing : IEntity
{
    public const int MaxLength = 2000;

    public Guid Id { get; init; }
    public DateTime Day { get; init; }
    public string Text { get; set; } = "";
    public DateTime WrittenAt { get; set; } = DateTime.Now;

    public static Guid IdFor(DateTime day) => Guid.Parse($"b81ef000-0000-4000-8000-{day:yyyyMMdd}0000");

    public static DailyBriefing For(DateTime day, string text, DateTime writtenAt) =>
        new() { Id = IdFor(day), Day = day.Date, Text = text.Length > MaxLength ? text[..MaxLength] : text, WrittenAt = writtenAt };

    public string? Validate() =>
        Id != IdFor(Day) ? "This briefing's id doesn't match its day."
        : Text.Length > MaxLength ? "Briefing is too long."
        : null;
}
