namespace Amiki.Domain.Wishlist;

/// <summary>
/// Wishlist money: the part of your balance set aside for wishlist things. Only a label on money
/// you already have; your accounts and total balance don't change.
///
/// <see cref="Amount"/> is the pool not put toward anything yet. Putting money toward an item
/// moves it from here to the item's <see cref="WishItem.Saved"/>; skipping or removing the item
/// moves it back; buying spends the item's money first, then the pool.
/// </summary>
public sealed class WishlistFund : IEntity
{
    /// <summary>There's only ever one per person; this is its id.</summary>
    public static readonly Guid SingleId = Guid.Parse("a15f0000-0000-4000-8000-000000000001");

    public const int MaxIncomeShare = 100;

    public Guid Id { get; init; } = SingleId;

    /// <summary>Wishlist money not put toward any item yet.</summary>
    public decimal Amount { get; set; }

    /// <summary>When set, this percent of every income you log goes into wishlist money by itself.</summary>
    public int? IncomeShare { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public WishlistFund Clone() => (WishlistFund)MemberwiseClone();

    /// <summary>A copy set to <paramref name="amount"/>.</summary>
    public WishlistFund SetTo(decimal amount, DateTime now)
    {
        var set = Clone();
        set.Amount = Math.Round(amount, 2);
        set.UpdatedAt = now;
        return set;
    }

    /// <summary>A copy with <paramref name="amount"/> added (or taken out, if negative; never below zero).</summary>
    public WishlistFund Add(decimal amount, DateTime now) => SetTo(Math.Max(0, Amount + amount), now);

    /// <summary>A copy with <paramref name="price"/> spent from it (never below zero).</summary>
    public WishlistFund Spend(decimal price, DateTime now) => Add(-price, now);

    public WishlistFund WithIncomeShare(int? percent, DateTime now)
    {
        var set = Clone();
        set.IncomeShare = percent is > 0 ? percent : null;
        set.UpdatedAt = now;
        return set;
    }

    /// <summary>What the income rule sets aside from <paramref name="income"/>; zero without a rule.</summary>
    public decimal ShareOf(decimal income) => IncomeShare is { } percent ? Math.Round(income * percent / 100, 2) : 0;

    /// <summary>
    /// Puts up to <paramref name="amount"/> from the pool toward <paramref name="item"/> (a negative
    /// amount takes it back). Never more than the pool has, or than the item still needs.
    /// </summary>
    public WishlistChange PutToward(WishItem item, decimal amount, DateTime now)
    {
        var moved = amount >= 0
            ? Math.Min(amount, Math.Min(Amount, item.Remaining))
            : -Math.Min(-amount, item.Saved);
        var updated = item.Clone();
        updated.Saved += moved;
        return new(Add(-moved, now), updated);
    }

    /// <summary>The item's money goes back to the pool (it was skipped, removed, or reopened from Bought).</summary>
    public WishlistChange Release(WishItem item, DateTime now)
    {
        var updated = item.Clone();
        updated.Saved = 0;
        return new(item.IsOpen ? Add(item.Saved, now) : this, updated);
    }

    /// <summary>
    /// Bought for <paramref name="paid"/>: spends what was put toward it first, then the pool for the
    /// rest (never below zero); anything put toward it beyond the price goes back to the pool.
    /// </summary>
    public WishlistChange Buy(WishItem item, decimal paid, Guid transactionId, DateTime now)
    {
        var bought = item.WithStatus(WishStatus.Bought, now);
        bought.TransactionId = transactionId;
        var fromPool = paid - item.Saved; // negative: it cost less than was put toward it
        bought.Saved = Math.Min(item.Saved, paid);
        return new(Add(-fromPool, now), bought);
    }

    public string? Validate() =>
        Id != SingleId ? "There's only one wishlist fund."
        : Amount < 0 ? "Wishlist money can't be negative."
        : Amount > WishItem.MaxPrice ? "That amount looks wrong."
        : IncomeShare is < 1 or > MaxIncomeShare ? $"The share of income has to be 1 to {MaxIncomeShare}%."
        : null;
}
