using System.Text.Json.Serialization;
using Amiki.Domain;

namespace Amiki.Domain.Finance;

public sealed class Transaction : IEntity
{
    public const int MaxNote = 500;

    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime Date { get; set; } = DateTime.Today;
    /// <summary>Always positive; <see cref="Kind"/> carries the direction.</summary>
    public decimal Amount { get; set; }
    public TxKind Kind { get; set; }
    public string Category { get; set; } = "Other";
    public string Account { get; set; } = "Cash";
    public string Note { get; set; } = "";
    public DateTime CreatedAt { get; init; } = DateTime.Now;

    public decimal Signed => Kind == TxKind.Income ? Amount : -Amount;

    /// <summary>
    /// Whether it counts in spending and income totals, charts and budgets. Everything does, except
    /// corrections that raised a balance: the first check on each account sets its real starting
    /// amount that way, which isn't income.
    /// </summary>
    public bool CountsInTotals => !Finance.Category.IsCorrection(Category) || Kind == TxKind.Expense;

    public Transaction Clone() => (Transaction)MemberwiseClone();

    public string? Validate() =>
        Amount <= 0 ? "Amount must be more than zero."
        : string.IsNullOrWhiteSpace(Category) ? "Pick a category."
        : string.IsNullOrWhiteSpace(Account) ? "Pick an account."
        : Note.Length > MaxNote ? $"Note is too long ({MaxNote} characters max)."
        : null;

    public string Describe() =>
        $"{(Kind == TxKind.Income ? "Received" : "Spent")} {Money.Format(Amount)} · {Category} · {Account}"
        + (Note.Length > 0 && Note != Category ? $" · “{Note}”" : "");
}
