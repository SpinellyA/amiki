using Amiki.Core;

namespace Amiki.Modules.Finance;

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

/// <summary>
/// A what-if: a snapshot of your balance on the day it was made, then a list of things you
/// might spend or get. The snapshot stays fixed so the plan keeps saying what it said,
/// until you choose "update to today's balance".
/// </summary>
public sealed class Plan : IEntity
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = "New plan";
    public decimal StartBalance { get; set; }
    public DateTime StartDate { get; set; } = DateTime.Today;
    public List<PlanItem> Items { get; set; } = [];
    public DateTime CreatedAt { get; init; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    /// <summary>Set when archived: kept for looking back, read-only, out of the picker and comparison.</summary>
    public DateTime? ArchivedAt { get; set; }

    public bool IsArchived => ArchivedAt is not null;

    public decimal Spending => Items.Where(i => i.Kind == TxKind.Expense).Sum(i => i.Amount);
    public decimal Income => Items.Where(i => i.Kind == TxKind.Income).Sum(i => i.Amount);
    public decimal LeftOver => StartBalance + Items.Sum(i => i.Signed);

    /// <summary>Items in the order they'd happen. Same day: money in first, so it isn't counted as a dip.</summary>
    public IEnumerable<PlanItem> Timeline => Items.OrderBy(i => i.Date).ThenBy(i => i.Kind == TxKind.Income ? 0 : 1);

    /// <summary>The lowest your balance gets along the way, and when.</summary>
    public (decimal Balance, DateTime Date) Lowest()
    {
        var (balance, lowest, date) = (StartBalance, StartBalance, StartDate);
        foreach (var item in Timeline)
        {
            balance += item.Signed;
            if (balance < lowest) (lowest, date) = (balance, item.Date);
        }
        return (lowest, date);
    }

    public Plan Clone(bool newIdentity = false) => new()
    {
        Id = newIdentity ? Guid.NewGuid() : Id,
        Name = Name,
        StartBalance = StartBalance,
        StartDate = StartDate,
        Items = Items.Select(i => i.Clone()).ToList(),
        CreatedAt = newIdentity ? DateTime.Now : CreatedAt,
        UpdatedAt = UpdatedAt,
        ArchivedAt = ArchivedAt,
    };

    public string? Validate() =>
        string.IsNullOrWhiteSpace(Name) ? "A plan needs a name."
        : Name.Length > 200 ? "Plan name is too long (200 characters max)."
        : Items.Count > 500 ? "That's too many items for one plan (500 max)."
        : Items.Any(i => i.Amount < 0) ? "Amounts can't be negative. Use money in or money out instead."
        : Items.Any(i => i.Name.Length > 200) ? "An item name is too long (200 characters max)."
        : null;

    /// <summary>
    /// Archived plans are read-only: saving one that is still archived is only accepted if
    /// nothing changed (so a retried save is harmless). Restoring it (clearing ArchivedAt) is fine.
    /// </summary>
    public static string? CheckReplace(Plan stored, Plan incoming) =>
        stored.IsArchived && incoming.IsArchived && !stored.SameContent(incoming)
            ? "Archived plans are read-only. Restore it first."
            : null;

    private bool SameContent(Plan other) =>
        Name == other.Name && StartBalance == other.StartBalance && StartDate == other.StartDate
        && Items.Count == other.Items.Count
        && Items.OrderBy(i => i.Id).Zip(other.Items.OrderBy(i => i.Id)).All(p => p.First.SameAs(p.Second));
}
