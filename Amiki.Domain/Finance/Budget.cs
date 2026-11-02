using System.Text.Json.Serialization;

namespace Amiki.Domain.Finance;

/// <summary>
/// How much you mean to spend in one month or week: a cut of your balance for that stretch of
/// time. With <see cref="Repeats"/>, it also applies to the periods after, until another budget
/// takes over. An <see cref="Amount"/> of zero means "no budget from here on".
/// </summary>
public sealed class Budget : IEntity
{
    public const decimal MaxAmount = 100_000_000;

    public Guid Id { get; init; }
    public PeriodLength Length { get; init; }
    /// <summary>The first day of the month or week it's for.</summary>
    public DateTime Start { get; init; }
    public decimal Amount { get; set; }
    public bool Repeats { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    /// <summary>Worked out from <see cref="Length"/> and <see cref="Start"/>, so never stored or sent.</summary>
    [JsonIgnore]
    public MoneyPeriod Period => new(Length, Start);

    /// <summary>A budget for <paramref name="period"/>. Its id comes from the period, so two devices edit the same one.</summary>
    public static Budget For(MoneyPeriod period, decimal amount, bool repeats) =>
        new() { Id = IdFor(period), Length = period.Length, Start = period.Start, Amount = Math.Round(amount, 2), Repeats = repeats };

    public static Guid IdFor(MoneyPeriod period) =>
        Guid.Parse($"b0d6e700-0000-4000-80{(period.Length == PeriodLength.Week ? "01" : "02")}-{period.Start:yyyyMMdd}0000");

    public string? Validate() =>
        Id != IdFor(Period) ? "This budget's id doesn't match its period."
        : MoneyPeriod.Containing(Length, Start).Start != Start ? "A budget starts on the first day of its month or week."
        : Amount < 0 ? "A budget can't be negative."
        : Amount > MaxAmount ? "That budget looks wrong."
        : null;
}
