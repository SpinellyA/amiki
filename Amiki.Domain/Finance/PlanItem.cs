namespace Amiki.Domain.Finance;

/// <summary>One line on a what-if receipt: "New laptop −₱35,000, now" or "October money +₱10,000, October".</summary>
public sealed class PlanItem
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public TxKind Kind { get; set; } = TxKind.Expense;
    public decimal Amount { get; set; }
    /// <summary>The plan's start date means "now"; otherwise the first of the month it happens in.</summary>
    public DateTime Date { get; set; }

    public decimal Signed => Kind == TxKind.Income ? Amount : -Amount;

    public PlanItem Clone() => (PlanItem)MemberwiseClone();

    public bool SameAs(PlanItem other) =>
        Id == other.Id && Name == other.Name && Kind == other.Kind && Amount == other.Amount && Date == other.Date;
}
