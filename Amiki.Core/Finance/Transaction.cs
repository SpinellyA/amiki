using Amiki.Core;

namespace Amiki.Modules.Finance;

public enum TxKind { Expense, Income }

public sealed class Transaction : IEntity
{
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

    public Transaction Clone() => (Transaction)MemberwiseClone();

    public string? Validate() =>
        Amount <= 0 ? "Amount must be more than zero."
        : string.IsNullOrWhiteSpace(Category) ? "Pick a category."
        : string.IsNullOrWhiteSpace(Account) ? "Pick an account."
        : Note.Length > 500 ? "Note is too long (500 characters max)."
        : null;

    public string Describe() =>
        $"{(Kind == TxKind.Income ? "Received" : "Spent")} {Money.Format(Amount)} · {Category} · {Account}"
        + (Note.Length > 0 && Note != Category ? $" · “{Note}”" : "");
}

public sealed record Account(string Name, decimal OpeningBalance);
