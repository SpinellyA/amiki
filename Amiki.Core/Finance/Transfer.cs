using Amiki.Core;

namespace Amiki.Modules.Finance;

/// <summary>
/// Money moved between your own accounts, e.g. Landbank to GCash. Not spending and not income:
/// the total stays the same, except for the fee, which leaves the "from" account as a real cost.
/// </summary>
public sealed class Transfer : IEntity
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime Date { get; set; } = DateTime.Today;
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    /// <summary>What arrives in <see cref="To"/>.</summary>
    public decimal Amount { get; set; }
    /// <summary>Charged on top, out of <see cref="From"/> (e.g. ₱15 InstaPay).</summary>
    public decimal Fee { get; set; }
    public string Note { get; set; } = "";
    public DateTime CreatedAt { get; init; } = DateTime.Now;

    /// <summary>What leaves <see cref="From"/>: the amount plus the fee.</summary>
    public decimal TotalOut => Amount + Fee;

    public Transfer Clone() => (Transfer)MemberwiseClone();

    public string? Validate() =>
        string.IsNullOrWhiteSpace(From) || string.IsNullOrWhiteSpace(To) ? "Pick both accounts."
        : string.Equals(From, To, StringComparison.OrdinalIgnoreCase) ? "Pick two different accounts."
        : Amount <= 0 ? "Amount must be more than zero."
        : Fee < 0 ? "A fee can't be negative."
        : Note.Length > 500 ? "Note is too long (500 characters max)."
        : null;

    public string Describe() =>
        $"Moved {Money.Format(Amount)} from {From} to {To}"
        + (Fee > 0 ? $" (fee {Money.Format(Fee)})" : "")
        + (Note.Length > 0 ? $" · “{Note}”" : "");
}
