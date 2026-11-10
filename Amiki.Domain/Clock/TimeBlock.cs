using System.Text.Json.Serialization;

namespace Amiki.Domain.Clock;

/// <summary>
/// A stretch of one day set aside for something, usually a task: "2:00–4:00 PM, Lab 3 write-up".
/// Times are minutes after midnight, so a block can't spill past the end of its day.
/// </summary>
public sealed class TimeBlock : IEntity
{
    public const int MinutesInDay = 24 * 60;
    /// <summary>Blocks snap to this, so the dial and the times stay tidy.</summary>
    public const int Step = 5;
    public const int ShortestMinutes = 15;
    public const int MaxLabel = 200;

    public Guid Id { get; init; } = Guid.NewGuid();
    /// <summary>The day it's on (date only).</summary>
    public DateTime Day { get; set; }
    public int StartMinute { get; set; }
    /// <summary>When it ends; up to <see cref="MinutesInDay"/> (midnight at the end of the day).</summary>
    public int EndMinute { get; set; }
    /// <summary>The task this time is for, if any.</summary>
    public Guid? TaskId { get; set; }
    /// <summary>What it's for: the task's title when it was planned (kept if the task goes), or your own words.</summary>
    public string Label { get; set; } = "";
    public DateTime CreatedAt { get; init; } = DateTime.Now;

    [JsonIgnore]
    public int Minutes => EndMinute - StartMinute;

    [JsonIgnore]
    public DateTime Starts => Day.Date.AddMinutes(StartMinute);

    [JsonIgnore]
    public DateTime Ends => Day.Date.AddMinutes(EndMinute);

    public bool Contains(DateTime moment) => moment >= Starts && moment < Ends;

    public bool Overlaps(TimeBlock other) =>
        other.Id != Id && other.Day.Date == Day.Date && StartMinute < other.EndMinute && other.StartMinute < EndMinute;

    public TimeBlock Clone() => (TimeBlock)MemberwiseClone();

    public string? Validate() =>
        Day != Day.Date ? "A block is for a whole day's date."
        : StartMinute < 0 || EndMinute > MinutesInDay ? "A block has to fit inside its day."
        : StartMinute % Step != 0 || EndMinute % Step != 0 ? $"Times go in {Step}-minute steps."
        : Minutes < ShortestMinutes ? $"A block is at least {ShortestMinutes} minutes."
        : string.IsNullOrWhiteSpace(Label) ? "Say what the time is for."
        : Label.Length > MaxLabel ? $"That's too long ({MaxLabel} characters max)."
        : null;
}
