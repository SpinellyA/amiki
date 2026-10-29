namespace Amiki.Domain.Wishlist;

/// <summary>What it takes to have an item's price saved by its target date.</summary>
public readonly record struct SavingPace(decimal Remaining, DateTime Today, DateTime Target)
{
    /// <summary>Whole weeks left, counting a part week as one (at least one).</summary>
    public int WeeksLeft => Math.Max(1, (int)Math.Ceiling((Target.Date - Today.Date).TotalDays / 7));

    /// <summary>How much to put toward it each week to make it.</summary>
    public decimal PerWeek => Math.Ceiling(Remaining / WeeksLeft);

    /// <summary>The target date has passed and it isn't fully saved.</summary>
    public bool IsLate => Target.Date < Today.Date;
}
